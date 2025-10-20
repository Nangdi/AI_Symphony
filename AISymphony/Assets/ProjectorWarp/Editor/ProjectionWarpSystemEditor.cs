using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections;
using System.Text;


namespace MultiProjectorWarpSystem
{
    [CustomEditor(typeof(ProjectionWarpSystem))]
    [CanEditMultipleObjects]
    public class ProjectionWarpSystemEditor : Editor
    {
        const int MAX_CAMERAS = 8;
        ProjectionWarpSystem myScript;
        public bool showReferenceGameObjects = false;
        public bool showDebug = false;
        public bool showStats = false;

        Vector2 prevProjectorResolution;
        int prevXDivisions;
        int prevYDivisions;
        int prevCameraCount;
        
        float prevOrthographicSizeScale;

        void OnEnable()
        {
            myScript = (ProjectionWarpSystem)target;
        }

        public void Refresh()
        {
            //determine if critical things are changed
            if (myScript.projectorCount > MAX_CAMERAS)
                myScript.projectorCount = MAX_CAMERAS;

            //target camera count is different from current list of cameras
            if (myScript.projectorCount != myScript.sourceCamerasContainer.childCount)
            {
                myScript.DestroyCameras();
                myScript.InitCameras();
            }
            //texture size changed
            if (myScript.projectionCameras.Count > 0)
            {
                if (Mathf.RoundToInt(myScript.renderTextureSize.x) != Mathf.RoundToInt(myScript.projectionCameras[0].width * 100f) ||
                    Mathf.RoundToInt(myScript.renderTextureSize.y) != Mathf.RoundToInt(myScript.projectionCameras[0].height * 100f))
                {
                    myScript.DestroyCameras();
                    myScript.InitCameras();
                }
            }
        }
        public override void OnInspectorGUI()
        {
            //DrawDefaultInspector();
            serializedObject.Update();

            //EditorGUI.BeginChangeCheck();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Initialization File", EditorStyles.boldLabel);
            EditorStyles.label.wordWrap = true;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Leave empty if you do not want to initialize with an initial calibration file.");
            myScript.defaultCalibrationFile = EditorGUILayout.TextField("Startup Calibration File", myScript.defaultCalibrationFile);
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("File IO", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Load Calibration"))
            {
                string[] filters = { "JSON Calibration File", "json" };
                string path = EditorUtility.OpenFilePanelWithFilters("Load Calibration", "", filters);
                myScript.LoadCalibration(path);
            }

            if (GUILayout.Button("Save Calibration"))
            {
                string path = EditorUtility.SaveFilePanelInProject(
                    "Save Calibration",
                    "projector_calibration.json",
                    "json",
                    "Enter a filename for the calibration file");
                myScript.SaveCalibration(path);

                AssetDatabase.Refresh();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout);
            foldoutStyle.fontStyle = FontStyle.Bold;

            EditorGUILayout.LabelField("Visibility", EditorStyles.boldLabel);
            myScript.showMouseCursor = EditorGUILayout.Toggle("Mouse Cursor", myScript.showMouseCursor);
            myScript.showProjectionWarpGUI = EditorGUILayout.Toggle("Projection Warp GUI", myScript.showProjectionWarpGUI);
            myScript.showIconLabels = EditorGUILayout.Toggle("Icon Labels", myScript.showIconLabels);
            
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Projection Settings", EditorStyles.boldLabel);
            ProjectionWarpSystem.CameraArragement cameraArrangement = (ProjectionWarpSystem.CameraArragement)EditorGUILayout.EnumPopup("Arrangement", myScript.arrangement);
            myScript.arrangement = cameraArrangement;

            float orthographicSizeScale = 1f;
            switch (cameraArrangement){
                case ProjectionWarpSystem.CameraArragement.HORIZONTAL_PERSPECTIVE:
                case ProjectionWarpSystem.CameraArragement.VERTICAL_PERSPECTIVE:
                case ProjectionWarpSystem.CameraArragement.HORIZONTAL_PERSPECTIVE_CIRCULAR:
                case ProjectionWarpSystem.CameraArragement.VERTICAL_PERSPECTIVE_CIRCULAR:
                    break;
                case ProjectionWarpSystem.CameraArragement.HORIZONTAL_ORTHOGRAPHIC:
                case ProjectionWarpSystem.CameraArragement.VERTICAL_ORTHOGRAPHIC:
                default:
                    orthographicSizeScale = EditorGUILayout.FloatField("Orthographic Size Scale", myScript.orthographicSizeScale);
                    if (orthographicSizeScale <= 0f) orthographicSizeScale = 0.001f;
                    myScript.orthographicSizeScale = orthographicSizeScale;
                    break;
            }
            
            
            Vector2 projectorResolution = EditorGUILayout.Vector2Field("Projector Resolution", myScript.renderTextureSize);
            if (projectorResolution.x < 1f) projectorResolution.x = 1f;
            else if (projectorResolution.x > 16384f) projectorResolution.x = 16384f;
            if (projectorResolution.y < 1f) projectorResolution.y = 1f;
            else if (projectorResolution.y > 16384f) projectorResolution.y = 16384f;

            myScript.renderTextureSize = projectorResolution;
            myScript.viewportSize = projectorResolution.y / 200f;

            int xDivisions = EditorGUILayout.IntField("X Divisions", myScript.xDivisions);
            xDivisions = Mathf.Clamp(xDivisions, 1, 100);
            myScript.xDivisions = xDivisions;

            int yDivisions = EditorGUILayout.IntField("Y Divisions", myScript.yDivisions);
            yDivisions = Mathf.Clamp(yDivisions, 1, 100);
            myScript.yDivisions = yDivisions;

            int firstProjector;

            switch(cameraArrangement){
                case ProjectionWarpSystem.CameraArragement.HORIZONTAL_PERSPECTIVE_CIRCULAR:
                case ProjectionWarpSystem.CameraArragement.VERTICAL_PERSPECTIVE_CIRCULAR:
                    firstProjector = EditorGUILayout.IntSlider("First Projector", myScript.firstProjector, 1, 6);
                    firstProjector = Mathf.Clamp(firstProjector, 1, 6);
                    myScript.firstProjector = firstProjector;
                    break;
                case ProjectionWarpSystem.CameraArragement.HORIZONTAL_PERSPECTIVE:
                case ProjectionWarpSystem.CameraArragement.VERTICAL_PERSPECTIVE:
                case ProjectionWarpSystem.CameraArragement.HORIZONTAL_ORTHOGRAPHIC:
                case ProjectionWarpSystem.CameraArragement.VERTICAL_ORTHOGRAPHIC:
                default:
                    firstProjector = EditorGUILayout.IntSlider("First Projector", myScript.firstProjector, 1, 8);
                    firstProjector = Mathf.Clamp(firstProjector, 1, 8);
                    myScript.firstProjector = firstProjector;
                    break;
            }
            

            int projectorCount;
            
            switch(cameraArrangement){
                case ProjectionWarpSystem.CameraArragement.HORIZONTAL_PERSPECTIVE_CIRCULAR:
                case ProjectionWarpSystem.CameraArragement.VERTICAL_PERSPECTIVE_CIRCULAR:
                    projectorCount = EditorGUILayout.IntSlider("Projector Count", myScript.projectorCount, 3, 8 - firstProjector + 1);
                    projectorCount = Mathf.Clamp(projectorCount, 3, 8 - firstProjector + 1);
                    myScript.projectorCount = projectorCount;
                    break;
                case ProjectionWarpSystem.CameraArragement.HORIZONTAL_PERSPECTIVE:
                case ProjectionWarpSystem.CameraArragement.VERTICAL_PERSPECTIVE:
                case ProjectionWarpSystem.CameraArragement.HORIZONTAL_ORTHOGRAPHIC:
                case ProjectionWarpSystem.CameraArragement.VERTICAL_ORTHOGRAPHIC:
                default:
                    projectorCount = EditorGUILayout.IntSlider("Projector Count", myScript.projectorCount, 1, 8 - firstProjector + 1);
                    projectorCount = Mathf.Clamp(projectorCount, 1, 8 - firstProjector + 1);
                    myScript.projectorCount = projectorCount;
                    break;
            }
            

            bool regenerateCamera = EditorGUILayout.Toggle("Regenerate Cameras", myScript.regenerateCamera);
            myScript.regenerateCamera = regenerateCamera;

            bool reverseOrdering = EditorGUILayout.Toggle("Reverse Ordering", myScript.reverseOrdering);
            myScript.reverseOrdering = reverseOrdering;
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Distances", EditorStyles.boldLabel);

            float near = EditorGUILayout.FloatField("Near", myScript.near);
            if (near <= 0f) near = 0.001f;
            myScript.near = near;

            float far = EditorGUILayout.FloatField("Far", myScript.far);
            if (far <= near) far = near + 0.001f;
            myScript.far = far;

            float overlap = 0f;
            float projectionCameraSpace = 0f;

            if (myScript.projectorCount > 1)
            {
                switch (cameraArrangement)
                {
                    case ProjectionWarpSystem.CameraArragement.HORIZONTAL_ORTHOGRAPHIC:
                        overlap = EditorGUILayout.FloatField("Overlap", myScript.overlap.x);
                        overlap = Mathf.Clamp(overlap, 0f, projectorResolution.x / 100f);
                        myScript.calibrationManager.overlapSlider.maxValue = projectorResolution.x / 100f;
                        myScript.calibrationManager.overlapSlider.value = overlap;
                        myScript.calibrationManager.overlapInputField.text = overlap.ToString();
                        myScript.overlap = new Vector2(overlap, 0f);
                        break;
                    case ProjectionWarpSystem.CameraArragement.VERTICAL_ORTHOGRAPHIC:
                        overlap = EditorGUILayout.FloatField("Overlap", myScript.overlap.y);
                        overlap = Mathf.Clamp(overlap, 0f, projectorResolution.y / 100f);
                        myScript.calibrationManager.overlapSlider.maxValue = projectorResolution.y / 100f;
                        myScript.calibrationManager.overlapSlider.value = overlap;
                        myScript.calibrationManager.overlapInputField.text = overlap.ToString();
                        myScript.overlap = new Vector2(0f, overlap);
                        break;
                    case ProjectionWarpSystem.CameraArragement.HORIZONTAL_PERSPECTIVE:
                        overlap = EditorGUILayout.FloatField("Overlap", myScript.overlap.x);
                        overlap = Mathf.Clamp(overlap, 0f, myScript.fieldOfView - (myScript.fieldOfView / myScript.sourceCameras.Count));
                        myScript.calibrationManager.overlapSlider.maxValue = myScript.fieldOfView - (myScript.fieldOfView / myScript.sourceCameras.Count);
                        myScript.calibrationManager.overlapSlider.value = overlap;
                        myScript.calibrationManager.overlapInputField.text = overlap.ToString();
                        myScript.overlap = new Vector2(overlap, 0f);
                        break;
                    case ProjectionWarpSystem.CameraArragement.VERTICAL_PERSPECTIVE:
                        overlap = EditorGUILayout.FloatField("Overlap", myScript.overlap.y);
                        overlap = Mathf.Clamp(overlap, 0f, myScript.fieldOfView - (myScript.fieldOfView / myScript.sourceCameras.Count));
                        myScript.calibrationManager.overlapSlider.maxValue = myScript.fieldOfView - (myScript.fieldOfView / myScript.sourceCameras.Count);
                        myScript.calibrationManager.overlapSlider.value = overlap;
                        myScript.calibrationManager.overlapInputField.text = overlap.ToString();
                        myScript.overlap = new Vector2(0f, overlap);
                        break;
                    case ProjectionWarpSystem.CameraArragement.HORIZONTAL_PERSPECTIVE_CIRCULAR:
                        overlap = EditorGUILayout.FloatField("Overlap", myScript.overlap.x);
                        overlap = Mathf.Clamp(overlap, 0f, (360f / (float)myScript.projectorCount) / 2f);
                        myScript.calibrationManager.overlapSlider.maxValue = (360f / (float)myScript.projectorCount) / 2f;
                        myScript.calibrationManager.overlapSlider.value = overlap;
                        myScript.calibrationManager.overlapInputField.text = overlap.ToString();
                        myScript.overlap = new Vector2(overlap, 0f);
                        break;
                    case ProjectionWarpSystem.CameraArragement.VERTICAL_PERSPECTIVE_CIRCULAR:
                        overlap = EditorGUILayout.FloatField("Overlap", myScript.overlap.y);
                        overlap = Mathf.Clamp(overlap, 0f, (360f / (float)myScript.projectorCount) / 2f);
                        myScript.calibrationManager.overlapSlider.maxValue = (360f / (float)myScript.projectorCount) / 2f;
                        myScript.calibrationManager.overlapSlider.value = overlap;
                        myScript.calibrationManager.overlapInputField.text = overlap.ToString();
                        myScript.overlap = new Vector2(0f, overlap);
                        break;
                    default:
                        break;
                }

                projectionCameraSpace = EditorGUILayout.FloatField("Projection Camera Space", myScript.projectionCameraSpace);
                if (projectionCameraSpace < 0) projectionCameraSpace = 0f;
                myScript.projectionCameraSpace = projectionCameraSpace;
            }
            else
            {
                myScript.overlap = Vector2.zero;
            }


            float fieldOfView = 0f;

            switch (cameraArrangement)
            {
                case ProjectionWarpSystem.CameraArragement.HORIZONTAL_PERSPECTIVE:
                case ProjectionWarpSystem.CameraArragement.VERTICAL_PERSPECTIVE:
                    fieldOfView = EditorGUILayout.FloatField("Field Of View", myScript.fieldOfView);
                    myScript.fieldOfView = fieldOfView;
                    break;
                default:
                    break;
            }


            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Edit Mode (Single Mesh)", EditorStyles.boldLabel);

            int selectedMesh = EditorGUILayout.IntSlider("Selected Mesh", myScript.selectedMesh, -1, projectorCount - 1);
            selectedMesh = Mathf.Clamp(selectedMesh, -1, projectorCount - 1);
            myScript.selectedMesh = selectedMesh;
            
            if (selectedMesh >= 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Visibility", EditorStyles.boldLabel);

                bool showGrid = EditorGUILayout.Toggle("Show Grid", myScript.projectionCameras[myScript.selectedMesh].showGrid);
                if (showGrid) myScript.projectionCameras[myScript.selectedMesh].ShowBaseGrid();
                else myScript.projectionCameras[myScript.selectedMesh].HideBaseGrid();

                bool showSelectedGrid = EditorGUILayout.Toggle("Show Selected Grid", myScript.projectionCameras[myScript.selectedMesh].showSelectedGrid);
                if (showSelectedGrid) myScript.projectionCameras[myScript.selectedMesh].ShowSelectedGrid();
                else myScript.projectionCameras[myScript.selectedMesh].HideSelectedGrid();

                bool showControlPoints = EditorGUILayout.Toggle("Show Control Points", myScript.projectionCameras[myScript.selectedMesh].showControlPoints);
                if (showControlPoints) myScript.projectionCameras[myScript.selectedMesh].ShowControlPoints();
                else myScript.projectionCameras[myScript.selectedMesh].HideControlPoints();

                bool showSelectedControlPoints = EditorGUILayout.Toggle("Show Selected Control Points", myScript.projectionCameras[myScript.selectedMesh].showSelectedControlPoints);
                if (showSelectedControlPoints) myScript.projectionCameras[myScript.selectedMesh].ShowSelectedControlPoints();
                else myScript.projectionCameras[myScript.selectedMesh].HideSelectedControlPoints();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Fade Controls", EditorStyles.boldLabel);

                #region TOP FADE
                float topFadeRange = EditorGUILayout.FloatField("Top Fade Range", myScript.projectionCameras[myScript.selectedMesh].topFadeRange);
                topFadeRange = Mathf.Clamp(topFadeRange, 0f, 1f);
                myScript.projectionCameras[myScript.selectedMesh].topFadeRange = topFadeRange;
                
                myScript.calibrationManager.topRangeInputField.text = topFadeRange.ToString();
                myScript.calibrationManager.topRangeSlider.value = topFadeRange;

                float topFadeChoke = EditorGUILayout.FloatField("Top Fade Choke", myScript.projectionCameras[myScript.selectedMesh].topFadeChoke);
                topFadeChoke = Mathf.Clamp(topFadeChoke, 0f, 1f);
                myScript.projectionCameras[myScript.selectedMesh].topFadeChoke = topFadeChoke;

                myScript.calibrationManager.topChokeInputField.text = topFadeChoke.ToString();
                myScript.calibrationManager.topChokeSlider.value = topFadeChoke;

                float topFadeGamma = EditorGUILayout.FloatField("Top Fade Gamma", myScript.projectionCameras[myScript.selectedMesh].topFadeGamma);
                topFadeGamma = Mathf.Clamp(topFadeGamma, 0f, 4f);
                myScript.projectionCameras[myScript.selectedMesh].topFadeGamma = topFadeGamma;

                myScript.calibrationManager.topGammaInputField.text = topFadeGamma.ToString();
                myScript.calibrationManager.topGammaSlider.value = topFadeGamma;

                #endregion

                #region BOTTOM FADE
                float bottomFadeRange = EditorGUILayout.FloatField("Bottom Fade Range", myScript.projectionCameras[myScript.selectedMesh].bottomFadeRange);
                bottomFadeRange = Mathf.Clamp(bottomFadeRange, 0f, 1f);
                myScript.projectionCameras[myScript.selectedMesh].bottomFadeRange = bottomFadeRange;

                myScript.calibrationManager.bottomRangeInputField.text = bottomFadeRange.ToString();
                myScript.calibrationManager.bottomRangeSlider.value = bottomFadeRange;

                float bottomFadeChoke = EditorGUILayout.FloatField("Bottom Fade Choke", myScript.projectionCameras[myScript.selectedMesh].bottomFadeChoke);
                bottomFadeChoke = Mathf.Clamp(bottomFadeChoke, 0f, 1f);
                myScript.projectionCameras[myScript.selectedMesh].bottomFadeChoke = bottomFadeChoke;

                myScript.calibrationManager.bottomChokeInputField.text = bottomFadeChoke.ToString();
                myScript.calibrationManager.bottomChokeSlider.value = bottomFadeChoke;

                float bottomFadeGamma = EditorGUILayout.FloatField("Bottom Fade Gamma", myScript.projectionCameras[myScript.selectedMesh].bottomFadeGamma);
                bottomFadeGamma = Mathf.Clamp(bottomFadeGamma, 0f, 4f);
                myScript.projectionCameras[myScript.selectedMesh].bottomFadeGamma = bottomFadeGamma;

                myScript.calibrationManager.bottomGammaInputField.text = bottomFadeGamma.ToString();
                myScript.calibrationManager.bottomGammaSlider.value = bottomFadeGamma;

                #endregion

                #region LEFT FADE

                float leftFadeRange = EditorGUILayout.FloatField("Left Fade Range", myScript.projectionCameras[myScript.selectedMesh].leftFadeRange);
                leftFadeRange = Mathf.Clamp(leftFadeRange, 0f, 1f);
                myScript.projectionCameras[myScript.selectedMesh].leftFadeRange = leftFadeRange;

                myScript.calibrationManager.leftRangeInputField.text = leftFadeRange.ToString();
                myScript.calibrationManager.leftRangeSlider.value = leftFadeRange;

                float leftFadeChoke = EditorGUILayout.FloatField("Left Fade Choke", myScript.projectionCameras[myScript.selectedMesh].leftFadeChoke);
                leftFadeChoke = Mathf.Clamp(leftFadeChoke, 0f, 1f);
                myScript.projectionCameras[myScript.selectedMesh].leftFadeChoke = leftFadeChoke;

                myScript.calibrationManager.leftChokeInputField.text = leftFadeChoke.ToString();
                myScript.calibrationManager.leftChokeSlider.value = leftFadeChoke;

                float leftFadeGamma = EditorGUILayout.FloatField("Left Fade Gamma", myScript.projectionCameras[myScript.selectedMesh].leftFadeGamma);
                leftFadeGamma = Mathf.Clamp(leftFadeGamma, 0f, 4f);
                myScript.projectionCameras[myScript.selectedMesh].leftFadeGamma = leftFadeGamma;

                myScript.calibrationManager.leftGammaInputField.text = leftFadeGamma.ToString();
                myScript.calibrationManager.leftGammaSlider.value = leftFadeGamma;

                #endregion

                #region RIGHT FADE

                float rightFadeRange = EditorGUILayout.FloatField("Right Fade Range", myScript.projectionCameras[myScript.selectedMesh].rightFadeRange);
                rightFadeRange = Mathf.Clamp(rightFadeRange, 0f, 1f);
                myScript.projectionCameras[myScript.selectedMesh].rightFadeRange = rightFadeRange;

                myScript.calibrationManager.rightRangeInputField.text = rightFadeRange.ToString();
                myScript.calibrationManager.rightRangeSlider.value = rightFadeRange;

                float rightFadeChoke = EditorGUILayout.FloatField("Right Fade Choke", myScript.projectionCameras[myScript.selectedMesh].rightFadeChoke);
                rightFadeChoke = Mathf.Clamp(rightFadeChoke, 0f, 1f);
                myScript.projectionCameras[myScript.selectedMesh].rightFadeChoke = rightFadeChoke;

                myScript.calibrationManager.rightChokeInputField.text = rightFadeChoke.ToString();
                myScript.calibrationManager.rightChokeSlider.value = rightFadeChoke;

                float rightFadeGamma = EditorGUILayout.FloatField("Right Fade Gamma", myScript.projectionCameras[myScript.selectedMesh].rightFadeGamma);
                rightFadeGamma = Mathf.Clamp(rightFadeGamma, 0f, 4f);
                myScript.projectionCameras[myScript.selectedMesh].rightFadeGamma = rightFadeGamma;

                myScript.calibrationManager.rightGammaInputField.text = rightFadeGamma.ToString();
                myScript.calibrationManager.rightGammaSlider.value = rightFadeGamma;

                #endregion

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("White Balance", EditorStyles.boldLabel);
                myScript.projectionCameras[myScript.selectedMesh].tint = EditorGUILayout.ColorField("Tint", myScript.projectionCameras[myScript.selectedMesh].tint); ;

                myScript.projectionCameras[myScript.selectedMesh].UpdateBlend();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Grid Controls", EditorStyles.boldLabel);
                int selectedVertex = EditorGUILayout.IntSlider("Selected Vertex", myScript.projectionCameras[selectedMesh].selectedVertex, -1, myScript.projectionCameras[selectedMesh].vertices.Length - 1);
                selectedVertex = Mathf.Clamp(selectedVertex, -1, myScript.projectionCameras[selectedMesh].vertices.Length - 1);
                myScript.projectionCameras[selectedMesh].selectedVertex = selectedVertex;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                myScript.projectionCameras[selectedMesh].editMode = (ProjectionMesh.MeshEditMode)EditorGUILayout.EnumPopup("Mesh Edit Mode", myScript.projectionCameras[selectedMesh].editMode);
                switch (myScript.projectionCameras[selectedMesh].editMode)
                {
                    case ProjectionMesh.MeshEditMode.CORNERS:
                        EditorGUILayout.LabelField("Corner Offsets", EditorStyles.boldLabel);
                        for (int i = 0; i < 4; i++)
                        {
                            myScript.projectionCameras[myScript.selectedMesh].cornerOffset[i] = EditorGUILayout.Vector2Field("Corner Vertex " + i, myScript.projectionCameras[myScript.selectedMesh].cornerOffset[i]);
                        }
                        break;
                    case ProjectionMesh.MeshEditMode.ROWS:
                        /*
                        EditorGUILayout.LabelField("Row Offsets", EditorStyles.boldLabel);
                        for (int i = 0; i < myScript.yDivisions + 1; i++)
                        {
                            myScript.projectionCameras[myScript.selectedMesh].rowOffset[i] = EditorGUILayout.Vector2Field("Row " + i, myScript.projectionCameras[myScript.selectedMesh].rowOffset[i]);
                        }
                        */
                        EditorGUILayout.LabelField("Point Offsets", EditorStyles.boldLabel);
                        for (int i = 0; i < myScript.yDivisions + 1; i++)
                        {
                            for (int j = 0; j < myScript.xDivisions + 1; j++)
                            {
                                int index = (i * (myScript.xDivisions + 1)) + j;
                                myScript.projectionCameras[myScript.selectedMesh].pointOffset[index] = EditorGUILayout.Vector2Field("Point (" + i + "," + j + ")", myScript.projectionCameras[myScript.selectedMesh].pointOffset[index]);
                            }
                        }
                        break;
                    case ProjectionMesh.MeshEditMode.COLUMNS:
                        /*
                        EditorGUILayout.LabelField("Column Offsets", EditorStyles.boldLabel);
                        for (int i = 0; i < myScript.xDivisions + 1; i++)
                        {
                            myScript.projectionCameras[myScript.selectedMesh].columnOffset[i] = EditorGUILayout.Vector2Field("Column " + i, myScript.projectionCameras[myScript.selectedMesh].columnOffset[i]);
                        }
                        */
                        EditorGUILayout.LabelField("Point Offsets", EditorStyles.boldLabel);
                        for (int i = 0; i < myScript.yDivisions + 1; i++)
                        {
                            for (int j = 0; j < myScript.xDivisions + 1; j++)
                            {
                                int index = (i * (myScript.xDivisions + 1)) + j;
                                myScript.projectionCameras[myScript.selectedMesh].pointOffset[index] = EditorGUILayout.Vector2Field("Point (" + i + "," + j + ")", myScript.projectionCameras[myScript.selectedMesh].pointOffset[index]);
                            }
                        }
                        break;
                    case ProjectionMesh.MeshEditMode.POINTS:
                        EditorGUILayout.LabelField("Point Offsets", EditorStyles.boldLabel);
                        for (int i = 0; i < myScript.yDivisions + 1; i++)
                        {
                            for (int j = 0; j < myScript.xDivisions + 1; j++)
                            {
                                int index = (i * (myScript.xDivisions + 1)) + j;
                                myScript.projectionCameras[myScript.selectedMesh].pointOffset[index] = EditorGUILayout.Vector2Field("Point (" + i + "," + j + ")", myScript.projectionCameras[myScript.selectedMesh].pointOffset[index]);
                            }
                        }

                        break;
                    default:
                        break;
                }
                EditorGUILayout.Space();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
                EditorGUILayout.EndVertical();
            }
            

            EditorGUILayout.Space();


            showReferenceGameObjects = EditorGUILayout.Foldout(showReferenceGameObjects, "Reference Game Objects", foldoutStyle);

            if (showReferenceGameObjects)
            {

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Do not unlink these references. If you accidentally unlink them, please revert the prefab or refer to the manual on how to relink them.");
                EditorGUILayout.Space();
                EditorGUILayout.EndVertical();

                myScript.sourceCamerasContainer = (Transform)EditorGUILayout.ObjectField("Pano Cameras Controller", myScript.sourceCamerasContainer, typeof(Transform), true);
                myScript.projectionCamerasContainer = (Transform)EditorGUILayout.ObjectField("Projection Cameras Controller", myScript.projectionCamerasContainer, typeof(Transform), true);
                myScript.calibrationManager = (CalibrationManager)EditorGUILayout.ObjectField("Calibration Manager", myScript.calibrationManager, typeof(CalibrationManager), true);
                myScript.notificationMessage = (NotificationMessage)EditorGUILayout.ObjectField("Notification Message", myScript.notificationMessage, typeof(NotificationMessage), true);
                EditorGUILayout.Space();
                
            }

            /*
            EditorGUILayout.Space();
            showStats = EditorGUILayout.Foldout(showStats, "Stats");
            if(showStats){
                string stats = "";
                stats+="Vertex Total: "+"\n";
                stats+="Triangle Total: "+"\n";
                EditorGUILayout.TextArea(stats);
            }
            */
            /*
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(myScript, "Changed Camera Count");
                //Refresh();

            }
            */
            if (GUI.changed)
            {
                
                if (myScript.showIconLabels) myScript.calibrationManager.ShowIconLabels();
                else myScript.calibrationManager.HideIconLabels();
                
                string[] res = UnityStats.screenRes.Split('x');

                //check to see if resolution matches, if not show warning message
                if (int.Parse(res[0]) != (int)projectorResolution.x ||
                    int.Parse(res[1]) != (int)projectorResolution.y)
                {
                    Debug.LogWarning("One of your Game windows set at " +
                        int.Parse(res[0]) + "x" + int.Parse(res[1]) +
                        " does not match the specified projector resolution " +
                        (int)projectorResolution.x + "x" + (int)projectorResolution.y +
                        ". Please update your Game window resolution or adjust your desired projector resolution to match.");
                }

                myScript.projectorCount = Mathf.Clamp(myScript.projectorCount, 1, MAX_CAMERAS);

                #region REBUILD CAMERA CONDITIONS
                //determine if critical things are changed
                bool rebuildCameras = false;

                //projection camera space changed
                if (prevOrthographicSizeScale != orthographicSizeScale)
                {
                    rebuildCameras = true;
                }

                //projection camera space changed
                if (prevProjectorResolution != projectorResolution)
                {
                    rebuildCameras = true;
                }

                //X/Y divisions changed
                if (prevXDivisions != xDivisions ||
                    prevYDivisions != yDivisions)
                {
                    rebuildCameras = true;
                }

                //Camera count changed
                if (prevCameraCount != projectorCount)
                {
                    rebuildCameras = true;
                }

                #endregion

                if (rebuildCameras)
                {
                    myScript.regenerateCamera = true;
                    myScript.DestroyCameras();
                    myScript.InitCameras();
                }
                
                
                //update selection and mesh vertex
                for (int i = 0; i < myScript.projectionCameras.Count; i++)
                {

                    myScript.projectionCameras[i].UpdateMeshVertices();
                    myScript.projectionCameras[i].UpdateSelectedLines();
                    myScript.projectionCameras[i].HighlightSelection();

                    myScript.targetDisplays[i] = i + myScript.firstProjector - 1;
                    myScript.sourceCameras[i].GetComponent<Camera>().targetDisplay = myScript.targetDisplays[i];
                    myScript.projectionCameras[i].transform.parent.GetComponent<Camera>().targetDisplay = myScript.targetDisplays[i];
                }


                myScript.UpdateCursor();               
                myScript.UpdateProjectionWarpGUI();
                myScript.UpdateSourceCameras();
                myScript.UpdateProjectionCameras();

                //EditorUtility.SetDirty(myScript);

                //EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            prevProjectorResolution = projectorResolution;
            prevXDivisions = xDivisions;
            prevYDivisions = yDivisions;
            prevCameraCount = projectorCount;
            prevOrthographicSizeScale = orthographicSizeScale;

            serializedObject.ApplyModifiedProperties();
        }
    }

}
