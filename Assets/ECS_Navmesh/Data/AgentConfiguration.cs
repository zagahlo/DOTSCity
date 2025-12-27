using UnityEngine;

namespace ECS_Navmesh.Data
{
    [CreateAssetMenu(menuName = "ScriptableObject/Map/NavMeshAgentConfig")]
    public class AgentConfiguration : ScriptableObject
    {
        public float minSpeed;
        public float maxSpeed;
        public float rotationSpeed;
        public float minDistanceReached;

        public int maxIteration = 1024;
        public int maxPathSize = 2048;
        public int pathNodeSize = 2048;
    }
}