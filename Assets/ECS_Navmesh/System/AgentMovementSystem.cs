using ECS_Navmesh.Data;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ECS_Navmesh.System
{
    public partial class AgentMovementSystem : SystemBase
    {
        private EntityQuery _mQuery;

        protected override void OnCreate()
        {
            // Query explícita: solo entidades que tengan TODO lo que el job necesita
            _mQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadWrite<AgentObjectComponentData>(),
                    ComponentType.ReadWrite<LocalTransform>(),
                    ComponentType.ReadWrite<QueryPointBuffer>(),
                    ComponentType.ReadWrite<AgentsWaypointsBuffer>(),
                }
            });
        }

        protected override void OnUpdate()
        {
            var job = new AgentMovementJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            };

            // Parallel + Burst
            Dependency = job.ScheduleParallel(_mQuery,Dependency);
        }
        
        [BurstCompile]
        public partial struct AgentMovementJob : IJobEntity
        {
            public float DeltaTime;

            void Execute(
                DynamicBuffer<QueryPointBuffer> qpb,
                DynamicBuffer<AgentsWaypointsBuffer> awb,
                ref AgentObjectComponentData agentData,
                ref LocalTransform trans)
            {
                // if the agent currentPosition - toLocation are less than min configured, pick the next location point from the agent waypoints buffer
                if (math.distance(trans.Position, agentData.toLocation) < agentData.minDistanceReached)
                {
                    for (var i = 0; i < awb.Length; i++)
                    {
                        var nextWp = awb[i].agentWaypoint;
                        // set the next waypoint if the distance from the current one is greater than min configured
                        if (math.distance(trans.Position, nextWp) > agentData.minDistanceReached)
                        {
                            agentData.toLocation = nextWp;
                            agentData.waypointsBufferIndex++;
                            LogConsole(agentData, $"waypointsBufferIndex++ {agentData.waypointsBufferIndex}");
                            break;
                        }
                    }
                }
                
                //  check query point buffer waypoints
                if (qpb.Length < 1)
                {
                    // if empty, add the next one
                    qpb.Add(new QueryPointBuffer { queryWaypoint = agentData.toLocation });
                }
                else
                {
                    agentData.queryPointBufferIndex = math.clamp(agentData.queryPointBufferIndex, 0, qpb.Length - 1);
                    
                    var d = qpb[agentData.queryPointBufferIndex].queryWaypoint - trans.Position;
                    agentData.waypointDirection = math.lengthsq(d) > 0.00001f ? math.normalize(d) : d;
                    
                    trans.Position += agentData.waypointDirection * agentData.speed * DeltaTime;

                    if (math.distance(trans.Position, qpb[agentData.queryPointBufferIndex].queryWaypoint) <= agentData.minDistanceReached &&
                        agentData.queryPointBufferIndex < qpb.Length - 1)
                    {
                        agentData.queryPointBufferIndex++;
                    }

                    if (math.distance(trans.Position, agentData.toLocation) <= agentData.minDistanceReached)
                    {
                        if (!agentData.reversing && agentData.waypointsBufferIndex < awb.Length - 1)
                        {
                            agentData.waypointsBufferIndex++;
                            LogConsole(agentData, $"waypointsBufferIndex++ {agentData.waypointsBufferIndex}");

                            if (agentData.waypointsBufferIndex == awb.Length - 1)
                            {
                                if (agentData.reverseAtEnd)
                                    agentData.reversing = true;
                            }

                        }
                        else if (agentData.reversing && agentData.waypointsBufferIndex > 0)
                        {
                            agentData.waypointsBufferIndex--;
                            LogConsole(agentData, $"waypointsBufferIndex-- {agentData.waypointsBufferIndex}");

                            if (agentData.waypointsBufferIndex == 0)
                                agentData.reversing = false;
                        }
                        else if (agentData.waypointsBufferIndex == awb.Length - 1)
                        {
                            agentData.waypointsBufferIndex = 0;
                            LogConsole(agentData, $"waypointsBufferIndex set 0");
                        }

                        agentData.toLocation = awb[agentData.waypointsBufferIndex].agentWaypoint;
                        agentData.queryPointBufferIndex = 0;
                        qpb.Clear();
                    }

                    trans.Rotation = math.slerp(trans.Rotation,
                        quaternion.LookRotationSafe(agentData.waypointDirection, math.up()),
                        agentData.rotationSpeed * DeltaTime);

                }
            }
        }

        private static void LogConsole(AgentObjectComponentData agentData, string message)
        {
            if(agentData.logger)
                Debug.Log(message);
        }
    }
}
