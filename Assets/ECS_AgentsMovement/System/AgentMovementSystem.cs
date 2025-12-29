using ECS_AgentsMovement.Component;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ECS_AgentsMovement.System
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
                // normalize vector to apply correct speed
                var direction = awb[agentData.waypointsBufferIndex].agentWaypoint - trans.Position;
                var normalizedDirection = math.lengthsq(direction) > 0 ? math.normalize(direction) : direction;
                
                // move the agent
                trans.Position += normalizedDirection * agentData.movementSpeed * DeltaTime;

                LogConsole(agentData, "agentData.waypointsBufferIndex"+agentData.waypointsBufferIndex);
                // if the distance between agent currentPosition and the nextWaypoint is less than min required configured
                if (math.distance(trans.Position, awb[agentData.waypointsBufferIndex].agentWaypoint) <= agentData.minDistanceReq)
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
                }

                trans.Rotation = math.slerp(trans.Rotation,
                    quaternion.LookRotationSafe(normalizedDirection, math.up()),
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
