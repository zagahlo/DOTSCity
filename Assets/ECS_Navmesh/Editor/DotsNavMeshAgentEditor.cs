using System.Collections.Generic;
using ECS_Navmesh.MonoBehavior;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[CanEditMultipleObjects]
[CustomEditor(typeof(DotsNavMeshAgent))]
public class DotsNavMeshAgentEditor  : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        GUILayout.Space(15);
        DotsNavMeshAgent navMeshAgent = (DotsNavMeshAgent) target;
        var waypointsContainer = navMeshAgent.waypointsEditorContainer;
        
        if(waypointsContainer == null) return;
        
        var wpArray = new List<Transform>();
        foreach (Transform transformItem in waypointsContainer.transform) { wpArray.Add(transformItem); }
        
        if (GUILayout.Button("Add Waypoint"))
        {
            CreateExistingWaypoints(navMeshAgent, wpArray, waypointsContainer);

            var newPos = navMeshAgent.transform.position;
            if (navMeshAgent.waypoints.Count > 0) newPos = navMeshAgent.waypoints[navMeshAgent.waypoints.Count - 1];
            
            var newWaypoint = CreateWaypoint(waypointsContainer.transform, newPos);
            
            navMeshAgent.waypoints.Add(newPos);
            newWaypoint.name = "Waypoint_"+(navMeshAgent.waypoints.Count-1);

            Selection.activeGameObject = newWaypoint;
            
            EditorUtility.SetDirty(navMeshAgent);
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Edit Waypoints"))
        {
            CreateExistingWaypoints(navMeshAgent, wpArray, waypointsContainer);
            
            EditorUtility.SetDirty(navMeshAgent);
        }

        GUILayout.Space(5);
        
        if (GUILayout.Button("Update"))
        {
            for (int i = 0; i < wpArray.Count; i++)
            {
                navMeshAgent.waypoints[i] = wpArray[i].position;
            }
            
            for (int i = 0; i < wpArray.Count; i++)
            {
                DestroyImmediate(wpArray[i].gameObject);
            }
            wpArray.Clear();
            
            EditorUtility.SetDirty(navMeshAgent);
            
        }

        GUILayout.Space(10);
        
        if (GUILayout.Button("Clone"))
        {
            CreateExistingWaypoints(navMeshAgent, wpArray, waypointsContainer);
            
            EditorUtility.SetDirty(navMeshAgent);

            GameObject clonedAgent = PrefabUtility.InstantiatePrefab(PrefabUtility.GetCorrespondingObjectFromSource(Selection.activeObject)) as GameObject;
            clonedAgent.transform.SetPositionAndRotation(navMeshAgent.transform.position, navMeshAgent.transform.rotation);
            clonedAgent.transform.SetAsLastSibling();

            var newWaypointsList = new List<Vector3>();
            foreach (var waypointItem in navMeshAgent.waypoints)
            {
                newWaypointsList.Add(waypointItem);
            }
            
            clonedAgent.GetComponent<DotsNavMeshAgent>().SetCloneValues(newWaypointsList, navMeshAgent.reverseAtEnd);

            CreateExistingWaypoints(clonedAgent.GetComponent<DotsNavMeshAgent>(), new List<Transform>(wpArray.Count), clonedAgent.GetComponent<DotsNavMeshAgent>().waypointsEditorContainer);

            for (int i = 0; i < wpArray.Count; i++)
            {
                DestroyImmediate(wpArray[i].gameObject);
            }
            wpArray.Clear();
            
            EditorUtility.SetDirty(navMeshAgent);
            
            Selection.activeObject = clonedAgent;

        }

    }

    private void CreateExistingWaypoints(DotsNavMeshAgent navMeshAgent, List<Transform> wpArray, Transform waypointsContainer)
    {
        if (wpArray.Count < 1)
        {
            for (int i = 0; i < navMeshAgent.waypoints.Count; i++)
            {
                var existingWp = CreateWaypoint(waypointsContainer.transform, navMeshAgent.waypoints[i]);
                existingWp.name = "Waypoint_" + i;
                wpArray.Add(existingWp.transform);
            }
        }
    }

    private GameObject CreateWaypoint(Transform parent, Vector3 pos)
    {
        GameObject newWaypoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        newWaypoint.tag = "EditorOnly";
        newWaypoint.transform.SetParent(parent);
        newWaypoint.transform.position = pos;
        newWaypoint.transform.localScale = Vector3.one * 0.6f;
        newWaypoint.GetComponent<MeshRenderer>().sharedMaterial.color = Color.cyan;
        newWaypoint.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

        return newWaypoint;
    }
}
