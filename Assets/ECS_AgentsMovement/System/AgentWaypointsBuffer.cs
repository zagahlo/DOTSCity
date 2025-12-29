using Unity.Entities;
using Unity.Mathematics;

namespace ECS_AgentsMovement.System
{
    public struct AgentsWaypointsBuffer : IBufferElementData
    {
        public float3 agentWaypoint;
    }
}