using Unity.Entities;
using Unity.Mathematics;

public struct AgentsWaypointsBuffer : IBufferElementData
{
    public float3 agentWaypoint;
}