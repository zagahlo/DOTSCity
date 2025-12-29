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
        private EntityQuery _eQuery;

        protected override void OnCreate()
        {
            // Query explícita: solo entidades que tengan TODO lo que el job necesita
            _eQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadWrite<AgentObjectComponentData>(),
                    ComponentType.ReadWrite<LocalTransform>(),
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

            Dependency = job.ScheduleParallel(_eQuery,Dependency);
        }
        
        [BurstCompile]
        public partial struct AgentMovementJob : IJobEntity
        {
            public float DeltaTime;

            void Execute(
                DynamicBuffer<AgentsWaypointsBuffer> awb,
                ref AgentObjectComponentData agentData,
                ref LocalTransform trans)
            {
                // if the agent currentPosition - toLocation are less than min configured, pick the next location point from the agent waypoints buffer
                if (math.distance(trans.Position, agentData.toLocation) < agentData.minDistanceReq)
                {
                    for (var i = 0; i < awb.Length; i++)
                    {
                        // set the next waypoint if the distance from the current one is greater than min configured
                        if (math.distance(trans.Position, awb[i].agentWaypoint) > agentData.minDistanceReq)
                        {
                            agentData.waypointsBufferIndex++;
                            LogConsole(agentData, $"waypointsBufferIndex++ {agentData.waypointsBufferIndex}");
                            break;
                        }
                    }
                }
                
                var d = awb[agentData.waypointsBufferIndex].agentWaypoint - trans.Position;
                agentData.waypointDirection = math.lengthsq(d) > 0 ? math.normalize(d) : d;
                
                trans.Position += agentData.waypointDirection * agentData.speed * DeltaTime;

                if (math.distance(trans.Position, agentData.toLocation) <= agentData.minDistanceReq)
                {
                    if (!agentData.reversing)
                    {
                        if (agentData.waypointsBufferIndex == awb.Length - 1)
                        {
                            agentData.waypointsBufferIndex = 0;
                            LogConsole(agentData, $"waypointsBufferIndex set 0");
                        }
                        else
                        {
                            agentData.waypointsBufferIndex++;
                            LogConsole(agentData, $"waypointsBufferIndex++ {agentData.waypointsBufferIndex}");
                            if (agentData.waypointsBufferIndex == awb.Length - 1)
                            {
                                if (agentData.reverseAtEnd)
                                    agentData.reversing = true;
                            }
                        }
                    }
                    else
                    {
                        agentData.waypointsBufferIndex--;
                        LogConsole(agentData, $"waypointsBufferIndex-- {agentData.waypointsBufferIndex}");

                        if (agentData.waypointsBufferIndex == 0)
                            agentData.reversing = false;
                    }

                    agentData.toLocation = awb[agentData.waypointsBufferIndex].agentWaypoint;
                }

                trans.Rotation = math.slerp(trans.Rotation,
                    quaternion.LookRotationSafe(agentData.waypointDirection, math.up()),
                    agentData.rotationSpeed * DeltaTime);
            }
        }

        private static void LogConsole(AgentObjectComponentData agentData, string message)
        {
            if(agentData.logger)
                Debug.Log(message);
        }
    }
}
