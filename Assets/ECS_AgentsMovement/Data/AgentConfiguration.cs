using UnityEngine;

namespace ECS_AgentsMovement.Data
{
    [CreateAssetMenu(menuName = "ScriptableObject/Agent Configuration")]
    public class AgentConfiguration : ScriptableObject
    {
        public float movementSpeed;
        public float rotationSpeed;
        public float minDistanceReached;
    }
}