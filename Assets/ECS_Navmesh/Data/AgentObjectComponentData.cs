using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Experimental.AI;

namespace ECS_Navmesh.Data
{
    public struct AgentObjectComponentData : IComponentData
    {
        public float3 fromLocation;
        public float3 toLocation;
        public bool ignoreObstacles;
        public bool reverseAtEnd;

        public bool reversing;

        //Movement
        public float3 waypointDirection;
        public float speed;
        public float rotationSpeed;
        public float minDistanceReached;
        public int waypointsBufferIndex;
        public int queryPointBufferIndex;

        public int maxIteration;
        public int maxPathSize;
        public int pathNodeSize;

        public PathRequestQueryInfo pathRequestQueryInfo;

        public struct PathRequestQueryInfo
        {
            public NavMeshLocation fromLocation;
            public NavMeshLocation toLocation;

            public bool inProgress;
        }
    }
}