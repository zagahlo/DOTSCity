using Unity.Entities;
using Unity.Mathematics;

namespace ECS_Navmesh.Data
{
    public struct QueryPointBuffer : IBufferElementData
    {
        public float3 wayPoints;
    }
}