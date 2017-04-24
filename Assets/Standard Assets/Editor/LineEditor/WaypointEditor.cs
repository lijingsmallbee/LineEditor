﻿/*  This file is part of the "Simple Waypoint System" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SWS
{
    /// <summary>
    /// Waypoint and path creation editor.
    /// <summary>
    [CustomEditor(typeof(WaypointManager))]
    public class WaypointEditor : Editor
    {
        //manager reference
        private WaypointManager script;
        //if we are placing new waypoints in editor
        private bool placing = false;
        //new path gameobject
        private GameObject path;
        //new path name
        private string pathName = "";
        //enables 2D mode placement (auto-detection)
        private bool mode2D = false;
        //Path Manager reference for editing waypoints
        private PathManager pathMan;
        //temporary list for editor created waypoints in a path
        private List<GameObject> wpList = new List<GameObject>();   

        //path type selection enum
        private enum PathType
        {
            standard,
            bezier
        }
        private PathType pathType = PathType.standard;


        public void OnSceneGUI()
        {
            //with creation mode enabled, place new waypoints on keypress
          //  if (Event.current.type == EventType.keyDown && Event.current.keyCode == KeyCode.P && placing)
			if(Event.current.type == EventType.MouseDown &&
			   Event.current.button == 0 && placing)
            {
                //cast a ray against mouse position
                Ray worldRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                RaycastHit hitInfo;

                //2d placement
                if (mode2D)
                {
                    Event.current.Use();
                    //convert screen to 2d position
                    Vector3 pos2D = worldRay.origin;
                    pos2D.z = 0;

                    //place a waypoint at clicked point
                    if (pathMan is BezierPathManager)
                        PlaceBezierPoint(pos2D);
                    else
                        PlaceWaypoint(pos2D);
                }
                else
                {
                    //3d placement
					GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
					Event.current.Use();
					bool buseParentColl = false;
					if(script.transform.parent != null)
					{
						MeshCollider collider = script.transform.parent.gameObject.GetComponent<MeshCollider>();
						if(collider)
						{
							buseParentColl = collider.Raycast(worldRay,out hitInfo,1000f);
							if(buseParentColl)
							{
								if (pathMan is BezierPathManager)
									PlaceBezierPoint(hitInfo.point);
								else
									PlaceWaypoint(hitInfo.point);
							}
						}
					}
                    int layermask = 1 << LayerMask.NameToLayer("Ground") | 1 << LayerMask.NameToLayer("Shadow");

                    if (!buseParentColl && Physics.Raycast(worldRay, out hitInfo,float.PositiveInfinity,layermask))
                    {
                        Event.current.Use();

                        //place a waypoint at clicked point
                        if (pathMan is BezierPathManager)
                            PlaceBezierPoint(hitInfo.point);
                        else
                            PlaceWaypoint(hitInfo.point);
                    }
                    else
                    {
                        Debug.LogWarning("Waypoint Manager: 3D Mode. Trying to place a waypoint but couldn't "
                                         + "find valid target. Have you clicked on a collider?");
                    }
                }
            }
        }


        public override void OnInspectorGUI()
        {
            //show default variables of manager
            DrawDefaultInspector();
            //get manager reference
            script = (WaypointManager)target;
            
            //get sceneview to auto-detect 2D mode
            SceneView view = SceneView.currentDrawingSceneView;
            if (view == null)
                view = EditorWindow.GetWindow<SceneView>("Scene",false);
            mode2D = view.in2DMode;

            EditorGUIUtility.LookLikeControls();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            //draw path text label
            GUILayout.Label("Enter Path Name: ", GUILayout.Height(15));
            //display text field for creating a path with that name
            pathName = EditorGUILayout.TextField(pathName, GUILayout.Height(15));

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            //draw path type selection enum
            GUILayout.Label("Select Path Type: ", GUILayout.Height(15));
            pathType = (PathType)EditorGUILayout.EnumPopup(pathType);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            //display label of current mode
            if (mode2D)
                GUILayout.Label("2D Mode Detected.", GUILayout.Height(15));
            else
                GUILayout.Label("3D Mode Detected.", GUILayout.Height(15));
            EditorGUILayout.Space();

            //draw path creation button
            if (!placing && GUILayout.Button("Start Path", GUILayout.Height(40)))
            {
                if (pathName == "")
                {
                    Debug.LogWarning("No path name defined. Cancelling.");
                    return;
                }

                if (script.transform.FindChild(pathName) != null)
                {
                    Debug.LogWarning("Path name already given. Cancelling.");
                    return;
                }

                //create a new container transform which will hold all new waypoints
                path = new GameObject(pathName);
                //reset position and parent container gameobject to this manager gameobject
                path.transform.position = script.gameObject.transform.position;
                path.transform.parent = script.gameObject.transform;
                StartPath();

                //we passed all prior checks, toggle waypoint placement
                placing = true;
                //focus sceneview for placement
				if(view != null)
				{
					view.Focus();
				}
             //   SceneView.currentDrawingSceneView.Focus();
            }

			if (!placing && GUILayout.Button("Export Path", GUILayout.Height(40)))
			{
				
				string filepath = EditorUtility.SaveFilePanelInProject("Save Path","PathInfo","csv","OKOK");
                Debug.Log(filepath);
				string fileName = filepath;//文件名字
				
				StringBuilder sb = new StringBuilder();
				//offset
				sb.Append("patrolId").Append(',');
				sb.Append("patrolPlan").Append(','); 
				sb.Append("patrolX").Append(',');
				sb.Append("patrolY").Append("\r\n");
				int totalCount = 0;
				PathManager[] pathes = script.GetComponentsInChildren<PathManager>();
				for(int i=0;i<pathes.Length;++i)
				{
					sb.Append('#').Append(pathes[i].name).Append(',').Append(pathes[i].transform.position.y).Append("\r\n");
					Vector3[] points = pathes[i].GetPathPoints();
					for(int j=0;j<points.Length;++j)
					{
						totalCount ++;
						sb.Append(totalCount).Append(',');
						sb.Append(i).Append(',');
						sb.Append(points[j].x).Append(',');
						sb.Append(points[j].z).Append("\r\n");
					}
				}
				
				//要写的数据源
				SaveTextFile(filepath,sb.ToString());	
			}

			if (!placing && GUILayout.Button("Import", GUILayout.Height(40)))
			{
				string filepath = EditorUtility.OpenFilePanel("Load pathes",Application.dataPath,"csv");
				if(filepath != null)
				{
					StreamReader reader = new StreamReader(filepath,new UTF8Encoding(false));
					if(reader != null)
					{
						string content = reader.ReadToEnd();
						int readPos = 0;
						//skip the first line
						string skipLine = EditorUtils.readLine( content, ref readPos );
						List<string> kv = null;
						PathManager manager = null;
						int pointCount = 0;
						float currentY = 0f;
						while( readPos< content.Length )
						{
							string lineNew = EditorUtils.readLine( content, ref readPos );
							//new path
							if(lineNew[0] == '#')
							{
								int noUse = 0;
                                kv = EditorUtils.readCsvLine(lineNew,ref noUse);
								string readpathName = kv[0].Substring(1);
								float.TryParse(kv[1],out currentY);
								path = new GameObject(readpathName);
								//reset position and parent container gameobject to this manager gameobject
								path.transform.position = script.gameObject.transform.position;
								path.transform.parent = script.gameObject.transform;
								StartPath();
								wpList.Clear();
							}
							else
							{
								int a = 0;
                                kv = EditorUtils.readCsvLine( lineNew,ref a);
								float x = 0f;
								float.TryParse(kv[2],out x);
								float z = 0f;
								float.TryParse(kv[3],out z);
								PlaceWaypoint(new Vector3(x,currentY,z));	
							}
							
						}
					}
				}
			}

			if (!placing && GUILayout.Button("CreateColliders", GUILayout.Height(40)))
			{
				UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath("Assets/PathEditor/Cube.prefab",typeof(UnityEngine.Object));
			    GameObject parent = GameObject.Find("WalkColliders");
				Texture2D tex = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/CameraPath3/Icons/options.png", typeof(Texture2D));
				if(parent == null)
				{
					parent = new GameObject("WalkColliders");
				}
				PathManager[] pathes = script.GetComponentsInChildren<PathManager>();
				for(int i=0;i<pathes.Length;++i)
				{
					Vector3[] points = pathes[i].GetPathPoints();
					int pointC = points.Length;
                    int loopcount = pointC;
                    //不是关闭类型的，不生成最后一个点到初始点的连线
                    if(pathes[i].closure == false)
                    {
                        loopcount = pointC - 1;
                    }
					for(int j=0;j<loopcount;++j)
					{
						Vector3 nextPos = points[(j+1)%pointC];
						Vector3 middlePos = (points[j] + nextPos)*0.5f;
						GameObject collider = GameObject.Instantiate(prefab,middlePos,Quaternion.identity) as GameObject;
						Vector3 lookPos = nextPos;
						lookPos.y = middlePos.y;
						collider.transform.LookAt(lookPos);
						collider.transform.parent = parent.transform;
						Vector3 setScale = collider.transform.localScale;
						setScale.z = (nextPos-points[j]).magnitude;
						collider.transform.localScale = setScale;
					}
				}
			}
            GUI.backgroundColor = Color.yellow;

            //finish path button
            if (placing && GUILayout.Button("Finish Editing", GUILayout.Height(40)))
            {
                if (wpList.Count < 2)
                {
                    Debug.LogWarning("Not enough waypoints placed. Cancelling.");
                    //if we have created a path already, destroy it again
                    if (path) DestroyImmediate(path);
                }

                //toggle placement off
                placing = false;
                //clear list with temporary waypoint references,
                //we only needed this for getting the waypoint count
                wpList.Clear();
                //reset path name input field
                pathName = "";
                //make the new path the active selection
                Selection.activeGameObject = path;
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space();
            //draw instructions
            GUILayout.TextArea("Hint:\nPress 'Start Path' to begin a new path, then press 'p' "
                            + "on your keyboard to place waypoints in the SceneView. In 3D Mode "
                            + "you have to place waypoints onto objects with colliders."
                            + "\n\nPress 'Finish Editing' to end your path.");
        }


        //destroy path when losing editor focus
        void OnDisable()
        {
            if (placing)
            {
                Debug.LogWarning("Waypoint Manager: Lost focus when placing waypoints. Destroying path.");
                if (path) DestroyImmediate(path);
            }
        }


        //differ between path selection
        void StartPath()
        {
            switch (pathType)
            {
                case PathType.standard:
                    pathMan = path.AddComponent<PathManager>();
                    pathMan.waypoints = new Transform[0];
                    break;
                case PathType.bezier:
                    pathMan = path.AddComponent<BezierPathManager>();
                    BezierPathManager thisPath = pathMan as BezierPathManager;
                    thisPath.showHandles = true;
                    thisPath.bPoints = new List<BezierPoint>();
                    break;
            }
        }


        //path manager placement
        void PlaceWaypoint(Vector3 placePos)
        {
            //instantiate waypoint gameobject
            GameObject wayp = new GameObject("Waypoint");

            //with every new waypoint, our waypoints array should increase by 1
            //but arrays gets erased on resize, so we use a classical rule of three
            Transform[] wpCache = new Transform[pathMan.waypoints.Length];
            System.Array.Copy(pathMan.waypoints, wpCache, pathMan.waypoints.Length);

            pathMan.waypoints = new Transform[pathMan.waypoints.Length + 1];
            System.Array.Copy(wpCache, pathMan.waypoints, wpCache.Length);
            pathMan.waypoints[pathMan.waypoints.Length - 1] = wayp.transform;

            //this is executed on placement of the first waypoint:
            //we position our path container transform to the first waypoint position,
            //so the transform (and grab/rotate/scale handles) aren't out of sight
            if (wpList.Count == 0)
                pathMan.transform.position = placePos;

            //position current waypoint at clicked position in scene view
            if (mode2D) placePos.z = 0f;
            wayp.transform.position = placePos;
            //parent it to the defined path 
            wayp.transform.parent = pathMan.transform;
            //add waypoint to temporary list
            wpList.Add(wayp);
            //rename waypoint to match the list count
            wayp.name = "Waypoint " + (wpList.Count - 1);
        }


        //bezier path placement
        void PlaceBezierPoint(Vector3 placePos)
        {
            //create new bezier point property class
            BezierPoint newPoint = new BezierPoint();

            //instantiate waypoint gameobject
            Transform wayp = new GameObject("Waypoint").transform;
            //assign waypoint to the class
            newPoint.wp = wayp;

            //same as above
            if (wpList.Count == 0)
                pathMan.transform.position = placePos;

            //position current waypoint at clicked position in scene view
            if (mode2D) placePos.z = 0f;
            wayp.position = placePos;
            //parent it to the defined path 
            wayp.parent = pathMan.transform;

            //create new array with bezier point handle positions
            Transform left = new GameObject("Left").transform;
            Transform right = new GameObject("Right").transform;
            left.parent = right.parent = wayp;
            left.position = wayp.position + new Vector3(2, 0, 0);
            right.position = wayp.position + new Vector3(-2, 0, 0);
            newPoint.cp = new[] { left, right };

            BezierPathManager thisPath = pathMan as BezierPathManager;
            //add waypoint to the list of waypoints
            thisPath.bPoints.Add(newPoint);
            thisPath.segmentDetail.Add(thisPath.pathDetail);
            //add waypoint to temporary list
            wpList.Add(wayp.gameObject);
            //rename waypoint to match the list count
            wayp.name = "Waypoint " + (wpList.Count - 1);
            //recalculate bezier path
            thisPath.CalculatePath();
        }
		
		static void SaveTextFile(string saveFilePath, string data)
		{
			string folder = System.IO.Path.GetDirectoryName(saveFilePath);
			System.IO.Directory.CreateDirectory(folder);
			
			#if UNITY_WEBPLAYER && !UNITY_EDITOR
			LogManager.LogError("Current build target is set to Web Player. Cannot perform file input/output when in Web Player.");
			#else
			System.IO.StreamWriter write = new System.IO.StreamWriter(saveFilePath, false, new UTF8Encoding(false)); // Unity's TextAsset.text borks when encoding used is UTF8 :(
			write.Write(data);
			write.Flush();
			write.Close();
			write.Dispose();
			#endif
		}
    }
}
