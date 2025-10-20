using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace MultiProjectorWarpSystem
{
    [RequireComponent(typeof(ProjectionWarpSystem))]
    public class ProjectionWarpSystemKeyboardInput : MonoBehaviour {

        [Header("Directions")]
        public KeyCode upKey;
        public KeyCode downKey;
        public KeyCode leftKey;
        public KeyCode rightKey;
        
        [Header("Adjustments")]
        public KeyCode addRowKey = KeyCode.F9;
        public KeyCode removeRowKey = KeyCode.F10;
        public KeyCode addColumnKey = KeyCode.F11;
        public KeyCode removeColumnKey = KeyCode.F12;


        [Header("Visibility")]
        public KeyCode uiToggleKey;
        public KeyCode mouseToggleKey;
        public KeyCode gridToggleKey;
        public KeyCode selectedGridToggleKey;
        public KeyCode controlPointsToggleKey;
        public KeyCode selectedControlPointsToggleKey;
        public KeyCode activateKey = KeyCode.Space;
        public KeyCode zeroOffsetKey = KeyCode.F5;

        [Header("Mode Selection")]
        public KeyCode helpKey = KeyCode.F1;
        public KeyCode noModeKey;
        public KeyCode cornerModeKey;
        public KeyCode rowModeKey;
        public KeyCode columnModeKey;
        public KeyCode pointModeKey;
        public KeyCode blendingModeKey;
        public KeyCode whiteBalanceModeKey;
        public KeyCode cameraSettingsKey;

        [Header("Projector Selection")]
        public KeyCode projectorAKey = KeyCode.Alpha1;
        public KeyCode projectorAAltKey = KeyCode.Keypad1;
        public KeyCode projectorBKey = KeyCode.Alpha2;
        public KeyCode projectorBAltKey = KeyCode.Keypad2;
        public KeyCode projectorCKey = KeyCode.Alpha3;
        public KeyCode projectorCAltKey = KeyCode.Keypad3;
        public KeyCode projectorDKey = KeyCode.Alpha4;
        public KeyCode projectorDAltKey = KeyCode.Keypad4;
        public KeyCode projectorEKey = KeyCode.Alpha5;
        public KeyCode projectorEAltKey = KeyCode.Keypad5;
        public KeyCode projectorFKey = KeyCode.Alpha6;
        public KeyCode projectorFAltKey = KeyCode.Keypad6;
        public KeyCode projectorGKey = KeyCode.Alpha7;
        public KeyCode projectorGAltKey = KeyCode.Keypad7;
        public KeyCode projectorHKey = KeyCode.Alpha8;
        public KeyCode projectorHAltKey = KeyCode.Keypad8;

        

        ProjectionWarpSystem system;
        
        void Start() {
            
            system = GetComponent<ProjectionWarpSystem>();
        }
        
        
        void Update() {
            //don't allow keyboard inputs when input field is focused
            CalibrationManager calibrationManager = system.calibrationManager;
            if (calibrationManager.topRangeInputField.isFocused ||
                calibrationManager.topChokeInputField.isFocused ||
                calibrationManager.bottomRangeInputField.isFocused ||
                calibrationManager.bottomChokeInputField.isFocused ||
                calibrationManager.leftRangeInputField.isFocused ||
                calibrationManager.leftChokeInputField.isFocused ||
                calibrationManager.rightRangeInputField.isFocused ||
                calibrationManager.rightChokeInputField.isFocused ||
                calibrationManager.redInputField.isFocused ||
                calibrationManager.greenInputField.isFocused ||
                calibrationManager.blueInputField.isFocused ||
                calibrationManager.filename.isFocused) return;
            

            ProjectionMesh mesh = system.GetCurrentProjectionCamera();
            
            #region Adjustments
            List<int> cornerIndexList = new List<int>();
            if (Input.GetKeyDown(addRowKey)||Input.GetKeyDown(removeRowKey)||Input.GetKeyDown(addColumnKey)||Input.GetKeyDown(removeColumnKey)){
                //check pre-change selected vertex
                
                for(int i=0;i<system.projectionCameras.Count;i++){
                    int cornerType = -1;
                    if(system.projectionCameras[i].selectedVertex==0){
                        cornerType = 0;
                    }
                    else if(system.projectionCameras[i].selectedVertex==system.projectionCameras[i].xDivisions){
                        cornerType = 1;
                    }
                    else if(system.projectionCameras[i].selectedVertex==((system.projectionCameras[i].yDivisions)*(system.projectionCameras[i].xDivisions+1))){
                        cornerType = 2;
                    }
                    else if(system.projectionCameras[i].selectedVertex==((system.projectionCameras[i].yDivisions+1)*(system.projectionCameras[i].xDivisions+1)-1)){
                        cornerType = 3;
                    }
                    cornerIndexList.Add(cornerType);
                    
                }
                
            }
            
            if (Input.GetKeyDown(addRowKey))
            {
                system.yDivisions++;
                for(int i=0;i<system.projectionCameras.Count;i++){
                    system.projectionCameras[i].yDivisions++;
                    system.projectionCameras[i].Rebuild();
                }
            }
            if (Input.GetKeyDown(removeRowKey))
            {
                
                if(system.yDivisions>1){
                    system.yDivisions--;
                    for(int i=0;i<system.projectionCameras.Count;i++){
                        system.projectionCameras[i].yDivisions--;
                        system.projectionCameras[i].Rebuild();
                    }
                }
                
            }
            if (Input.GetKeyDown(addColumnKey))
            {
                system.xDivisions++;
                for(int i=0;i<system.projectionCameras.Count;i++){
                    system.projectionCameras[i].xDivisions++;
                    system.projectionCameras[i].Rebuild();
                }
            }
            if (Input.GetKeyDown(removeColumnKey))
            {
                if(system.xDivisions>1){
                    system.xDivisions--;
                    for(int i=0;i<system.projectionCameras.Count;i++){
                        system.projectionCameras[i].xDivisions--;
                        system.projectionCameras[i].Rebuild();
                    }
                }
            }
            
            if (Input.GetKeyDown(addRowKey)||Input.GetKeyDown(removeRowKey)||Input.GetKeyDown(addColumnKey)||Input.GetKeyDown(removeColumnKey)){
                
                
                for(int i=0;i<system.projectionCameras.Count;i++){
                    switch(cornerIndexList[i]){
                        case 0:
                            system.projectionCameras[i].selectedVertex = 0;
                            break;
                        case 1:
                            system.projectionCameras[i].selectedVertex = system.projectionCameras[i].xDivisions;
                            break;
                        case 2:
                            system.projectionCameras[i].selectedVertex = (system.projectionCameras[i].yDivisions)*(system.projectionCameras[i].xDivisions+1);
                            break;
                        case 3:
                            system.projectionCameras[i].selectedVertex = (system.projectionCameras[i].yDivisions+1)*(system.projectionCameras[i].xDivisions+1)-1;
                            break;
                        default:
                            system.projectionCameras[i].selectedVertex = 0;
                            break;
                    }
                }
            }

            
            #endregion

            #region Visibility Toggles
            
            if (Input.GetKeyDown(uiToggleKey))
            {
                system.showProjectionWarpGUI = !system.showProjectionWarpGUI;
                system.UpdateProjectionWarpGUI();
            }
            if (Input.GetKeyDown(mouseToggleKey))
            {
                system.showMouseCursor = !system.showMouseCursor;
                system.UpdateCursor();
            }
            if (Input.GetKeyDown(gridToggleKey))
            {
                if(mesh!=null) mesh.ToggleBaseGrid();
            }
            if (Input.GetKeyDown(selectedGridToggleKey))
            {
                if (mesh != null) mesh.ToggleSelectedGrid();
            }
            if (Input.GetKeyDown(controlPointsToggleKey))
            {
                if (mesh != null) mesh.ToggleControlPoints();
            }
            if (Input.GetKeyDown(selectedControlPointsToggleKey))
            {
                if (mesh != null) mesh.ToggleSelectedControlPoints();
            }
            #endregion

            #region UI Toggles
            if (Input.GetKeyDown(helpKey))
            {
                system.calibrationManager.OnToggleHelp();
            }

            if (Input.GetKeyDown(noModeKey))
            {
                system.SetEditMode(ProjectionMesh.MeshEditMode.NONE);
            }
            else if (Input.GetKeyDown(cornerModeKey))
            {
                system.SetEditMode(ProjectionMesh.MeshEditMode.CORNERS);
            }
            else if (Input.GetKeyDown(rowModeKey))
            {
                system.SetEditMode(ProjectionMesh.MeshEditMode.ROWS);
            }
            else if (Input.GetKeyDown(columnModeKey))
            {
                system.SetEditMode(ProjectionMesh.MeshEditMode.COLUMNS);
            }
            else if (Input.GetKeyDown(pointModeKey))
            {
                system.SetEditMode(ProjectionMesh.MeshEditMode.POINTS);
            }

            if (Input.GetKeyDown(blendingModeKey))
            {
                system.calibrationManager.OnToggleBlend();
            }
            else if (Input.GetKeyDown(whiteBalanceModeKey))
            {
                system.calibrationManager.OnToggleWhiteBalance();
            }
            else if (Input.GetKeyDown(cameraSettingsKey)){
                system.calibrationManager.OnToggleCameraSettings();
            }
            #endregion

            #region Projector Selection
            if (Input.GetKeyDown(projectorAKey) || Input.GetKeyDown(projectorAAltKey)) {
                system.SelectProjector(0, true);
            }
            else if (Input.GetKeyDown(projectorBKey) || Input.GetKeyDown(projectorBAltKey))
            {
                system.SelectProjector(1, true);
            }
            else if (Input.GetKeyDown(projectorCKey) || Input.GetKeyDown(projectorCAltKey))
            {
                system.SelectProjector(2, true);
            }
            else if (Input.GetKeyDown(projectorDKey) || Input.GetKeyDown(projectorDAltKey))
            {
                system.SelectProjector(3, true);
            }
            else if (Input.GetKeyDown(projectorEKey) || Input.GetKeyDown(projectorEAltKey))
            {
                system.SelectProjector(4, true);
            }
            else if (Input.GetKeyDown(projectorFKey) || Input.GetKeyDown(projectorFAltKey))
            {
                system.SelectProjector(5, true);
            }
            else if (Input.GetKeyDown(projectorGKey) || Input.GetKeyDown(projectorGAltKey))
            {
                system.SelectProjector(6, true);
            }
            else if (Input.GetKeyDown(projectorHKey) || Input.GetKeyDown(projectorHAltKey))
            {
                system.SelectProjector(7, true);
            }

            #endregion

            #region Selection Activation
            if (mesh != null)
            {
                if (mesh.editMode != ProjectionMesh.MeshEditMode.NONE)
                {
                    if (Input.GetKeyDown(activateKey))
                    {
                        if (mesh.selectionActive)
                        {
                            mesh.DeactivateSelection();
                        }
                        else
                        {
                            mesh.ActivateSelection();
                        }
                    }
                }

            }

            #endregion

            #region Point Selection and Adjustment

            //corner indexes are at - 0, xDivisions, (xDivisions+1) * yDivisions, (xDivisions+1) * (yDivisions+1)-1
            float adjustmentScale = 1f;

            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) )
            {
                adjustmentScale = 0.001f;
            }
            if (Input.GetKey(KeyCode.LeftControl)|| Input.GetKey(KeyCode.RightControl))
            {
                adjustmentScale = 0.01f;
            }
            else if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                adjustmentScale = 0.1f;
            }

            if (mesh != null)
            {
                switch (mesh.editMode)
                {
                    case ProjectionMesh.MeshEditMode.CORNERS:
                        if (mesh.selectedVertex < 0) mesh.selectedVertex = 0;

                        //navigate control point corners
                        if (!mesh.selectionActive)
                        {
                            if (Input.GetKeyDown(upKey))
                            {
                                if (mesh.selectedVertex > system.xDivisions)
                                {
                                    mesh.selectedVertex -= ((system.xDivisions + 1) * system.yDivisions);
                                }
                            }
                            if (Input.GetKeyDown(downKey))
                            {
                                if (mesh.selectedVertex < system.xDivisions + 1)
                                {
                                    mesh.selectedVertex += ((system.xDivisions + 1) * system.yDivisions);
                                }
                            }
                            if (Input.GetKeyDown(leftKey))
                            {
                                if (mesh.selectedVertex == system.xDivisions || mesh.selectedVertex == ((system.xDivisions + 1) * (system.yDivisions + 1) - 1))
                                {
                                    mesh.selectedVertex -= system.xDivisions;
                                }
                            }
                            if (Input.GetKeyDown(rightKey))
                            {
                                if (mesh.selectedVertex == 0 || mesh.selectedVertex == ((system.xDivisions + 1) * system.yDivisions))
                                {
                                    mesh.selectedVertex += system.xDivisions;
                                }
                            }

                            
                        }

                        //move control points
                        else
                        {
                            int cornerIndex = -1;
                            if (mesh.selectedVertex == 0) cornerIndex = 0;
                            else if (mesh.selectedVertex == mesh.xDivisions) cornerIndex = 1;
                            else if (mesh.selectedVertex == ((mesh.xDivisions + 1) * mesh.yDivisions)) cornerIndex = 2;
                            else if (mesh.selectedVertex == ((mesh.xDivisions + 1) * (mesh.yDivisions + 1) - 1)) cornerIndex = 3;

                            if(cornerIndex>=0){
                                Vector2 point = mesh.cornerOffset[cornerIndex];


                                if (Input.GetKeyDown(upKey))
                                {
                                    mesh.cornerOffset[cornerIndex] = new Vector2(point.x, point.y + (0.1f * adjustmentScale));
                                }
                                if (Input.GetKeyDown(downKey))
                                {
                                    mesh.cornerOffset[cornerIndex] = new Vector2(point.x, point.y - (0.1f * adjustmentScale));
                                }
                                if (Input.GetKeyDown(leftKey))
                                {
                                    mesh.cornerOffset[cornerIndex] = new Vector2(point.x - (0.1f * adjustmentScale), point.y);
                                }
                                if (Input.GetKeyDown(rightKey))
                                {
                                    mesh.cornerOffset[cornerIndex] = new Vector2(point.x + (0.1f * adjustmentScale), point.y);
                                }
                                if (Input.GetKey(zeroOffsetKey))
                                {
                                    mesh.cornerOffset[cornerIndex] = Vector2.zero;
                                }
                            }
                            
                        }


                        break;
                    case ProjectionMesh.MeshEditMode.ROWS:
                        if (mesh.selectedVertex < 0) mesh.selectedVertex = 0;

                        //up and down selects row
                        if (!mesh.selectionActive)
                        {
                            if (Input.GetKeyDown(upKey))
                            {
                                if (mesh.selectedVertex > system.xDivisions)
                                {
                                    mesh.selectedVertex -= (system.xDivisions + 1);
                                }
                            }
                            if (Input.GetKeyDown(downKey))
                            {
                                if (mesh.selectedVertex < ((system.xDivisions + 1) * system.yDivisions))
                                {
                                    mesh.selectedVertex += (system.xDivisions + 1);
                                }
                            }
                        }

                        //move row around
                        else
                        {
                            //figured out which row is being moved
                            int rowIndex = -1;
                            rowIndex = (int)Mathf.Floor(mesh.selectedVertex / (mesh.xDivisions + 1));
                            Vector2 point;
                            int index;

                            //Vector2 point = mesh.rowOffset[rowIndex];
                            if (Input.GetKeyDown(upKey))
                            {
                                for (var i = 0; i < mesh.xDivisions + 1; i++)
                                {
                                    index = (rowIndex * (mesh.xDivisions + 1)) + i;
                                    point = mesh.pointOffset[index];
                                    mesh.pointOffset[index] = new Vector2(point.x, point.y + (0.1f * adjustmentScale));
                                }

                                //mesh.rowOffset[rowIndex] = new Vector2(point.x, point.y + (0.1f * adjustmentScale));
                            }
                            if (Input.GetKeyDown(downKey))
                            {
                                for (var i = 0; i < mesh.xDivisions + 1; i++)
                                {
                                    index = (rowIndex * (mesh.xDivisions + 1)) + i;
                                    point = mesh.pointOffset[index];
                                    mesh.pointOffset[index] = new Vector2(point.x, point.y - (0.1f * adjustmentScale));
                                }
                                //mesh.rowOffset[rowIndex] = new Vector2(point.x, point.y - (0.1f * adjustmentScale));
                            }
                            if (Input.GetKeyDown(leftKey))
                            {
                                for (var i = 0; i < mesh.xDivisions + 1; i++)
                                {
                                    index = (rowIndex * (mesh.xDivisions + 1)) + i;
                                    point = mesh.pointOffset[index];
                                    mesh.pointOffset[index] = new Vector2(point.x - (0.1f * adjustmentScale), point.y);
                                }
                                //mesh.rowOffset[rowIndex] = new Vector2(point.x - (0.1f * adjustmentScale), point.y);
                            }
                            if (Input.GetKeyDown(rightKey))
                            {
                                for (var i = 0; i < mesh.xDivisions + 1; i++)
                                {
                                    index = (rowIndex * (mesh.xDivisions + 1)) + i;
                                    point = mesh.pointOffset[index];
                                    mesh.pointOffset[index] = new Vector2(point.x + (0.1f * adjustmentScale), point.y);
                                }
                                //mesh.rowOffset[rowIndex] = new Vector2(point.x + (0.1f * adjustmentScale), point.y);
                            }

                            if (Input.GetKey(zeroOffsetKey))
                            {
                                for (var i = 0; i < mesh.xDivisions + 1; i++)
                                {
                                    index = (rowIndex * (mesh.xDivisions + 1)) + i;
                                    mesh.pointOffset[index] = Vector2.zero;
                                }
                            }
                        }


                        break;
                    case ProjectionMesh.MeshEditMode.COLUMNS:
                        if (mesh.selectedVertex < 0) mesh.selectedVertex = 0;

                        //left and right selects column
                        if (!mesh.selectionActive)
                        {
                            if (Input.GetKeyDown(leftKey))
                            {
                                if (mesh.selectedVertex % (system.xDivisions + 1) != 0)
                                {
                                    mesh.selectedVertex -= 1;
                                }
                            }
                            if (Input.GetKeyDown(rightKey))
                            {
                                if (mesh.selectedVertex % (system.xDivisions + 1) != system.xDivisions)
                                {
                                    mesh.selectedVertex += 1;
                                }
                            }
                        }

                        //move column around
                        else
                        {
                            //figured out which column is being moved
                            int columnIndex = -1;
                            columnIndex = mesh.selectedVertex % (mesh.xDivisions + 1);
                            Vector2 point;
                            int index;

                            if (Input.GetKeyDown(upKey))
                            {
                                for (var i = 0; i < mesh.yDivisions + 1; i++)
                                {
                                    index = (i * (mesh.xDivisions + 1)) + columnIndex;
                                    point = mesh.pointOffset[index];
                                    mesh.pointOffset[index] = new Vector2(point.x, point.y + (0.1f * adjustmentScale));
                                }
                                //mesh.columnOffset[columnIndex] = new Vector2(point.x, point.y + (0.1f * adjustmentScale));
                            }
                            if (Input.GetKeyDown(downKey))
                            {
                                for (var i = 0; i < mesh.yDivisions + 1; i++)
                                {
                                    index = (i * (mesh.xDivisions + 1)) + columnIndex;
                                    point = mesh.pointOffset[index];
                                    mesh.pointOffset[index] = new Vector2(point.x, point.y - (0.1f * adjustmentScale));
                                }
                                //mesh.columnOffset[columnIndex] = new Vector2(point.x, point.y - (0.1f * adjustmentScale));
                            }
                            if (Input.GetKeyDown(leftKey))
                            {
                                for (var i = 0; i < mesh.yDivisions + 1; i++)
                                {
                                    index = (i * (mesh.xDivisions + 1)) + columnIndex;
                                    point = mesh.pointOffset[index];
                                    mesh.pointOffset[index] = new Vector2(point.x - (0.1f * adjustmentScale), point.y);
                                }
                                //mesh.columnOffset[columnIndex] = new Vector2(point.x - (0.1f * adjustmentScale), point.y);
                            }
                            if (Input.GetKeyDown(rightKey))
                            {
                                for (var i = 0; i < mesh.yDivisions + 1; i++)
                                {
                                    index = (i * (mesh.xDivisions + 1)) + columnIndex;
                                    point = mesh.pointOffset[index];
                                    mesh.pointOffset[index] = new Vector2(point.x + (0.1f * adjustmentScale), point.y);
                                }
                                //mesh.columnOffset[columnIndex] = new Vector2(point.x + (0.1f * adjustmentScale), point.y);
                            }
                            if (Input.GetKey(zeroOffsetKey))
                            {
                                for(var i = 0; i < mesh.yDivisions + 1; i++)
                                {
                                    index = (i * (mesh.xDivisions + 1)) + columnIndex;
                                    mesh.pointOffset[index] = Vector2.zero;
                                }
                            }
                        }



                        break;
                    case ProjectionMesh.MeshEditMode.POINTS:
                        if (mesh.selectedVertex < 0) mesh.selectedVertex = 0;

                        if (!mesh.selectionActive)
                        {
                            if (Input.GetKeyDown(upKey))
                            {
                                if (mesh.selectedVertex > system.xDivisions)
                                {
                                    mesh.selectedVertex -= (system.xDivisions + 1);
                                }
                            }
                            if (Input.GetKeyDown(downKey))
                            {
                                if (mesh.selectedVertex < ((system.xDivisions + 1) * system.yDivisions))
                                {
                                    mesh.selectedVertex += (system.xDivisions + 1);
                                }
                            }
                            if (Input.GetKeyDown(leftKey))
                            {
                                if (mesh.selectedVertex % (system.xDivisions + 1) != 0)
                                {
                                    mesh.selectedVertex -= 1;
                                }
                            }
                            if (Input.GetKeyDown(rightKey))
                            {
                                if (mesh.selectedVertex % (system.xDivisions + 1) != system.xDivisions)
                                {
                                    mesh.selectedVertex += 1;
                                }
                            }
                        }
                        else
                        {
                            //figured out which row is being moved

                            Vector2 point = mesh.pointOffset[mesh.selectedVertex];

                            if (Input.GetKeyDown(upKey))
                            {
                                mesh.pointOffset[mesh.selectedVertex] = new Vector2(point.x, point.y + (0.1f * adjustmentScale));
                            }
                            if (Input.GetKeyDown(downKey))
                            {
                                mesh.pointOffset[mesh.selectedVertex] = new Vector2(point.x, point.y - (0.1f * adjustmentScale));
                            }
                            if (Input.GetKeyDown(leftKey))
                            {
                                mesh.pointOffset[mesh.selectedVertex] = new Vector2(point.x - (0.1f * adjustmentScale), point.y);
                            }
                            if (Input.GetKeyDown(rightKey))
                            {
                                mesh.pointOffset[mesh.selectedVertex] = new Vector2(point.x + (0.1f * adjustmentScale), point.y);
                            }
                            if (Input.GetKey(zeroOffsetKey))
                            {
                                mesh.pointOffset[mesh.selectedVertex] = Vector2.zero;
                            }
                        }

                        break;
                    default:
                    case ProjectionMesh.MeshEditMode.NONE:
                        break;
                }
            }
            

            #endregion

            if (Input.anyKeyDown)
            {
                //only rebuild when keys have been pressed
                if (mesh != null)
                {
                    mesh.UpdateMeshVertices();
                    mesh.CreateBaseGridLines();
                    mesh.HighlightSelection();
                    mesh.UpdateSelectedLines();
                }
                
            }
        }
    }
}