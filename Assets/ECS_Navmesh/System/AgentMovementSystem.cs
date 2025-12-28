using ECS_Navmesh.Data;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS_Navmesh.System
{
    public partial class AgentMovementSystem : SystemBase
    {
        private EntityQuery _mQuery;
        private const float CloseZero = 1e-6f;

        protected override void OnCreate()
        {
            var entityQueryDesc = new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(AgentObjectComponentData) }
            };

            _mQuery = GetEntityQuery(entityQueryDesc);
        }

        protected override void OnUpdate()
        {
            float deltaTime = World.Time.DeltaTime;

            //Movement
            Entities
                .WithStoreEntityQueryInField(ref _mQuery)
                .WithBurst()
                .ForEach((ref DynamicBuffer<QueryPointBuffer> ub,
                    ref DynamicBuffer<AgentsWaypointsBuffer> agentsWaypointsBuffers,
                    ref AgentObjectComponentData uc,
                    ref LocalTransform trans) =>
                {
                    if (math.abs(math.distance(uc.fromLocation, uc.toLocation)) <= CloseZero)
                    {
                        foreach (var agentsWaypointsBuffer in agentsWaypointsBuffers)
                        {
                            if (math.abs(math.distance(uc.fromLocation, agentsWaypointsBuffer.agentWaypoint)) > uc.minDistanceReached)
                            {
                                uc.toLocation = agentsWaypointsBuffer.agentWaypoint;
                                uc.waypointsBufferIndex++;
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (ub.Length < 1)
                        {
                            ub.Add(new QueryPointBuffer { wayPoints = uc.toLocation });
                        }
                    }

                    if (ub.Length > 0)
                    {
                        uc.queryPointBufferIndex = math.clamp(uc.queryPointBufferIndex, 0, ub.Length - 1);
                        
                        var d = ub[uc.queryPointBufferIndex].wayPoints - trans.Position;
                        if (math.lengthsq(d) > CloseZero)
                            uc.waypointDirection = math.normalize(d);
                        
                        trans.Position += uc.waypointDirection * uc.speed * deltaTime;

                        if (math.distance(trans.Position, ub[uc.queryPointBufferIndex].wayPoints) <= uc.minDistanceReached &&
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
                            quaternion.LookRotationSafe(uc.waypointDirection, math.up()),
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
