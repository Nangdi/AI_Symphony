using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Globalization;
using SimpleJSON;
using UnityEngine.EventSystems;

namespace MultiProjectorWarpSystem
{
    [ExecuteInEditMode]
    public class ProjectionWarpSystem : MonoBehaviour
    {
        public enum CameraArragement
        {
            HORIZONTAL_ORTHOGRAPHIC = 1,
            VERTICAL_ORTHOGRAPHIC = 2,
            HORIZONTAL_PERSPECTIVE = 3,
            VERTICAL_PERSPECTIVE = 4,
            HORIZONTAL_PERSPECTIVE_CIRCULAR = 5,
            VERTICAL_PERSPECTIVE_CIRCULAR = 6,
        }


        [Header("Projection Settings")]
        public string defaultCalibrationFile;
        public CameraArragement arrangement;
        public Vector2 renderTextureSize;
        public int xDivisions = 5;
        public int yDivisions = 5;
        public int firstProjector = 1;
        public int projectorCount;
        public bool regenerateCamera = true;
        public bool reverseOrdering = false;
        

        [Header("Edit Mode")]
        public int selectedMesh;

        [Header("Debug")]
        public bool showMouseCursor;
        public bool showProjectionWarpGUI;
        public bool showIconLabels;

        [Header("Reference Game Objects")]
        public Transform projectionCamerasContainer;
        public Transform sourceCamerasContainer;
        public RectTransform projectionUIContainer;
        public GameObject fileIOContainer;
        public NotificationMessage notificationMessage;
        public List<Material> fadeMaterials;
        public CalibrationManager calibrationManager;


        [Header("Cameras & UI")]
        public List<Camera> sourceCameras;
        public List<ProjectionMesh> projectionCameras;
        public List<int> targetDisplays;

        [Header("Distances")]
        public Vector2 overlap;
        public float viewportSize;
        public float orthographicSizeScale;
        public float near;
        public float far;
        public float fieldOfView;

        public float aspectRatio;

        public float projectionCameraSpace;

        [Header("File IO")]
        public string saveCalibrationFile;

        


        public void UpdateFilename()
        {
            saveCalibrationFile = calibrationManager.filename.text;
        }

        
        void OnEnable()
        {

            if (FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
                eventSystem.AddComponent<StandaloneInputModule>();
            }

            UpdateCursor();
            UpdateProjectionWarpGUI();


            //bind enter press event on input field
            calibrationManager.filename.onEndEdit.RemoveAllListeners();
            calibrationManager.filename.onEndEdit.AddListener(val =>
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    saveCalibrationFile = calibrationManager.filename.text;
                }
            });
            
            /*
            if (projectorCount != sourceCameras.Count)
            {
                DestroyCameras();
                InitCameras();

            }
            */

            DestroyCameras();
            InitCameras();
            UpdateSourceCameras();
        }

        public void AssignReferences()
        {

            for (int i = 0; i < projectorCount; i++)
            {
                if (sourceCameras[i] == null || projectionCameras[i] == null) continue;
                projectionCameras[i].meshRenderer.sharedMaterial.mainTexture = sourceCameras[i].targetTexture;
            }
        }
        void Start()
        {
            
            if (File.Exists(Application.dataPath + "/../default_calibration.json"))	
            {	
                LoadCalibration(Application.dataPath + "/../default_calibration.json");	
            }	
            else if (defaultCalibrationFile.Length > 0)	
            {	
                LoadCalibration(defaultCalibrationFile);	
            }

            AssignReferences();
            SelectProjector(0, false);
            
        }

        public ProjectionMesh GetCurrentProjectionCamera()
        {
            if (selectedMesh < 0 || selectedMesh > 7) return null;

            return projectionCameras[selectedMesh];
        }
        public void UpdateCursor()
        {
            Cursor.visible = showMouseCursor;
            calibrationManager.mouseSelectedHighlight.SetActive(showMouseCursor);
        }

        public void UpdateProjectionWarpGUI()
        {
            calibrationManager.canvas.enabled = showProjectionWarpGUI;

            if (Application.isPlaying)
            {
                if (showProjectionWarpGUI) EventSystem.current.sendNavigationEvents = false;
                else EventSystem.current.sendNavigationEvents = true;
            }
            
        }

        public void SetEditMode(ProjectionMesh.MeshEditMode mode)
        {
            if (GetCurrentProjectionCamera() == null) return;
            
            GetCurrentProjectionCamera().SetEditMode(mode);
            
            switch (mode)
            {
                case ProjectionMesh.MeshEditMode.NONE:
                    calibrationManager.SetButtonState(CalibrationManager.MenuState.NONE);
                    break;
                case ProjectionMesh.MeshEditMode.CORNERS:
                    calibrationManager.SetButtonState(CalibrationManager.MenuState.CORNERS);
                    break;
                case ProjectionMesh.MeshEditMode.ROWS:
                    calibrationManager.SetButtonState(CalibrationManager.MenuState.ROWS);
                    break;
                case ProjectionMesh.MeshEditMode.COLUMNS:
                    calibrationManager.SetButtonState(CalibrationManager.MenuState.COLUMNS);
                    break;
                case ProjectionMesh.MeshEditMode.POINTS:
                    calibrationManager.SetButtonState(CalibrationManager.MenuState.POINTS);
                    break;
                default:
                    break;
            }
            

        }
        public void DeactivateAllSelection()
        {
            for(int i = 0; i < projectionCameras.Count; i++)
            {
                projectionCameras[i].DeactivateSelection();
            }

        }
        public void SelectProjector(int projectorIndex, bool animateIndex)
        {
            if (projectorCount <= projectorIndex) return;

            ProjectionMesh mesh = null;

            if (selectedMesh >= 0 && selectedMesh <= 7)
            {
                GetCurrentProjectionCamera().DeactivateSelection();
            }

            selectedMesh = projectorIndex;
            mesh = GetCurrentProjectionCamera();
            calibrationManager.currentProjectorText.text = (projectorIndex + 1).ToString();
            if (animateIndex) mesh.ShowProjectorIndex();


            //check to see if in blend, white balance or help mode
            if (calibrationManager.state == CalibrationManager.MenuState.BLEND ||
                calibrationManager.state == CalibrationManager.MenuState.WHITE_BALANCE ||
                calibrationManager.state == CalibrationManager.MenuState.HELP)
            {
                //cache the current mode
                CalibrationManager.MenuState state = calibrationManager.state;
                //reset selections
                SetEditMode(ProjectionMesh.MeshEditMode.NONE);

                //reapply current mode
                calibrationManager.SetButtonState(state);
            }
            else
            {
                //use last used edit mode
                SetEditMode(mesh.editMode);
            }


            //update UI values by applying only to sliders
            calibrationManager.topRangeSlider.value = mesh.topFadeRange;
            calibrationManager.topRangeInputField.text = mesh.topFadeRange.ToString();
            calibrationManager.topChokeSlider.value = mesh.topFadeChoke;
            calibrationManager.topChokeInputField.text = mesh.topFadeChoke.ToString();
            calibrationManager.topGammaSlider.value = mesh.topFadeGamma;
            calibrationManager.topGammaInputField.text = mesh.topFadeGamma.ToString();
            calibrationManager.bottomRangeSlider.value = mesh.bottomFadeRange;
            calibrationManager.bottomRangeInputField.text = mesh.bottomFadeRange.ToString();
            calibrationManager.bottomChokeSlider.value = mesh.bottomFadeChoke;
            calibrationManager.bottomChokeInputField.text = mesh.bottomFadeChoke.ToString();
            calibrationManager.bottomGammaSlider.value = mesh.bottomFadeGamma;
            calibrationManager.bottomGammaInputField.text = mesh.bottomFadeGamma.ToString();
            calibrationManager.leftRangeSlider.value = mesh.leftFadeRange;
            calibrationManager.leftRangeInputField.text = mesh.leftFadeRange.ToString();
            calibrationManager.leftChokeSlider.value = mesh.leftFadeChoke;
            calibrationManager.leftChokeInputField.text = mesh.leftFadeChoke.ToString();
            calibrationManager.leftGammaSlider.value = mesh.leftFadeGamma;
            calibrationManager.leftGammaInputField.text = mesh.leftFadeGamma.ToString();
            calibrationManager.rightRangeSlider.value = mesh.rightFadeRange;
            calibrationManager.rightRangeInputField.text = mesh.rightFadeRange.ToString();
            calibrationManager.rightChokeSlider.value = mesh.rightFadeChoke;
            calibrationManager.rightChokeInputField.text = mesh.rightFadeChoke.ToString();
            calibrationManager.rightGammaSlider.value = mesh.rightFadeGamma;
            calibrationManager.rightGammaInputField.text = mesh.rightFadeGamma.ToString();

            calibrationManager.redSlider.value = mesh.tint.r * 255;
            calibrationManager.greenSlider.value = mesh.tint.g * 255;
            calibrationManager.blueSlider.value = mesh.tint.b * 255;
        }


        public void SelectNextProjector()
        {
            int meshIndex = selectedMesh;
            meshIndex++;
            if (meshIndex > projectorCount - 1) meshIndex = 0;
            SelectProjector(meshIndex, true);
        }
        void Update()
        {
#if UNITY_EDITOR
            AssignReferences();
#endif

        }

        public void UpdateProjectionCameras()
        {

            for (int i = 0; i < projectionCameras.Count; i++)
            {
                if (projectionCameras[i] == null) continue;
                //projectionCameras[i].transform.parent.localPosition = new Vector3((float)i * (projectionCameras[i].width + projectionCameraSpace), 0f, 0f);
                
                switch(arrangement){
                    case CameraArragement.VERTICAL_ORTHOGRAPHIC:
                    case CameraArragement.VERTICAL_PERSPECTIVE:
                    case CameraArragement.VERTICAL_PERSPECTIVE_CIRCULAR:
                        projectionCameras[i].transform.parent.localPosition = new Vector3(0f, -(float)i * (projectionCameras[i].height + projectionCameraSpace), 0f);
                        break;
                    case CameraArragement.HORIZONTAL_ORTHOGRAPHIC:
                    case CameraArragement.HORIZONTAL_PERSPECTIVE:
                    case CameraArragement.HORIZONTAL_PERSPECTIVE_CIRCULAR:
                    default:
                        projectionCameras[i].transform.parent.localPosition = new Vector3((float)i * (projectionCameras[i].width + projectionCameraSpace), 0f, 0f);
                        break;
                }
                projectionCameras[i].width = renderTextureSize.x / 100f;
                projectionCameras[i].height = renderTextureSize.y / 100f;
                projectionCameras[i].xDivisions = xDivisions;
                projectionCameras[i].yDivisions = yDivisions;
                projectionCameras[i].targetCamera.orthographicSize = viewportSize;

                projectionCameras[i].CreateBaseGridLines();
            }

        }

        public float HorizontalToVerticalFOV(float hFov)
        {
            float hFovRad = hFov * Mathf.Deg2Rad;
            float vFovRad = Mathf.Atan(Mathf.Tan(hFovRad * 0.5f) / aspectRatio) * 2f;
            float vFov = vFovRad * Mathf.Rad2Deg;
            return vFov;
        }

        public void UpdateSourceCameras()
        {   
            
            aspectRatio = (float)renderTextureSize.x/ (float)renderTextureSize.y;
            
            float viewportHeight = viewportSize * 2;
            float viewportWidth = viewportHeight * aspectRatio;

            float singleFieldOfViewH;
            float singleFieldOfViewV;
            float startAngle;
            float compressedArcAngle;
            float offsetAngle;


            // Near far adjustments only for orthographic cameras
            switch (arrangement)
            {
                case CameraArragement.HORIZONTAL_ORTHOGRAPHIC:
                case CameraArragement.VERTICAL_ORTHOGRAPHIC:
                    for (int i = 0; i < sourceCameras.Count; i++)
                    {
                        sourceCameras[i].nearClipPlane = near;
                        sourceCameras[i].farClipPlane = far;

                        // Orthograhpic cameras will be locked into target resolution
                        sourceCameras[i].orthographic = true;
                        sourceCameras[i].orthographicSize = viewportSize * orthographicSizeScale;
                    }
                    break;
                case CameraArragement.HORIZONTAL_PERSPECTIVE:
                case CameraArragement.VERTICAL_PERSPECTIVE:
                    break;
                default:
                    break;
            }

            // Calculate camera transforms
            switch (arrangement)
            {
                case CameraArragement.HORIZONTAL_ORTHOGRAPHIC:

                    float startX = (-(sourceCameras.Count / 2f) * viewportWidth * orthographicSizeScale) + (viewportWidth / 2f * orthographicSizeScale) + ((overlap.x / 2f * orthographicSizeScale) * (sourceCameras.Count - 1));
                    
                    float offsetX = 0f;

                    for (int i = 0; i < sourceCameras.Count; i++)
                    {
                        int index = i;
                        if(reverseOrdering) index = projectorCount - i - 1;

                        offsetX = startX + (index * viewportWidth * orthographicSizeScale) - (index * overlap.x * orthographicSizeScale);
                        sourceCameras[i].transform.localPosition = new Vector3(
                            offsetX,
                            0,
                            0);
                        sourceCameras[i].transform.localEulerAngles = Vector3.zero;
                    }
                    break;
                case CameraArragement.VERTICAL_ORTHOGRAPHIC:

                    float startY = (-(sourceCameras.Count / 2f) * viewportHeight * orthographicSizeScale) + (viewportHeight / 2f * orthographicSizeScale) + ((overlap.y / 2f * orthographicSizeScale) * (sourceCameras.Count - 1));
                    float offsetY = 0f;
                    for (int i = 0; i < sourceCameras.Count; i++)
                    {
                        int index = i;
                        if(reverseOrdering) index = projectorCount - i - 1;

                        offsetY = startY + (index * viewportHeight * orthographicSizeScale) - (index * overlap.y * orthographicSizeScale);
                        sourceCameras[i].transform.localPosition = new Vector3(
                            0,
                            offsetY,
                            0);
                        sourceCameras[i].transform.localEulerAngles = Vector3.zero;
                    }
                    break;
                case ProjectionWarpSystem.CameraArragement.HORIZONTAL_PERSPECTIVE:
                    singleFieldOfViewH = (fieldOfView / sourceCameras.Count) + overlap.x;
                    singleFieldOfViewV = HorizontalToVerticalFOV(singleFieldOfViewH);

                    startAngle = -(fieldOfView / 2f) + (singleFieldOfViewH / 2f);
                    compressedArcAngle = -startAngle * 2f;

                    for (int i = 0; i < sourceCameras.Count; i++)
                    {
                        int index = i;
                        if(reverseOrdering) index = projectorCount - i - 1;

                        if (sourceCameras[i] == null) continue;
                        sourceCameras[i].nearClipPlane = near;
                        sourceCameras[i].farClipPlane = far;

                        //calculate from field of view total
                        sourceCameras[i].orthographic = false;
                        sourceCameras[i].fieldOfView = singleFieldOfViewV;

                        sourceCameras[i].transform.localPosition = Vector3.zero;

                        if (projectorCount > 1)
                        {
                            sourceCameras[i].transform.localEulerAngles = new Vector3(0, startAngle + (index * (compressedArcAngle / (projectorCount - 1))), 0);
                        }
                        else
                        {
                            sourceCameras[i].transform.localEulerAngles = new Vector3(0, startAngle, 0);
                        }


                    }
                    break;
                case ProjectionWarpSystem.CameraArragement.VERTICAL_PERSPECTIVE:
                    singleFieldOfViewV = (fieldOfView / sourceCameras.Count) + overlap.y;
                    startAngle = -(fieldOfView / 2f) + (singleFieldOfViewV / 2f);
                    compressedArcAngle = -startAngle * 2f;
                
                    for (int i = 0; i < sourceCameras.Count; i++)
                    {
                        int index = i;
                        if(reverseOrdering) index = projectorCount - i - 1;

                        sourceCameras[i].nearClipPlane = near;
                        sourceCameras[i].farClipPlane = far;

                        //calculate from field of view total
                        sourceCameras[i].orthographic = false;
                        sourceCameras[i].fieldOfView = singleFieldOfViewV;

                        sourceCameras[i].transform.localPosition = Vector3.zero;
                        if (projectorCount > 1)
                        {
                            sourceCameras[i].transform.localEulerAngles = new Vector3(startAngle + (index * (compressedArcAngle / (projectorCount - 1))), 0, 0);
                        }
                        else
                        {
                            sourceCameras[i].transform.localEulerAngles = new Vector3(startAngle, 0, 0);
                        }
                    }
                    break;
                case ProjectionWarpSystem.CameraArragement.HORIZONTAL_PERSPECTIVE_CIRCULAR:
                    singleFieldOfViewH = (360f / (float)projectorCount) + overlap.x;
                    singleFieldOfViewV = HorizontalToVerticalFOV(singleFieldOfViewH);

                    //startAngle = -(fieldOfView / 2f) + (singleFieldOfViewH / 2f);
                    startAngle = 0;
                    offsetAngle = 360f / (float)projectorCount;

                    for (int i = 0; i < sourceCameras.Count; i++)
                    {
                        int index = i;
                        if(reverseOrdering) index = projectorCount - i - 1;

                        if (sourceCameras[i] == null) continue;
                        sourceCameras[i].nearClipPlane = near;
                        sourceCameras[i].farClipPlane = far;

                        //calculate from field of view total
                        sourceCameras[i].orthographic = false;
                        sourceCameras[i].fieldOfView = singleFieldOfViewV;

                        sourceCameras[i].transform.localPosition = Vector3.zero;

                        if (projectorCount > 1)
                        {
                            sourceCameras[i].transform.localEulerAngles = new Vector3(0, startAngle + ((float)index * offsetAngle), 0);
                        }
                        else
                        {
                            sourceCameras[i].transform.localEulerAngles = new Vector3(0, startAngle, 0);
                        }


                    }
                    break;
                case ProjectionWarpSystem.CameraArragement.VERTICAL_PERSPECTIVE_CIRCULAR:
                    singleFieldOfViewV = (360f / (float)projectorCount) + overlap.y;
                    startAngle = 0f;
                    
                    offsetAngle = 360f / (float)projectorCount;
                    
                    for (int i = 0; i < sourceCameras.Count; i++)
                    {
                        int index = i;
                        if(reverseOrdering) index = projectorCount - i - 1;

                        sourceCameras[i].nearClipPlane = near;
                        sourceCameras[i].farClipPlane = far;

                        //calculate from field of view total
                        sourceCameras[i].orthographic = false;
                        sourceCameras[i].fieldOfView = singleFieldOfViewV;

                        sourceCameras[i].transform.localPosition = Vector3.zero;
                        if (projectorCount > 1)
                        {
                            sourceCameras[i].transform.localEulerAngles = new Vector3(startAngle + ((float)index * offsetAngle), 0, 0);
                        }
                        else
                        {
                            sourceCameras[i].transform.localEulerAngles = new Vector3(startAngle, 0, 0);
                        }
                    }
                    break;
                default:
                    break;
            }
        }


        public void DestroyCameras()
        {
            if (!regenerateCamera) return;
            int count;
            
            count = sourceCamerasContainer.childCount;
            for (int i = 0; i < count; i++)
            {
                DestroyImmediate(sourceCamerasContainer.GetChild(0).gameObject);
            }
            sourceCameras = new List<Camera>();

            targetDisplays = new List<int>();

            count = projectionCamerasContainer.childCount;
            for (int i = 0; i < count; i++)
            {
                DestroyImmediate(projectionCamerasContainer.GetChild(0).gameObject);
            }
            projectionCameras = new List<ProjectionMesh>();
        }
        
        public void InitCameras()
        {        
            if (!regenerateCamera) return;
            //build source render texture cameras array
            GameObject sourceCamera;
            Camera camera;
            RenderTexture renderTexture;

            for (int i = 0; i < projectorCount; i++)
            {
                sourceCamera = Instantiate(Resources.Load("Prefabs/Source Camera", typeof(GameObject))) as GameObject;
                camera = sourceCamera.GetComponent<Camera>();
                sourceCameras.Add(camera);
                sourceCamera.name = "Source Camera " + (i + 1);
                sourceCamera.transform.SetParent(sourceCamerasContainer);
                renderTexture = new RenderTexture((int)renderTextureSize.x, (int)renderTextureSize.y, 24, RenderTextureFormat.ARGB32);
                renderTexture.antiAliasing = 8;
                renderTexture.Create();
                targetDisplays.Add(i + firstProjector - 1);
                camera.targetTexture = renderTexture;
                camera.targetDisplay = targetDisplays[i];
            }

            //build final render cameras
            GameObject projectionCamera;
            ProjectionMesh projectionMesh;

            for (int i = 0; i < projectorCount; i++)
            {
                int index = i;
                if(reverseOrdering) index = projectorCount - i - 1;

                projectionCamera = Instantiate(Resources.Load("Prefabs/Projection Camera", typeof(GameObject))) as GameObject;
                projectionMesh = projectionCamera.transform.GetChild(0).GetComponent<ProjectionMesh>();
                
                projectionCameras.Add(projectionMesh);
                projectionMesh.meshIndex = i;
                projectionCamera.name = "Projection Camera " + (i + 1);
                projectionCamera.transform.SetParent(projectionCamerasContainer);
                
                switch(arrangement){
                    case CameraArragement.VERTICAL_ORTHOGRAPHIC:
                    case CameraArragement.VERTICAL_PERSPECTIVE:
                    case CameraArragement.VERTICAL_PERSPECTIVE_CIRCULAR:
                        projectionCamera.transform.localPosition = new Vector3(0f, (float)index * (projectionMesh.height + projectionCameraSpace), 0f);
                        break;
                    case CameraArragement.HORIZONTAL_ORTHOGRAPHIC:
                    case CameraArragement.HORIZONTAL_PERSPECTIVE:
                    case CameraArragement.HORIZONTAL_PERSPECTIVE_CIRCULAR:
                    default:
                        projectionCamera.transform.localPosition = new Vector3((float)index * (projectionMesh.width + projectionCameraSpace), 0f, 0f);
                        break;
                }
                
                projectionMesh.projectorIndexText.text = (i + 1).ToString();
                projectionMesh.xDivisions = xDivisions;
                projectionMesh.yDivisions = yDivisions;
                //projectionMesh.prevXDivision = xDivisions;
                //projectionMesh.prevYDivision = yDivisions;
                projectionMesh.width = renderTextureSize.x / 100f;
                projectionMesh.height = renderTextureSize.y / 100f;
                projectionCamera.GetComponent<Camera>().targetDisplay = targetDisplays[i];

                projectionMesh.CreateMesh();

                Material projectionImage = Instantiate(Resources.Load("Materials/Projection Image", typeof(Material))) as Material;
                projectionImage.name = "Projection Image " + (i + 1);
                projectionImage.mainTexture = sourceCameras[i].targetTexture;

                projectionMesh.meshRenderer.material = projectionImage;

            }

            UpdateProjectionCameras();

        }

        #region File IO
        public void SaveCalibrationUsingInput(GameObject input)
        {
            SaveCalibration(input.GetComponent<InputField>().text);
            notificationMessage.messageText.text = "Calibration has been saved";
            notificationMessage.Show();
        }
        public void LoadCalibrationUsingInput(GameObject input)
        {
            if (LoadCalibration(input.GetComponent<InputField>().text))
            {
                notificationMessage.messageText.text = "Calibration has been loaded";
            }
            else
            {
                notificationMessage.messageText.text = "Error loading calibration";
            }

            notificationMessage.Show();
        }
        public void SaveCalibration(string path)
        {
            if (path == null || path.Length == 0) return;
            //        Debug.Log(Application.dataPath+"/"+path);
            string iItemFormat = "G";
            string json = "";
            json += "{";

            #region Global Settings
            json += "\"Version\": \"" + "3.4.0" + "\",";
            json += "\"Arrangement\":" + (int)arrangement + ",";
            if (reverseOrdering){
                json += "\"ReverseOrdering\":1,";
            }
            else{
                json += "\"ReverseOrdering\":0,";
            }
            
            json += "\"OrthographicSizeScale\":" + orthographicSizeScale.ToString(iItemFormat, CultureInfo.InvariantCulture) + ",";
            json += "\"TextureWidth\":" + (int)renderTextureSize.x + ",";
            json += "\"TextureHeight\":" + (int)renderTextureSize.y + ",";
            json += "\"XDivisions\":" + xDivisions + ",";
            json += "\"YDivisions\":" + yDivisions + ",";
            json += "\"OverlapX\":" + overlap.x.ToString(iItemFormat, CultureInfo.InvariantCulture) + ",";
            json += "\"OverlapY\":" + overlap.y.ToString(iItemFormat, CultureInfo.InvariantCulture) + ",";
            json += "\"ViewportSize\":" + viewportSize.ToString(iItemFormat, CultureInfo.InvariantCulture) + ",";
            json += "\"FieldOfView\":" + fieldOfView.ToString(iItemFormat, CultureInfo.InvariantCulture) + ",";
            json += "\"Near\":" + near.ToString(iItemFormat, CultureInfo.InvariantCulture) + ",";
            json += "\"Far\":" + far.ToString(iItemFormat, CultureInfo.InvariantCulture) + ",";
            json += "\"Spacing\":" + projectionCameraSpace.ToString(iItemFormat, CultureInfo.InvariantCulture) + ",";
            json += "\"FirstProjector\":" + firstProjector + ",";
            json += "\"Cameras\":";
            #endregion

            json += "[";

            for (int i = 0; i < projectorCount; i++)
            {
                ProjectionMesh projectionMesh = projectionCameras[i];
                json += "{";

                #region Edge Blending & White Balance
                json += "\"TopFadeRange\":" + projectionMesh.topFadeRange.ToString(iItemFormat, CultureInfo.InvariantCulture) + ",";
                json += "\"TopFadeChoke\":" + projectionMesh.topFadeChoke.ToString(iItemFormat, CultureInfo.InvariantCulture) + ",";
                json += "\"TopFadeGamma\":" + projectionMesh.topFadeGamma.ToString(iItemFormat, CultureInfo.InvariantCulture) + ",";
                json += "\"BottomFadeRange\":" + projectionMesh.bottomFadeRange.ToString(iItemFormat, CultureInfo.InvariantCulture) + ",";
                json += "\"BottomFadeChoke\":" + projectionMesh.bottomFadeChoke.ToString(iItemFormat, CultureInfo.InvariantCulture) + ",";
                json += "\"BottomFadeGamma\":" + projectionMesh.bottomFadeGamma.ToString(iItemFormat, CultureInfo.InvariantCulture) + ",";
                json += "\"LeftFadeRange\":" + projectionMesh.leftFadeRange.ToString(iItemFormat, CultureInfo.InvariantCulture) + ",";
                json += "\"LeftFadeChoke\":" + projectionMesh.leftFadeChoke.ToString(iItemFormat, CultureInfo.InvariantCulture) + ",";
                json += "\"LeftFadeGamma\":" + projectionMesh.leftFadeGamma.ToString(iItemFormat, CultureInfo.InvariantCulture) + ",";
                json += "\"RightFadeRange\":" + projectionMesh.rightFadeRange.ToString(iItemFormat, CultureInfo.InvariantCulture) + ",";
                json += "\"RightFadeChoke\":" + projectionMesh.rightFadeChoke.ToString(iItemFormat, CultureInfo.InvariantCulture) + ",";
                json += "\"RightFadeGamma\":" + projectionMesh.rightFadeGamma.ToString(iItemFormat, CultureInfo.InvariantCulture) + ",";
                
                json += "\"Tint\":" + "{ \"r\":" + projectionMesh.tint.r.ToString(iItemFormat, CultureInfo.InvariantCulture) + 
                    ",\"g\":" + projectionMesh.tint.g.ToString(iItemFormat, CultureInfo.InvariantCulture) + 
                    ",\"b\":" + projectionMesh.tint.b.ToString(iItemFormat, CultureInfo.InvariantCulture) + "},";

                #endregion

                #region Offsets
                json += "\"Offset\":";
                json += "{";
                json += "\"Corner\":";
                json += "[";
                for (int j = 0; j < 4; j++)
                {
                    json += projectionMesh.cornerOffset[j].x.ToString(iItemFormat, CultureInfo.InvariantCulture);
                    json += ",";
                    json += projectionMesh.cornerOffset[j].y.ToString(iItemFormat, CultureInfo.InvariantCulture);
                    if(j<3) json += ",";
                }
                json += "]";
                json += ",";           
                json += "\"Point\":";
                json += "[";
                int pointCount = (xDivisions + 1) * (yDivisions + 1);
                for (int j = 0; j < pointCount; j++)
                {
                    json += projectionMesh.pointOffset[j].x.ToString(iItemFormat, CultureInfo.InvariantCulture);
                    json += ",";
                    json += projectionMesh.pointOffset[j].y.ToString(iItemFormat, CultureInfo.InvariantCulture);
                    if (j < pointCount-1) json += ",";
                }
                json += "]";

               
                #endregion
                
                json += "}";
                json += "}";
                if (i < projectorCount - 1) json += ",";
            }
            
            json += "]";
          

            json += "}";

            var sr = File.CreateText(Application.dataPath + "/../" + path);
            sr.WriteLine(json);
            sr.Close();

            Debug.Log(path + " has been saved.");
        }

        public bool LoadCalibration(string path)
        {
            if (path == null || path.Length == 0) return false;

            string json = "";
            try
            {
                string line;

                StreamReader theReader = new StreamReader(path, Encoding.Default);
                using (theReader)
                {

                    do
                    {
                        line = theReader.ReadLine();

                        if (line != null)
                        {
                            json += line;
                        }
                    }
                    while (line != null);
                    theReader.Close();

                }
            }
            catch (Exception e)
            {
                //Console.WriteLine("{0}\n", e.Message);
                Debug.Log(e.Message);
                return false;

            }
            var N = JSON.Parse(json);
            //fieldOfView = N["FieldOfView"].AsFloat;	
            fieldOfView = float.Parse(N["FieldOfView"], CultureInfo.InvariantCulture.NumberFormat);
            projectorCount = N["Cameras"].Count;
            renderTextureSize = new Vector2(N["TextureWidth"].AsInt, N["TextureHeight"].AsInt);
            xDivisions = N["XDivisions"].AsInt;
            yDivisions = N["YDivisions"].AsInt;
            arrangement = (CameraArragement)N["Arrangement"].AsInt;
            reverseOrdering = false;
            if(N["ReverseOrdering"]!=null){
                int ordering = N["ReverseOrdering"].AsInt;
                if(ordering==1){
                    reverseOrdering = true;
                }
                else{
                    reverseOrdering = false;
                }
            }
            
            //overlap = new Vector2(N["OverlapX"].AsFloat, N["OverlapY"].AsFloat);	
            overlap = new Vector2(float.Parse(N["OverlapX"], CultureInfo.InvariantCulture.NumberFormat), float.Parse(N["OverlapY"], CultureInfo.InvariantCulture.NumberFormat));	
            
            switch(arrangement){
                case CameraArragement.HORIZONTAL_ORTHOGRAPHIC:
                case CameraArragement.HORIZONTAL_PERSPECTIVE:
                case CameraArragement.HORIZONTAL_PERSPECTIVE_CIRCULAR:
                    calibrationManager.overlapSlider.value = overlap.x;
                    calibrationManager.overlapInputField.text = overlap.x.ToString();
                    break;
                case CameraArragement.VERTICAL_ORTHOGRAPHIC:
                case CameraArragement.VERTICAL_PERSPECTIVE:
                case CameraArragement.VERTICAL_PERSPECTIVE_CIRCULAR:
                    calibrationManager.overlapSlider.value = overlap.y;
                    calibrationManager.overlapInputField.text = overlap.y.ToString();
                    break;
                default:
                    break;
            }
            
            //viewportSize = N["ViewportSize"].AsFloat;	
            viewportSize = float.Parse(N["ViewportSize"], CultureInfo.InvariantCulture.NumberFormat);

            //near = N["Near"].AsFloat;	
            //far = N["Far"].AsFloat;	
            //projectionCameraSpace = N["Spacing"].AsFloat;	

            near = float.Parse(N["Near"], CultureInfo.InvariantCulture.NumberFormat);	
            far = float.Parse(N["Far"], CultureInfo.InvariantCulture.NumberFormat);	
            projectionCameraSpace = float.Parse(N["Spacing"], CultureInfo.InvariantCulture.NumberFormat);	


            orthographicSizeScale = 1f;
            if (N["OrthographicSizeScale"] != null){
                //orthographicSizeScale = N["OrthographicSizeScale"].AsFloat;	
                orthographicSizeScale = float.Parse(N["OrthographicSizeScale"], CultureInfo.InvariantCulture.NumberFormat);
            }

            firstProjector = 1;
            if (N["FirstProjector"] != null){
                firstProjector = N["FirstProjector"].AsInt;
            }

            DestroyCameras();
            InitCameras();


            for (int i = 0; i < projectorCount; i++)
            {
                ProjectionMesh projectionMesh = projectionCameras[i];
                JSONNode cameraNode = N["Cameras"][i];
                /*
                projectionMesh.topFadeRange = cameraNode["TopFadeRange"].AsFloat;
                projectionMesh.topFadeChoke = cameraNode["TopFadeChoke"].AsFloat;
                projectionMesh.topFadeGamma = cameraNode["TopFadeGamma"].AsFloat;
                projectionMesh.bottomFadeRange = cameraNode["BottomFadeRange"].AsFloat;
                projectionMesh.bottomFadeChoke = cameraNode["BottomFadeChoke"].AsFloat;
                projectionMesh.bottomFadeGamma = cameraNode["BottomFadeGamma"].AsFloat;
                projectionMesh.leftFadeRange = cameraNode["LeftFadeRange"].AsFloat;
                projectionMesh.leftFadeChoke = cameraNode["LeftFadeChoke"].AsFloat;
                projectionMesh.leftFadeGamma = cameraNode["LeftFadeGamma"].AsFloat;
                projectionMesh.rightFadeRange = cameraNode["RightFadeRange"].AsFloat;
                projectionMesh.rightFadeChoke = cameraNode["RightFadeChoke"].AsFloat;
                projectionMesh.rightFadeGamma = cameraNode["RightFadeGamma"].AsFloat;
                
                projectionMesh.tint = new Color(cameraNode["Tint"]["r"].AsFloat, cameraNode["Tint"]["g"].AsFloat, cameraNode["Tint"]["b"].AsFloat);
                */
                projectionMesh.topFadeRange = float.Parse(cameraNode["TopFadeRange"], CultureInfo.InvariantCulture.NumberFormat);		
                projectionMesh.topFadeChoke = float.Parse(cameraNode["TopFadeChoke"], CultureInfo.InvariantCulture.NumberFormat);	
                if(cameraNode["TopFadeGamma"]!=null) projectionMesh.topFadeGamma = float.Parse(cameraNode["TopFadeGamma"], CultureInfo.InvariantCulture.NumberFormat);	
                projectionMesh.bottomFadeRange = float.Parse(cameraNode["BottomFadeRange"], CultureInfo.InvariantCulture.NumberFormat);		
                projectionMesh.bottomFadeChoke = float.Parse(cameraNode["BottomFadeChoke"], CultureInfo.InvariantCulture.NumberFormat);	
                if(cameraNode["BottomFadeGamma"]!=null) projectionMesh.bottomFadeGamma = float.Parse(cameraNode["BottomFadeGamma"], CultureInfo.InvariantCulture.NumberFormat);	
                projectionMesh.leftFadeRange = float.Parse(cameraNode["LeftFadeRange"], CultureInfo.InvariantCulture.NumberFormat);		
                projectionMesh.leftFadeChoke = float.Parse(cameraNode["LeftFadeChoke"], CultureInfo.InvariantCulture.NumberFormat);
                if(cameraNode["LeftFadeGamma"]!=null) projectionMesh.leftFadeGamma = float.Parse(cameraNode["LeftFadeGamma"], CultureInfo.InvariantCulture.NumberFormat);	
                projectionMesh.rightFadeRange = float.Parse(cameraNode["RightFadeRange"], CultureInfo.InvariantCulture.NumberFormat);		
                projectionMesh.rightFadeChoke = float.Parse(cameraNode["RightFadeChoke"], CultureInfo.InvariantCulture.NumberFormat);		
                if(cameraNode["RightFadeGamma"]!=null) projectionMesh.rightFadeGamma = float.Parse(cameraNode["RightFadeGamma"], CultureInfo.InvariantCulture.NumberFormat);
                	
                projectionMesh.tint = new Color(float.Parse(cameraNode["Tint"]["r"], CultureInfo.InvariantCulture.NumberFormat),	
                    float.Parse(cameraNode["Tint"]["g"], CultureInfo.InvariantCulture.NumberFormat),	
                    float.Parse(cameraNode["Tint"]["b"], CultureInfo.InvariantCulture.NumberFormat));

                JSONNode cornerNode = cameraNode["Offset"]["Corner"];
                
                for (int j = 0; j < 4; j++)
                {
                    //projectionMesh.cornerOffset[j] = new Vector2(cornerNode[j * 2].AsFloat, cornerNode[(j * 2) + 1].AsFloat);	
                    projectionMesh.cornerOffset[j] = new Vector2(float.Parse(cornerNode[j*2], CultureInfo.InvariantCulture.NumberFormat), 	
                        float.Parse(cornerNode[(j * 2)+1], CultureInfo.InvariantCulture.NumberFormat));
                }
                
                JSONNode pointNode = cameraNode["Offset"]["Point"];
                for (int j = 0; j < (xDivisions + 1)*(yDivisions+1); j++)
                {
                    //projectionMesh.pointOffset[j] = new Vector2(pointNode[j * 2].AsFloat, pointNode[(j * 2) + 1].AsFloat);	
                    projectionMesh.pointOffset[j] = new Vector2(float.Parse(pointNode[j * 2], CultureInfo.InvariantCulture.NumberFormat),	
                        float.Parse(pointNode[(j * 2) + 1], CultureInfo.InvariantCulture.NumberFormat));
                }
                

                projectionMesh.CreateMesh();
                projectionMesh.BlendRefresh();
                //projectionMesh.OffsetRefresh();
                projectionMesh.UpdateUI();
            }
            
            defaultCalibrationFile = path;
            UpdateSourceCameras();
            Debug.Log(path + " has been loaded.");

            return true;
        }

        #endregion


    }

}
