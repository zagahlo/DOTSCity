using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Transforms;
using UnityEngine.SocialPlatforms;
using Random = UnityEngine.Random;

public class DotsNavMeshAgent : MonoBehaviour
{
    public Transform waypointsEditorContainer;
    public List<Vector3> waypoints;
    [Space(5f)]
    public AgentConfiguration agentConfiguration;
    public bool reverseAtEnd;
    public bool ignoreObstacles;

    private Entity entity;
    private EntityManager entityManager;
    private Vector3 gizmoInitPosition;
    private bool initialized;

    private void Awake()
    {
        waypointsEditorContainer = null;
     
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        entity = entityManager.CreateEntity();
#if UNITY_EDITOR
        entityManager.SetName(entity, "Pedestrian_"+transform.position.x+"x"+transform.position.z);
#endif
    }

    private void OnEnable()
    {
        if (entity != Entity.Null && !initialized)
        {
            SetComponentData();
        }
    }

    private void Start()
    {
#if UNITY_EDITOR
        gizmoInitPosition = transform.position;
#endif
    }

    private void Update()
    {
        if (entity != Entity.Null && initialized)
        {
            transform.SetPositionAndRotation(
                entityManager.GetComponentData<LocalTransform>(entity).Position,
                entityManager.GetComponentData<LocalTransform>(entity).Rotation);
        }
    }

    private void SetComponentData()
    {
        entityManager.AddComponentData(entity, new AgentObjectComponentData
        {
            fromLocation = transform.position,
            toLocation = waypoints[0],
            speed = Random.Range(agentConfiguration.minSpeed, agentConfiguration.maxSpeed),
            rotationSpeed = agentConfiguration.rotationSpeed,
            minDistanceReached = agentConfiguration.minDistanceReached,
            queryPointBufferIndex = 0,
            maxIteration = agentConfiguration.maxIteration,
            maxPathSize = agentConfiguration.maxPathSize,
            pathNodeSize = agentConfiguration.pathNodeSize,
            reverseAtEnd = reverseAtEnd,
            ignoreObstacles = ignoreObstacles
        });
        
        var agentWaypointsBuffer = entityManager.AddBuffer<AgentsWaypointsBuffer>(entity);
        agentWaypointsBuffer.Add(new AgentsWaypointsBuffer { agentWaypoint = transform.position });

        var waypointsCount = waypoints.Count;
        if (waypointsCount == 0)
        {
            Debug.LogWarning(gameObject.name+ " doesn't have any waypoint");
            enabled = false;
        }
        else
        {
            for (int i = 0; i < waypointsCount; i++)
            {
                agentWaypointsBuffer.Add(new AgentsWaypointsBuffer { agentWaypoint = waypoints[i] });
            }
        }

        entityManager.AddBuffer<QueryPointBuffer>(entity);
        entityManager.AddComponentData(entity,
            LocalTransform.FromPositionRotation(transform.position, transform.rotation));
        
        initialized = true;
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
                Gizmos.DrawLine(waypoints[i-1], waypoints[i]);
            }
        }

        if (waypoints.Count > 0)
        {
            if (Application.isPlaying)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(gizmoInitPosition, .3f);
                Gizmos.color = Color.green;
                Gizmos.DrawLine(gizmoInitPosition, waypoints[0]);
                
                if(!reverseAtEnd)
                    Gizmos.DrawLine(waypoints[waypoints.Count-1], gizmoInitPosition);
            }
            else
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(transform.position, .3f);
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, waypoints[0]);
                
                if(!reverseAtEnd)
                    Gizmos.DrawLine(transform.position, waypoints[waypoints.Count-1]);
            }
            
        
           
        }
    }
#endif
}
