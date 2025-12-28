using Unity.Entities;
using Unity.Mathematics;

namespace ECS_Navmesh.System
{
    public struct QueryPointBuffer : IBufferElementData
    {
        public float3 queryWaypoint;
    }
}