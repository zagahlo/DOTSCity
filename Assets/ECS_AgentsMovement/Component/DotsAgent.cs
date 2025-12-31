using System.Collections.Generic;
using ECS_AgentsMovement.Data;
using ECS_AgentsMovement.EntitiesSystem;
using Unity.Entities;
using UnityEngine;
using Unity.Transforms;

namespace ECS_AgentsMovement.Component
{
    public class DotsAgent : MonoBehaviour
    {
        public Transform waypointsEditorContainer;
        public List<Vector3> waypoints;
        [Space(5f)]
        public AgentConfiguration agentConfiguration;
        public bool reverseAtEnd;
        [Space(5f)]
        public bool logger;
        
        private Entity _entity;
        private EntityManager _entityManager;
        private Vector3 _gizmoInitPosition;
        private bool _initialized;

        private void Awake()
        {
            waypointsEditorContainer = null;

            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _entity = _entityManager.CreateEntity();
        }

        private void OnEnable()
        {
            if (_entity != Entity.Null && !_initialized)
            {
                InitComponentData();
            }
        }

        private void Start()
        {
#if UNITY_EDITOR
            _gizmoInitPosition = transform.position;
#endif
        }

        private void Update()
        {
            if (_entity != Entity.Null && _initialized)
            {
                transform.SetPositionAndRotation(
                    _entityManager.GetComponentData<LocalTransform>(_entity).Position,
                    _entityManager.GetComponentData<LocalTransform>(_entity).Rotation);
            }
        }

        private void InitComponentData()
        {
            var waypointsCount = waypoints?.Count;
            if (waypointsCount == 0)
            {
                Debug.LogWarning(gameObject.name + " doesn't have any waypoint");
                enabled = false;
                return;
            }

            _entityManager.AddComponentData(_entity, new AgentObjectComponentData
            {
                movementSpeed = agentConfiguration.movementSpeed,
                rotationSpeed = agentConfiguration.rotationSpeed,
                minDistanceReq = agentConfiguration.minDistanceReached,
                waypointsBufferIndex = 1,
                reverseAtEnd = reverseAtEnd,
                logger = logger
            });

            _entityManager.AddComponentData(_entity, LocalTransform.FromPositionRotation(transform.position, transform.rotation));
            
            var agentWaypointsBuffer = _entityManager.AddBuffer<AgentsWaypointsBuffer>(_entity);
            agentWaypointsBuffer.Add(new AgentsWaypointsBuffer
            {
                agentWaypoint = _entityManager.GetComponentData<LocalTransform>(_entity).Position
            });
            for (var i = 0; i < waypointsCount; i++)
            {
                agentWaypointsBuffer.Add(new AgentsWaypointsBuffer
                {
                    agentWaypoint = waypoints[i]
                });
            }

            _initialized = true;
        }



#if UNITY_EDITOR
        public void SetCloneValues(List<Vector3> waypoints, bool reverseAtEnd)
        {
            this.waypoints = waypoints;
            this.reverseAtEnd = reverseAtEnd;
        }

        private void OnDrawGizmosSelected()
        {
            for (int i = 0; i < waypoints.Count; i++)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(waypoints[i], .3f);
                if (i > 0)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(waypoints[i - 1], waypoints[i]);
                }
            }

            if (waypoints.Count > 0)
            {
                if (Application.isPlaying)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawSphere(_gizmoInitPosition, .3f);
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(_gizmoInitPosition, waypoints[0]);

                    if (!reverseAtEnd)
                        Gizmos.DrawLine(waypoints[waypoints.Count - 1], _gizmoInitPosition);
                }
                else
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawSphere(transform.position, .3f);
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(transform.position, waypoints[0]);

                    if (!reverseAtEnd)
                        Gizmos.DrawLine(transform.position, waypoints[waypoints.Count - 1]);
                }



            }
        }
#endif
    }
}