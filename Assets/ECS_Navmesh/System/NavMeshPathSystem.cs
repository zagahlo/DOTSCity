using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.AI;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(AgentMovementSystem))]
public partial class NavMeshPathSystem  : SystemBase
{
    private NavMeshWorld navMeshWorld;
    private EntityQuery m_Query;
    
    private Dictionary<Entity,NavMeshQuery> navMeshQueryDict;
    
    protected override void OnCreate()
    {
        navMeshWorld = NavMeshWorld.GetDefaultWorld();

        var entityQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[]{ typeof(AgentObjectComponentData)}
        };
        
        m_Query = GetEntityQuery(entityQueryDesc);

        navMeshQueryDict = new Dictionary<Entity, NavMeshQuery>();
    }


    protected override void OnDestroy()
    {
        base.OnDestroy();

        foreach (var navMeshQuery in navMeshQueryDict)
        {
            navMeshQuery.Value.Dispose();
        }
        
        navMeshQueryDict.Clear();
    }

    protected override void OnUpdate()
    {
        Entities
            .WithoutBurst()
            .WithStoreEntityQueryInField(ref m_Query)
            .ForEach((Entity e,
                ref DynamicBuffer<QueryPointBuffer> ub,
                ref DynamicBuffer<AgentsWaypointsBuffer> agentsWaypointsBuffers,
                ref AgentObjectComponentData uc)=>
            {
                if (!uc.ignoreObstacles && ub.Length == 0 && math.distance(uc.fromLocation, uc.toLocation) > 0)
                {
                    if (!uc.pathRequestQueryInfo.inProgress)
                    {
                        NavMeshQuery navMeshQuery = new NavMeshQuery(navMeshWorld, Allocator.Persistent, uc.pathNodeSize);
                    
                        NavMeshLocation  from = navMeshQuery.MapLocation(uc.fromLocation,  Vector3.one * 10, 0);
                        NavMeshLocation  to = navMeshQuery.MapLocation(uc.toLocation,  Vector3.one * 10, 0);

                        if (navMeshQuery.IsValid(from) && navMeshQuery.IsValid(to))
                        {
                            var status = navMeshQuery.BeginFindPath(from, to, -1);

                            if (status == PathQueryStatus.InProgress || status == PathQueryStatus.Success)
                            {
                                uc.pathRequestQueryInfo.fromLocation = from;
                                uc.pathRequestQueryInfo.toLocation = to;
                                uc.pathRequestQueryInfo.inProgress = true;
                                navMeshQueryDict[e] = navMeshQuery;
                            }
                            else
                            {
                                navMeshQuery.Dispose();
                            }
                        }
                        else
                        {
                            navMeshQuery.Dispose();
                        }
                    }

                    if (uc.pathRequestQueryInfo.inProgress)
                    {
                        NavMeshQuery navMeshQuery = navMeshQueryDict[e];
                        
                        UpdatePathFindingJob spfj = new UpdatePathFindingJob
                        {
                            query = navMeshQuery,
                            fromLocation = uc.fromLocation,
                            toLocation = uc.toLocation,
                            maxIteration = uc.maxIteration,
                            maxPathSize = uc.maxPathSize,
                            ub = ub,
                        };
                        
                        var spfjJob = spfj.Schedule();
                        spfjJob.Complete();
                       
                    }
                }

                if (ub.Length > 0 && uc.pathRequestQueryInfo.inProgress)
                {
                    uc.pathRequestQueryInfo.inProgress = false;

                    if (navMeshQueryDict.TryGetValue(e, out var navMeshQuery))
                    {
                        navMeshQuery.Dispose();
                        navMeshQueryDict.Remove(e);
                    }
                }
            }).Run();
    }
    
    
     [BurstCompile]
    private struct UpdatePathFindingJob : IJob
    {
        public NavMeshQuery query;
        public float3 fromLocation;
        public float3 toLocation;
        public int maxIteration;
        public DynamicBuffer<QueryPointBuffer> ub;
        public int maxPathSize;

        public void Execute()
        {
            PathQueryStatus returningStatus = query.UpdateFindPath(maxIteration, out int iterationPerformed);
            
            if ((returningStatus & PathQueryStatus.Success) != 0)
            {
                returningStatus = query.EndFindPath(out int polygonSize);
                
                NativeArray<NavMeshLocation> res = new NativeArray<NavMeshLocation>(polygonSize*2, Allocator.Temp);
                NativeArray<StraightPathFlags> straightPathFlag = new NativeArray<StraightPathFlags>(maxPathSize, Allocator.Temp);
                NativeArray<float> vertexSide = new NativeArray<float>(maxPathSize, Allocator.Temp);
                NativeArray<PolygonId> polys = new NativeArray<PolygonId>(polygonSize, Allocator.Temp);
                
                int straightPathCount = 0;
                query.GetPathResult(polys);
                returningStatus = NavMeshPathUtils.FindStraightPath(
                    query,
                    fromLocation,
                    toLocation,
                    polys,
                    polygonSize,
                    ref res,
                    ref straightPathFlag,
                    ref vertexSide,
                    ref straightPathCount,
                    maxPathSize
                    );
                
                if(returningStatus == PathQueryStatus.Success)
                {
                    for (int i=0; i<straightPathCount; i++)
                    {
                        ub.Add(new QueryPointBuffer { wayPoints = res[i].position });
                    }
                }
                res.Dispose();
                straightPathFlag.Dispose();
                polys.Dispose();
                vertexSide.Dispose();
            }
        }
    }
}