using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections;
using System.Text;
using UnityEngine.UI;

namespace MultiProjectorWarpSystem {
    [CustomEditor(typeof(ProjectionMesh))]
    [CanEditMultipleObjects]
    public class ProjectionMeshEditor : Editor
    {
        ProjectionMesh myScript;
        public bool showReferenceGameObjects = false;

        void OnEnable()
        {
            myScript = (ProjectionMesh)target;
        }

        public override void OnInspectorGUI()
        {

            DrawDefaultInspector();

            serializedObject.Update();
            myScript = (ProjectionMesh)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Reference Camera", EditorStyles.boldLabel);
            
            float planeDistance = EditorGUILayout.FloatField("Plane Distance", myScript.planeDistance);
            planeDistance = Mathf.Clamp(planeDistance, myScript.targetCamera.nearClipPlane + 0.01f, myScript.targetCamera.farClipPlane - 0.01f);
            myScript.planeDistance = planeDistance;

            float indexAppearDuration = EditorGUILayout.FloatField("Index Appear Duration", myScript.indexAppearDuration);
            indexAppearDuration = Mathf.Clamp(indexAppearDuration, 0, float.MaxValue);
            myScript.indexAppearDuration = indexAppearDuration;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Edit Mode", EditorStyles.boldLabel);
            myScript.editMode = (ProjectionMesh.MeshEditMode)EditorGUILayout.EnumPopup("Mesh Edit Mode", myScript.editMode);
            int selectedVertex = EditorGUILayout.IntField("Selected Vertex", myScript.selectedVertex);
            selectedVertex = Mathf.Clamp(selectedVertex, -1, (myScript.xDivisions + 1) * (myScript.yDivisions + 1)-1);
            myScript.selectedVertex = selectedVertex;
            myScript.selectionActive = EditorGUILayout.Toggle("Selection Active", myScript.selectionActive);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            switch (myScript.editMode)
            {
                case ProjectionMesh.MeshEditMode.CORNERS:
                    EditorGUILayout.LabelField("Corner Offsets", EditorStyles.boldLabel);
                    for(int i = 0; i < 4; i++)
                    {
                        myScript.cornerOffset[i] = EditorGUILayout.Vector2Field("Corner Vertex "+i, myScript.cornerOffset[i]);
                    }
                    break;
                case ProjectionMesh.MeshEditMode.ROWS:
                    /*
                    EditorGUILayout.LabelField("Row Offsets", EditorStyles.boldLabel);
                    for (int i = 0; i < myScript.yDivisions+1; i++)
                    {
                        myScript.rowOffset[i] = EditorGUILayout.Vector2Field("Row " + i, myScript.rowOffset[i]);
                    }
                    */
                    EditorGUILayout.LabelField("Point Offsets", EditorStyles.boldLabel);
                    for (int i = 0; i < myScript.yDivisions + 1; i++)
                    {
                        for (int j = 0; j < myScript.xDivisions + 1; j++)
                        {
                            int index = (i * (myScript.xDivisions + 1)) + j;
                            myScript.pointOffset[index] = EditorGUILayout.Vector2Field("Point (" + i + "," + j + ")", myScript.pointOffset[index]);
                        }
                    }
                    break;
                case ProjectionMesh.MeshEditMode.COLUMNS:
                    /*
                    EditorGUILayout.LabelField("Column Offsets", EditorStyles.boldLabel);
                    for (int i = 0; i < myScript.xDivisions + 1; i++)
                    {
                        myScript.columnOffset[i] = EditorGUILayout.Vector2Field("Column " + i, myScript.columnOffset[i]);
                    }
                    */
                    EditorGUILayout.LabelField("Point Offsets", EditorStyles.boldLabel);
                    for (int i = 0; i < myScript.yDivisions + 1; i++)
                    {
                        for (int j = 0; j < myScript.xDivisions + 1; j++)
                        {
                            int index = (i * (myScript.xDivisions + 1)) + j;
                            myScript.pointOffset[index] = EditorGUILayout.Vector2Field("Point (" + i + "," + j + ")", myScript.pointOffset[index]);
                        }
                    }
                    break;
                case ProjectionMesh.MeshEditMode.POINTS:
                    EditorGUILayout.LabelField("Point Offsets", EditorStyles.boldLabel);
                    for (int i = 0; i < myScript.yDivisions + 1; i++)
                    {
                        for (int j = 0; j < myScript.xDivisions + 1; j++)
                        {
                            int index = (i * (myScript.xDivisions+1)) + j;
                            myScript.pointOffset[index] = EditorGUILayout.Vector2Field("Point (" + i + ","+ j+")", myScript.pointOffset[index]);
                        }
                    }

                    break;
                default:
                    break;
            }
            EditorGUILayout.Space();
            if (GUILayout.Button("Reset All Offsets"))
            {
                myScript.ResetOffsets();
            }
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Fade Controls", EditorStyles.boldLabel);

            float topFadeRange = EditorGUILayout.FloatField("Top Fade Range", myScript.topFadeRange);
            topFadeRange = Mathf.Clamp(topFadeRange, 0f, 1f);
            myScript.topFadeRange = topFadeRange;

            float topFadeChoke = EditorGUILayout.FloatField("Top Fade Choke", myScript.topFadeChoke);
            topFadeChoke = Mathf.Clamp(topFadeChoke, 0f, 0.999f);
            myScript.topFadeChoke = topFadeChoke;

            float bottomFadeRange = EditorGUILayout.FloatField("Bottom Fade Range", myScript.bottomFadeRange); ;
            bottomFadeRange = Mathf.Clamp(bottomFadeRange, 0f, 1f);
            myScript.bottomFadeRange = bottomFadeRange;

            float bottomFadeChoke = EditorGUILayout.FloatField("Bottom Fade Choke", myScript.bottomFadeChoke);
            bottomFadeChoke = Mathf.Clamp(bottomFadeChoke, 0f, 0.999f);
            myScript.bottomFadeChoke = bottomFadeChoke;

            float leftFadeRange = EditorGUILayout.FloatField("Left Fade Range", myScript.leftFadeRange);
            leftFadeRange = Mathf.Clamp(leftFadeRange, 0f, 1f);
            myScript.leftFadeRange = leftFadeRange;

            float leftFadeChoke = EditorGUILayout.FloatField("Left Fade Choke", myScript.leftFadeChoke);
            leftFadeChoke = Mathf.Clamp(leftFadeChoke, 0f, 0.999f);
            myScript.leftFadeChoke = leftFadeChoke;

            float rightFadeRange = EditorGUILayout.FloatField("Right Fade Range", myScript.rightFadeRange);
            rightFadeRange = Mathf.Clamp(rightFadeRange, 0f, 1f);
            myScript.rightFadeRange = rightFadeRange;

            float rightFadeChoke = EditorGUILayout.FloatField("Right Fade Choke", myScript.rightFadeChoke);
            rightFadeChoke = Mathf.Clamp(rightFadeChoke, 0f, 0.999f);
            myScript.rightFadeChoke = rightFadeChoke;
            

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("White Balance", EditorStyles.boldLabel);
            myScript.tint = EditorGUILayout.ColorField("Tint", myScript.tint);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Grid", EditorStyles.boldLabel);
            myScript.unselectedGridLineMaterial = (Material)EditorGUILayout.ObjectField("Unselected Grid Line Material", myScript.unselectedGridLineMaterial, typeof(Material),true);
            myScript.selectedGridLineMaterial = (Material)EditorGUILayout.ObjectField("Selected Grid Line Material", myScript.selectedGridLineMaterial, typeof(Material), true);
            myScript.activeGridLineMaterial = (Material)EditorGUILayout.ObjectField("Active Grid Line Material", myScript.activeGridLineMaterial, typeof(Material), true);
            /*
            myScript.unselectedLineColor = EditorGUILayout.ColorField("Unselected Line Color", myScript.unselectedLineColor);
            myScript.selectedLineColor = EditorGUILayout.ColorField("Selected Line Color", myScript.selectedLineColor);
            myScript.activeLineColor = EditorGUILayout.ColorField("Active Line Color", myScript.activeLineColor);
            */
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Custom Options", EditorStyles.boldLabel);
            
            myScript.showGrid = EditorGUILayout.Toggle("Show Grid", myScript.showGrid);
            myScript.showSelectedGrid = EditorGUILayout.Toggle("Show Selected Grid", myScript.showSelectedGrid);
            myScript.showControlPoints = EditorGUILayout.Toggle("Show Control Points", myScript.showControlPoints);
            myScript.showSelectedControlPoints = EditorGUILayout.Toggle("Show Selected Control Points", myScript.showSelectedControlPoints);
            
            if (myScript.showControlPoints) myScript.ShowControlPoints();
            else myScript.HideControlPoints();
            
            EditorGUILayout.Space();
            GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout);
            foldoutStyle.fontStyle = FontStyle.Bold;
            showReferenceGameObjects = EditorGUILayout.Foldout(showReferenceGameObjects, "Reference Game Objects", foldoutStyle);
            if (showReferenceGameObjects)
            {
                myScript.targetCamera = (Camera)EditorGUILayout.ObjectField("Target Camera", myScript.targetCamera, typeof(Camera), true);
                myScript.projectorIndexText = (Text)EditorGUILayout.ObjectField("Projector Index Text", myScript.projectorIndexText, typeof(Text), true);
                myScript.controlPointsContainer = (Transform)EditorGUILayout.ObjectField("Control Points Container", myScript.controlPointsContainer, typeof(Transform), true);
                myScript.baseRowLinesContainer = (Transform)EditorGUILayout.ObjectField("Base Row Lines", myScript.baseRowLinesContainer, typeof(Transform), true);
                myScript.baseColumnLinesContainer = (Transform)EditorGUILayout.ObjectField("Base Column Lines", myScript.baseColumnLinesContainer, typeof(Transform), true);
                myScript.selectedRowLinesContainer = (Transform)EditorGUILayout.ObjectField("Selected Row Lines", myScript.selectedRowLinesContainer, typeof(Transform), true);
                myScript.selectedColumnLinesContainer = (Transform)EditorGUILayout.ObjectField("Selected Column Lines", myScript.selectedColumnLinesContainer, typeof(Transform), true);
                myScript.meshFilter = (MeshFilter)EditorGUILayout.ObjectField("Mesh Filter", myScript.meshFilter, typeof(MeshFilter), true);
                myScript.selectedControlPoint = (ControlPoint)EditorGUILayout.ObjectField("Selected Control Point", myScript.selectedControlPoint, typeof(ControlPoint), true);
            }


            if (GUI.changed)
            {
                myScript.Rebuild();

                //EditorUtility.SetDirty(myScript);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

}