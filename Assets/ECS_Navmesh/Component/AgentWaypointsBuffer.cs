using Unity.Entities;
using Unity.Mathematics;

namespace ECS_Navmesh.Component
{
    public struct AgentsWaypointsBuffer : IBufferElementData
    {
        public float3 agentWaypoint;
    }
}