using Unity.Entities;
using Unity.Mathematics;

namespace ECS_Navmesh.Component
{
    public struct QueryPointBuffer : IBufferElementData
    {
        public float3 wayPoints;
    }
}