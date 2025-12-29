using Unity.Entities;

namespace ECS_AgentsMovement.Component
{
    public struct AgentObjectComponentData : IComponentData
    {
        public bool reverseAtEnd;
        public bool logger;

        //Movement
        public float movementSpeed;
        public float rotationSpeed;
        public float minDistanceReq;
        public int waypointsBufferIndex;
        public bool reversing;
    }
}