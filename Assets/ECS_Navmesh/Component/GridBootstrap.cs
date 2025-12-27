using ECS_Navmesh.System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECS_Navmesh.Component
{
    public class GridBootstrap : MonoBehaviour
    {
        public int width = 128;
        public int height = 128;
        public float cellSize = 1f;
        public Vector3 origin = Vector3.zero;

        void Start()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            var em = world.EntityManager;

            // Crea entidad singleton del grid
            Entity grid = em.CreateEntity(typeof(GridSettings));
            em.SetComponentData(grid, new GridSettings
            {
                Size = new int2(width, height),
                CellSize = cellSize,
                Origin = (float3)origin
            });

            // Buffer de celdas
            var buf = em.AddBuffer<GridCell>(grid);
            buf.ResizeUninitialized(width * height);

            // Todo walkable por defecto
            for (int i = 0; i < buf.Length; i++)
                buf[i] = new GridCell { Walkable = 1, Cost = 0 };

            // Ejemplo: bloquea un rectángulo (obstáculo) para probar
            BlockRect(em, grid, new int2(40, 40), new int2(60, 60));
        }

        static void BlockRect(EntityManager em, Entity grid, int2 min, int2 max)
        {
            var g = em.GetComponentData<GridSettings>(grid);
            var cells = em.GetBuffer<GridCell>(grid);

            for (int y = min.y; y <= max.y; y++)
            for (int x = min.x; x <= max.x; x++)
            {
                if ((uint)x >= (uint)g.Size.x || (uint)y >= (uint)g.Size.y) continue;
                int idx = y * g.Size.x + x;
                var c = cells[idx];
                c.Walkable = 0;
                cells[idx] = c;
            }
        }
    }
}