using ECS_Navmesh.System;
using Unity.Mathematics;

namespace ECS_Navmesh.Util
{
    public static class GridUtils
    {
        public static int Index(int2 p, int2 size) => p.y * size.x + p.x;

        public static bool InBounds(int2 p, int2 size) =>
            (uint)p.x < (uint)size.x && (uint)p.y < (uint)size.y;

        public static int2 WorldToCell(float3 w, in GridSettings g)
        {
            float2 local = (w.xz - g.Origin.xz) / g.CellSize;
            return new int2((int)math.floor(local.x), (int)math.floor(local.y));
        }

        public static float3 CellToWorldCenter(int2 c, in GridSettings g, float y = 0f)
        {
            float3 w = g.Origin + new float3((c.x + 0.5f) * g.CellSize, y, (c.y + 0.5f) * g.CellSize);
            return w;
        }
    }
}