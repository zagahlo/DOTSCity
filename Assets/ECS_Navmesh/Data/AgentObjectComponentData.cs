using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Experimental.AI;

namespace ECS_Navmesh.Data
{
    public struct AgentObjectComponentData : IComponentData
    {
        public float3 toLocation;
        public bool reverseAtEnd;
        public bool logger;

        public bool reversing;

        //Movement
        public float3 waypointDirection;
        public float speed;
        public float rotationSpeed;
        public float minDistanceReq;
        public int waypointsBufferIndex;
    }
}