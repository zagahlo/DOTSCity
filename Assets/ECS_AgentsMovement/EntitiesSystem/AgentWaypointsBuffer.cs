using Unity.Entities;
using Unity.Mathematics;

namespace ECS_AgentsMovement.EntitiesSystem
{
    public struct AgentsWaypointsBuffer : IBufferElementData
    {
        public float3 agentWaypoint;
    }
}