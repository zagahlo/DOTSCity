using Unity.Entities;
using Unity.Mathematics;

namespace ECS_Navmesh.System
{
    public struct GridSettings : IComponentData
    {
        public int2 Size;
        public float CellSize;
        public float3 Origin;
    }

    public struct GridCell : IBufferElementData
    {
        public byte Walkable;
        public byte Cost;
    }
}