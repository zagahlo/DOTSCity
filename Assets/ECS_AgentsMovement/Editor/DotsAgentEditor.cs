using System.Collections.Generic;
using ECS_AgentsMovement.Component;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[CanEditMultipleObjects]
[CustomEditor(typeof(DotsAgent))]
public class DotsAgentEditor  : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        GUILayout.Space(15);
        DotsAgent singleAgent = (DotsAgent) target;
        var singleWaypointsContainer = singleAgent.waypointsEditorContainer;
        
        if(singleWaypointsContainer == null) return;
        
        var singleWpArray = new List<Transform>();
        foreach (Transform waypointItem in singleWaypointsContainer.transform)
        {
            singleWpArray.Add(waypointItem);
        }
        
        if (GUILayout.Button("Add Waypoint"))
        {
            CreateExistingWaypoints(singleAgent, singleWpArray, singleWaypointsContainer);

            var newPos = singleAgent.transform.position;
            if (singleAgent.waypoints.Count > 0) newPos = singleAgent.waypoints[singleAgent.waypoints.Count - 1];
            
            var newWaypoint = CreateWaypoint(singleWaypointsContainer.transform, newPos);
            
            singleAgent.waypoints.Add(newPos);
            newWaypoint.name = "Waypoint_"+(singleAgent.waypoints.Count-1);

            Selection.activeGameObject = newWaypoint;
            
            EditorUtility.SetDirty(singleAgent);
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Edit Waypoints"))
        {
            CreateExistingWaypoints(singleAgent, singleWpArray, singleWaypointsContainer);
            
            EditorUtility.SetDirty(singleAgent);
        }

        GUILayout.Space(5);
        
        if (GUILayout.Button("Clone"))
        {
            FinishAndRefresh(Selection.gameObjects);
            GameObject[] clonedAgents = new GameObject[Selection.gameObjects.Length];
            if (Selection.gameObjects.Length > 0)
            {
                for(var i = 0; i < Selection.gameObjects.Length; i++)
                {
                    var agentToClone = Selection.gameObjects[i].GetComponent<DotsAgent>();
                    var agentWaypointsArray = new List<Transform>();
                    foreach (Transform transformItem in agentToClone.transform)
                    {
                        agentWaypointsArray.Add(transformItem);
                    }
                    CreateExistingWaypoints(agentToClone, agentWaypointsArray, agentToClone.waypointsEditorContainer);
            
                    EditorUtility.SetDirty(agentToClone);
                    
                    GameObject clonedAgent = Instantiate(agentToClone.gameObject);
                    clonedAgent.gameObject.name = agentToClone.gameObject.name + "_" + (Selection.gameObjects.Length+i);
                    clonedAgent.transform.SetPositionAndRotation(agentToClone.transform.position, agentToClone.transform.rotation);
                    clonedAgent.transform.SetAsLastSibling();

                    var newWaypointsList = new List<Vector3>();
                    foreach (var waypointItem in agentToClone.waypoints)
                    {
                        newWaypointsList.Add(waypointItem);
                    }
            
                    clonedAgent.GetComponent<DotsAgent>().SetCloneValues(newWaypointsList, agentToClone.reverseAtEnd);
                    
                    for (var w = 0; w< agentWaypointsArray.Count; w++)
                    {
                        DestroyImmediate(agentWaypointsArray[w].gameObject);
                    }
                    agentWaypointsArray.Clear();
            
                    EditorUtility.SetDirty(agentToClone);
                    clonedAgents[i] = clonedAgent;
                }
            }

            Selection.objects = clonedAgents;
        }

        GUILayout.Space(15);
        var existWaypoints = false;
        for (var i = 0; i < Selection.gameObjects.Length; i++)
        {
            var agentSelected = Selection.gameObjects[i].GetComponent<DotsAgent>();
            if (agentSelected != null && agentSelected.transform.childCount > 0)
            {
                existWaypoints = true;
                break;
            }
        }
        if (existWaypoints && GUILayout.Button("Finish & Refresh"))
        {
            FinishAndRefresh(Selection.gameObjects);
        }
    }

    private void FinishAndRefresh(GameObject[] selectedGameObjects)
    {
        for (var i = 0; i < selectedGameObjects.Length; i++)
        {
            var agentSelected = selectedGameObjects[i].GetComponent<DotsAgent>();
            var agentWaypointsArray = new List<Transform>();
            foreach (Transform transformItem in agentSelected.transform)
            {
                agentWaypointsArray.Add(transformItem);
            }
                
            if (agentWaypointsArray.Count > 0)
            {
                for (var x = 0; x < agentWaypointsArray.Count; x++)
                {
                    agentSelected.waypoints[x] = agentWaypointsArray[x].position;
                }

                for (int y = 0; y < agentWaypointsArray.Count; y++)
                {
                    DestroyImmediate(agentWaypointsArray[y].gameObject);
                }

                agentWaypointsArray.Clear();

                EditorUtility.SetDirty(agentSelected);
            }
        }
    }

    private void CreateExistingWaypoints(DotsAgent agent, List<Transform> wpArray, Transform waypointsContainer)
    {
        if (wpArray.Count < 1)
        {
            for (int i = 0; i < agent.waypoints.Count; i++)
            {
                var existingWp = CreateWaypoint(waypointsContainer.transform, agent.waypoints[i]);
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
