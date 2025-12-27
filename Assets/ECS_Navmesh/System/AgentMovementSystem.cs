using ECS_Navmesh.Component;
using ECS_Navmesh.Data;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ECS_Navmesh.System
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(GridPathSystem))]
    public partial class AgentMovementSystem : SystemBase
    {
        private EntityQuery m_Query;

        protected override void OnCreate()
        {
            var entityQueryDesc = new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(AgentObjectComponentData) }
            };

            m_Query = GetEntityQuery(entityQueryDesc);
        }

        protected override void OnUpdate()
        {
            float deltaTime = World.Time.DeltaTime;

            //Movement
            Entities
                .WithStoreEntityQueryInField(ref m_Query)
                .WithBurst()
                .ForEach((ref DynamicBuffer<QueryPointBuffer> ub,
                    ref DynamicBuffer<AgentsWaypointsBuffer> agentsWaypointsBuffers,
                    ref AgentObjectComponentData uc,
                    ref LocalTransform trans) =>
                {
                    if (math.distance(uc.fromLocation, uc.toLocation) == 0)
                    {
                        foreach (var agentsWaypointsBuffer in agentsWaypointsBuffers)
                        {
                            if (math.distance(uc.fromLocation, agentsWaypointsBuffer.agentWaypoint) > 0)
                            {
                                uc.toLocation = agentsWaypointsBuffer.agentWaypoint;
                                uc.waypointsBufferIndex++;
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (ub.Length < 1 && uc.ignoreObstacles)
                        {
                            ub.Add(new QueryPointBuffer { wayPoints = uc.toLocation });
                        }
                    }

                    if (ub.Length > 0)
                    {
                        uc.queryPointBufferIndex = math.clamp(uc.queryPointBufferIndex, 0, ub.Length - 1);
                        
                        float3 d = ub[uc.queryPointBufferIndex].wayPoints - trans.Position;
                        if (math.lengthsq(d) > 1e-6f)
                            uc.waypointDirection = math.normalize(d);


                        trans.Position += uc.waypointDirection * uc.speed * deltaTime;

                        if (math.distance(trans.Position, ub[uc.queryPointBufferIndex].wayPoints) <=
                            uc.minDistanceReached &&
                            uc.queryPointBufferIndex < ub.Length - 1)
                        {
                            uc.queryPointBufferIndex++;
                        }

                        if (math.distance(trans.Position, uc.toLocation) <= uc.minDistanceReached)
                        {
                            uc.fromLocation = trans.Position;

                            if (!uc.reversing && uc.waypointsBufferIndex < agentsWaypointsBuffers.Length - 1)
                            {
                                uc.waypointsBufferIndex++;

                                if (uc.waypointsBufferIndex == agentsWaypointsBuffers.Length - 1)
                                {
                                    if (uc.reverseAtEnd) uc.reversing = true;
                                }

                            }
                            else if (uc.reversing && uc.waypointsBufferIndex > 0)
                            {
                                uc.waypointsBufferIndex--;

                                if (uc.waypointsBufferIndex == 0)
                                    uc.reversing = false;
                            }
                            else if (uc.waypointsBufferIndex == agentsWaypointsBuffers.Length - 1)
                            {
                                uc.waypointsBufferIndex = 0;
                            }

                            uc.toLocation = agentsWaypointsBuffers[uc.waypointsBufferIndex].agentWaypoint;
                            uc.queryPointBufferIndex = 0;
                            ub.Clear();
                        }

                        trans.Rotation = math.slerp(trans.Rotation,
                            quaternion.LookRotationSafe(uc.waypointDirection, Vector3.up),
                            uc.rotationSpeed * deltaTime);

                    }

                }).ScheduleParallel();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

        }
    }
}
