using ECS_Navmesh.Component;
using ECS_Navmesh.Data;
using ECS_Navmesh.System;
using ECS_Navmesh.Util;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS_Navmesh.System
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(AgentMovementSystem))]
    public partial class GridPathSystem : SystemBase
    {
        private Entity _gridEntity;
        private GridSettings _grid;
        private DynamicBuffer<GridCell> _cells;

        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<GridSettings>();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            _gridEntity = SystemAPI.GetSingletonEntity<GridSettings>();
            _grid = SystemAPI.GetSingleton<GridSettings>();
            _cells = EntityManager.GetBuffer<GridCell>(_gridEntity);
        }

        protected override void OnUpdate()
        {
            Entities
                .WithoutBurst()
                .ForEach((ref DynamicBuffer<QueryPointBuffer> ub,
                    ref AgentObjectComponentData uc,
                    in LocalTransform trans) =>
                {
                    if (uc.ignoreObstacles) return; // en tu lógica, si ignora, no calcula grid
                    if (ub.Length > 0) return;
                    if (math.distance(trans.Position, uc.toLocation) <= 0.001f) return;

                    int2 start = GridUtils.WorldToCell(trans.Position, _grid);
                    int2 goal = GridUtils.WorldToCell(uc.toLocation, _grid);

                    // Si fuera de bounds o bloqueado, no hacemos nada (o puedes buscar el walkable más cercano)
                    if (!GridUtils.InBounds(start, _grid.Size) || !GridUtils.InBounds(goal, _grid.Size) ||
                        !IsWalkable(start) || !IsWalkable(goal))
                        return;

                    ub.Clear();
                    FindPathAStarAndFillBuffer(start, goal, uc.toLocation.y, ref ub);

                }).Run();
        }

        private bool IsWalkable(int2 p)
        {
            int idx = GridUtils.Index(p, _grid.Size);
            return _cells[idx].Walkable != 0;
        }

        private static int Heuristic(int2 a, int2 b)
        {
            // Manhattan distance
            int2 d = math.abs(a - b);
            return d.x + d.y;
        }

        private void FindPathAStarAndFillBuffer(int2 start, int2 goal, float y, ref DynamicBuffer<QueryPointBuffer> ub)
        {
            // Open set (muy simple). Para empezar: NativeList y scans lineales.
            // Optimización futura: binary heap.
            var open = new NativeList<int2>(Allocator.Temp);
            var cameFrom = new NativeHashMap<int, int>(1024, Allocator.Temp); // key: idx, value: parentIdx
            var gScore = new NativeHashMap<int, int>(1024, Allocator.Temp); // key: idx, value: g
            var fScore = new NativeHashMap<int, int>(1024, Allocator.Temp);

            int startIdx = GridUtils.Index(start, _grid.Size);
            int goalIdx = GridUtils.Index(goal, _grid.Size);

            open.Add(start);
            gScore[startIdx] = 0;
            fScore[startIdx] = Heuristic(start, goal);

            // 4 vecinos
            int2 n0 = new int2(1, 0);
            int2 n1 = new int2(-1, 0);
            int2 n2 = new int2(0, 1);
            int2 n3 = new int2(0, -1);

            bool found = false;

            while (open.Length > 0)
            {
                // escoger el nodo con menor f (scan lineal)
                int bestI = 0;
                int bestF = int.MaxValue;
                for (int i = 0; i < open.Length; i++)
                {
                    int idx = GridUtils.Index(open[i], _grid.Size);
                    int f = fScore.TryGetValue(idx, out var fv) ? fv : int.MaxValue;
                    if (f < bestF)
                    {
                        bestF = f;
                        bestI = i;
                    }
                }

                int2 current = open[bestI];
                open.RemoveAtSwapBack(bestI);

                int currentIdx = GridUtils.Index(current, _grid.Size);
                if (currentIdx == goalIdx)
                {
                    found = true;
                    break;
                }

                // expand neighbors
                ExpandNeighbor(current, currentIdx, current + n0);
                ExpandNeighbor(current, currentIdx, current + n1);
                ExpandNeighbor(current, currentIdx, current + n2);
                ExpandNeighbor(current, currentIdx, current + n3);
            }

            if (!found)
            {
                open.Dispose();
                cameFrom.Dispose();
                gScore.Dispose();
                fScore.Dispose();
                return;
            }

            // reconstruir camino: goal -> start
            var path = new NativeList<int>(Allocator.Temp);
            int cur = goalIdx;
            path.Add(cur);

            while (cur != startIdx)
            {
                if (!cameFrom.TryGetValue(cur, out int parent))
                    break;
                cur = parent;
                path.Add(cur);
            }

            // invertir y llenar buffer (saltamos el start si quieres)
            for (int i = path.Length - 1; i >= 0; i--)
            {
                int idx = path[i];
                int2 cell = new int2(idx % _grid.Size.x, idx / _grid.Size.x);

                float3 wp = GridUtils.CellToWorldCenter(cell, _grid, y);
                ub.Add(new QueryPointBuffer { wayPoints = wp });
            }

            path.Dispose();
            open.Dispose();
            cameFrom.Dispose();
            gScore.Dispose();
            fScore.Dispose();

            // --- local function ---
            void ExpandNeighbor(int2 current, int currentIdx, int2 nb)
            {
                if (!GridUtils.InBounds(nb, _grid.Size)) return;
                if (!IsWalkable(nb)) return;

                int nbIdx = GridUtils.Index(nb, _grid.Size);

                int currentG = gScore.TryGetValue(currentIdx, out var cg) ? cg : int.MaxValue;
                int stepCost = 1 + _cells[nbIdx].Cost; // cost extra opcional
                int tentativeG = currentG + stepCost;

                int nbG = gScore.TryGetValue(nbIdx, out var ng) ? ng : int.MaxValue;
                if (tentativeG >= nbG) return;

                cameFrom[nbIdx] = currentIdx;
                gScore[nbIdx] = tentativeG;
                fScore[nbIdx] = tentativeG + Heuristic(nb, goal);

                // añade a open si no está (scan lineal)
                for (int i = 0; i < open.Length; i++)
                    if (open[i].x == nb.x && open[i].y == nb.y)
                        return;
                open.Add(nb);
            }
        }
    }
}