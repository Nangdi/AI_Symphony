using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace MultiProjectorWarpSystem
{
    [RequireComponent(typeof(Animator))]
    public class ProjectionMesh : MonoBehaviour
    {
        public int meshIndex = 0;
        const float NEAR_FAR_INSET = 0.001f;
        public float planeDistance = 10f;
        
        public enum MeshEditMode
        {
            NONE,
            CORNERS,
            ROWS,
            COLUMNS,
            POINTS
        }
        [Header("Parameters")]
        
        public MeshEditMode editMode;
        public bool selectionActive;
        public int selectedVertex;

        public int xDivisions;
        public int yDivisions;

        public float width;
        public float height;

        public Vector2[] cornerOffset = new Vector2[4];
        //public Vector2[] rowOffset;
        //public Vector2[] columnOffset;
        public Vector2[] pointOffset;

        public float indexAppearDuration = 3;
        public float timer;

        [Header("Colors")]
        public Material unselectedGridLineMaterial;
        public Material selectedGridLineMaterial;
        public Material activeGridLineMaterial;

        public Camera targetCamera;
        public Text projectorIndexText;

        [Header("Containers")]
        public Transform controlPointsContainer;
        public Transform baseRowLinesContainer;
        public Transform baseColumnLinesContainer;
        public Transform selectedRowLinesContainer;
        public Transform selectedColumnLinesContainer;

        [Header("Line Renderers")]
        public List<LineRenderer> baseRowLines;
        public List<LineRenderer> baseColumnLines;
        public List<LineRenderer> selectedRowLines;
        public List<LineRenderer> selectedColumnLines;

        public MeshFilter meshFilter;
        public MeshRenderer meshRenderer;

        [Header("Fade Controls")]
        public float topFadeRange;
        public float topFadeChoke;
        public float topFadeGamma;
        public float bottomFadeRange;
        public float bottomFadeChoke;
        public float bottomFadeGamma;
        public float leftFadeRange;
        public float leftFadeChoke;
        public float leftFadeGamma;
        public float rightFadeRange;
        public float rightFadeChoke;
        public float rightFadeGamma;


        [Header("Color Correction")]
        public Color tint;

        [Header("Gizmos")]
        public float sphereRadius;

        public Vector3[] vertices;
        public Vector3[] normals;
        public Vector2[] uv;
        public int[] triangles;

        int prevXDivision;
        int prevYDivision;
        float prevWidth;
        float prevHeight;
        Animator animator;

        public bool showGrid;
        public bool showSelectedGrid;
        public bool showControlPoints;
        public bool showSelectedControlPoints;
        
        public int editVertexIndex;

        public ControlPoint selectedControlPoint;

        List<ControlPoint> controlPoints = new List<ControlPoint>();

        void Start()
        {

            animator = GetComponent<Animator>();
            editVertexIndex = 0;
            UpdateUI();
            Material fadeMaterial = meshRenderer.sharedMaterial;

            if (fadeMaterial)
            {
                topFadeRange = fadeMaterial.GetFloat("_TopFadeRange");
                topFadeChoke = fadeMaterial.GetFloat("_TopFadeChoke");
                topFadeGamma = fadeMaterial.GetFloat("_TopFadeGamma");
                bottomFadeRange = fadeMaterial.GetFloat("_BottomFadeRange");
                bottomFadeChoke = fadeMaterial.GetFloat("_BottomFadeChoke");
                bottomFadeGamma = fadeMaterial.GetFloat("_BottomFadeGamma");
                leftFadeRange = fadeMaterial.GetFloat("_LeftFadeRange");
                leftFadeChoke = fadeMaterial.GetFloat("_LeftFadeChoke");
                leftFadeGamma = fadeMaterial.GetFloat("_LeftFadeGamma");
                rightFadeRange = fadeMaterial.GetFloat("_RightFadeRange");
                rightFadeChoke = fadeMaterial.GetFloat("_RightFadeChoke");
                rightFadeGamma = fadeMaterial.GetFloat("_RightFadeGamma");
            }

            //CreateMesh();
            Refresh();
        }
        

        public void Rebuild(){
            ClearControlPoints();
            ClearBaseGridLines();

            CreateMesh();
            BlendRefresh();

            if (showControlPoints) ShowControlPoints();
            else HideControlPoints();

            if (showSelectedControlPoints) ShowSelectedControlPoints();
            else HideSelectedControlPoints();
            
            if (showGrid) ShowBaseGrid();
            else HideBaseGrid();

            if (showSelectedGrid) ShowSelectedGrid();
            else HideSelectedGrid();

            //show selections
            HighlightSelection();
            UpdateSelectedLines();
        }
        #region BLEND
        public void UpdateBlend()
        {
            Material fadeMaterial = meshRenderer.sharedMaterial;

            if (fadeMaterial)
            {
                
                fadeMaterial.SetFloat("_TopFadeRange", topFadeRange);
                fadeMaterial.SetFloat("_TopFadeChoke", topFadeChoke);
                fadeMaterial.SetFloat("_TopFadeGamma", topFadeGamma);

                fadeMaterial.SetFloat("_BottomFadeRange", bottomFadeRange);
                fadeMaterial.SetFloat("_BottomFadeChoke", bottomFadeChoke);
                fadeMaterial.SetFloat("_BottomFadeGamma", bottomFadeGamma);

                fadeMaterial.SetFloat("_LeftFadeRange", leftFadeRange);
                fadeMaterial.SetFloat("_LeftFadeChoke", leftFadeChoke);
                fadeMaterial.SetFloat("_LeftFadeGamma", leftFadeGamma);

                fadeMaterial.SetFloat("_RightFadeRange", rightFadeRange);
                fadeMaterial.SetFloat("_RightFadeChoke", rightFadeChoke);
                fadeMaterial.SetFloat("_RightFadeGamma", rightFadeGamma);

                fadeMaterial.SetColor("_TintColor", tint);
            }
            
        }

        public void BlendRefresh()
        {
           
            UpdateBlend();
            
        }
        #endregion

        #region CONTROL POINTS
        public void ClearControlPoints()
        {
            int childCount = controlPointsContainer.childCount;

            for (int i = 0; i < childCount; i++)
            {
                DestroyImmediate(controlPointsContainer.GetChild(0).gameObject);
            }
            controlPoints = new List<ControlPoint>();

        }
        public void CreateControlPointsContainer()
        {
            ClearControlPoints();

            if (controlPointsContainer.childCount == 0)
            {
                GameObject controlPointPrefab = Resources.Load("Prefabs/Control Point") as GameObject;
                GameObject controlPoint;

                for (int i = 0; i < xDivisions + 1; i++)
                {
                    for (int j = 0; j < yDivisions + 1; j++)
                    {
                        controlPoint = (GameObject)Instantiate(controlPointPrefab, Vector3.zero, Quaternion.identity);
                        controlPoint.transform.SetParent(controlPointsContainer);
                        controlPoint.name = "Control Point (" + j + "," + i + ")";
                        controlPoints.Add(controlPoint.GetComponent<ControlPoint>());
                    }
                }

            }
        }
        public void ToggleSelectedControlPoints()
        {
            if (showSelectedControlPoints) HideSelectedControlPoints();
            else ShowSelectedControlPoints();
        }
        public void ShowSelectedControlPoints()
        {
            if (selectedControlPoint)
            {
                //check conditions for it to be shown
                if(editMode == MeshEditMode.CORNERS||editMode == MeshEditMode.POINTS)
                {
                    selectedControlPoint.gameObject.SetActive(true);
                }
                
            }
            showSelectedControlPoints = true;
        }
        public void HideSelectedControlPoints()
        {
            if(selectedControlPoint) selectedControlPoint.gameObject.SetActive(false);
            showSelectedControlPoints = false;
        }

        public void ToggleControlPoints()
        {
            if (showControlPoints) HideControlPoints();
            else ShowControlPoints();
        }
        public void ShowControlPoints()
        {
            controlPointsContainer.gameObject.SetActive(true);
            showControlPoints = true;
        }
        public void HideControlPoints()
        {
            controlPointsContainer.gameObject.SetActive(false);
            showControlPoints = false;
        }
        #endregion

        #region BASE GRID LINES
        public void ClearBaseGridLines()
        {
            ClearBaseRowLines();
            ClearBaseColumnLines();
        }
        public void ClearBaseRowLines()
        {
            int childCount = baseRowLinesContainer.childCount;

            for (int i = 0; i < childCount; i++)
            {
                DestroyImmediate(baseRowLinesContainer.GetChild(0).gameObject);
            }
            baseRowLines = new List<LineRenderer>();

        }
        public void ClearBaseColumnLines()
        {
            int childCount = baseColumnLinesContainer.childCount;

            for (int i = 0; i < childCount; i++)
            {
                DestroyImmediate(baseColumnLinesContainer.GetChild(0).gameObject);
            }
            baseColumnLines = new List<LineRenderer>();
        }
        public void CreateBaseRowLines()
        {
            ClearBaseRowLines();
            if (baseRowLinesContainer.childCount == 0)
            {
                GameObject gridLinePrefab = Resources.Load("Prefabs/Grid Line") as GameObject;
                GameObject gridLine;

                for (int i = 0; i < yDivisions + 1; i++)
                {
                    gridLine = (GameObject)Instantiate(gridLinePrefab, Vector3.zero, Quaternion.identity);
                    gridLine.transform.SetParent(baseRowLinesContainer);
                    gridLine.name = "Row Grid Line (" + i + ")";
                    LineRenderer lineRenderer = gridLine.GetComponent<LineRenderer>();
                    lineRenderer.sharedMaterial = unselectedGridLineMaterial;
                    lineRenderer.positionCount = xDivisions + 1;

                    baseRowLines.Add(lineRenderer);

                    for (int j = 0; j < xDivisions + 1; j++)
                    {
                        int index = i * (xDivisions + 1) + j;
                        lineRenderer.SetPosition(j, transform.position + vertices[index] + new Vector3(0, 0, -NEAR_FAR_INSET / 2f));
                    }
                }
            }
        }

        public void CreateBaseColumnLines()
        {
            ClearBaseColumnLines();

            if (baseColumnLinesContainer.childCount == 0)
            {
                GameObject gridLinePrefab = Resources.Load("Prefabs/Grid Line") as GameObject;
                GameObject gridLine;


                for (int i = 0; i < xDivisions + 1; i++)
                {
                    gridLine = (GameObject)Instantiate(gridLinePrefab, Vector3.zero, Quaternion.identity);
                    gridLine.transform.SetParent(baseColumnLinesContainer);
                    gridLine.name = "Column Grid Line (" + i + ")";
                    LineRenderer lineRenderer = gridLine.GetComponent<LineRenderer>();
                    lineRenderer.sharedMaterial = unselectedGridLineMaterial;
                    lineRenderer.positionCount = yDivisions + 1;

                    baseColumnLines.Add(lineRenderer);
                    
                    for (int j = 0; j < yDivisions + 1; j++)
                    {
                        int index = j * (xDivisions + 1) + i;
                        lineRenderer.SetPosition(j, transform.position + vertices[index] + new Vector3(0, 0, -NEAR_FAR_INSET / 2f));
                    }
                }

            }
        }
        public void CreateBaseGridLines()
        {
            CreateBaseRowLines();
            CreateBaseColumnLines();
        }

        public void ToggleBaseGrid()
        {
            if (showGrid) HideBaseGrid();
            else ShowBaseGrid();
        }
        public void ShowBaseGrid()
        {
            if (baseRowLinesContainer) baseRowLinesContainer.gameObject.SetActive(true);
            if (baseColumnLinesContainer) baseColumnLinesContainer.gameObject.SetActive(true);

            showGrid = true;
        }
        public void HideBaseGrid()
        {
            if (baseRowLinesContainer) baseRowLinesContainer.gameObject.SetActive(false);
            if (baseColumnLinesContainer) baseColumnLinesContainer.gameObject.SetActive(false);

            showGrid = false;
        }
        #endregion

        #region SELECTED GRID LINES
        public void ClearSelectedGridLines()
        {
            ClearSelectedRowLines();
            ClearSelectedColumnLines();
        }
        public void ClearSelectedRowLines()
        {
            int childCount = selectedRowLinesContainer.childCount;

            for (int i = 0; i < childCount; i++)
            {
                DestroyImmediate(selectedRowLinesContainer.GetChild(0).gameObject);
            }
            selectedRowLines = new List<LineRenderer>();

        }
        public void ClearSelectedColumnLines()
        {
            int childCount = selectedColumnLinesContainer.childCount;

            for (int i = 0; i < childCount; i++)
            {
                DestroyImmediate(selectedColumnLinesContainer.GetChild(0).gameObject);
            }
           selectedColumnLines = new List<LineRenderer>();
        }
        public void CreateSelectedRowLines()
        {
            ClearSelectedRowLines();
            if (selectedVertex < 0) return;

            if (selectedRowLinesContainer.childCount == 0)
            {
                GameObject gridLinePrefab = Resources.Load("Prefabs/Grid Line") as GameObject;
                GameObject gridLine;

                for (int i = 0; i < yDivisions + 1; i++)
                {
                    gridLine = (GameObject)Instantiate(gridLinePrefab, Vector3.zero, Quaternion.identity);
                    gridLine.transform.SetParent(selectedRowLinesContainer);
                    gridLine.name = "Row Grid Line (" + i + ")";
                    LineRenderer lineRenderer = gridLine.GetComponent<LineRenderer>();
                    if (selectionActive)
                    {
                        lineRenderer.sharedMaterial = activeGridLineMaterial;
                    }
                    else
                    {
                        lineRenderer.sharedMaterial = selectedGridLineMaterial;
                    }

                    lineRenderer.positionCount = xDivisions + 1;

                    selectedRowLines.Add(lineRenderer);

                    for (int j = 0; j < xDivisions + 1; j++)
                    {
                        int index = (i * (xDivisions + 1)) + j;
                        lineRenderer.SetPosition(j, transform.position + vertices[index] + new Vector3(0, 0, -NEAR_FAR_INSET *0.75f));
                    }
                }
            }
        }
        public void CreateSelectedRowLine()
        {
            ClearSelectedRowLines();
            if (selectedVertex < 0) return;

            if (selectedRowLinesContainer.childCount == 0)
            {
                GameObject gridLinePrefab = Resources.Load("Prefabs/Grid Line") as GameObject;
                GameObject gridLine;

                int selectedRow = Mathf.FloorToInt((float)selectedVertex / (float)(xDivisions + 1));

                gridLine = (GameObject)Instantiate(gridLinePrefab, Vector3.zero, Quaternion.identity);
                gridLine.transform.SetParent(selectedRowLinesContainer);
                gridLine.name = "Row Grid Line (" + selectedRow + ")";
                LineRenderer lineRenderer = gridLine.GetComponent<LineRenderer>();
                if (selectionActive)
                {
                    lineRenderer.sharedMaterial = activeGridLineMaterial;
                }
                else
                {
                    lineRenderer.sharedMaterial = selectedGridLineMaterial;
                }
                lineRenderer.positionCount = xDivisions + 1;

                selectedRowLines.Add(lineRenderer);

                for (int i = 0; i < xDivisions + 1; i++)
                {
                    int index = (selectedRow * (xDivisions + 1)) + i;
                    lineRenderer.SetPosition(i, transform.position + vertices[index] + new Vector3(0, 0, -NEAR_FAR_INSET * 0.75f));
                }
                
            }
        }

        public void CreateSelectedColumnLines()
        {
            ClearSelectedColumnLines();
            if (selectedVertex < 0) return;

            if (selectedColumnLinesContainer.childCount == 0)
            {
                GameObject gridLinePrefab = Resources.Load("Prefabs/Grid Line") as GameObject;
                GameObject gridLine;

                for (int i = 0; i < xDivisions + 1; i++)
                {
                    gridLine = (GameObject)Instantiate(gridLinePrefab, Vector3.zero, Quaternion.identity);
                    gridLine.transform.SetParent(selectedColumnLinesContainer);
                    gridLine.name = "Column Grid Line (" + i + ")";
                    LineRenderer lineRenderer = gridLine.GetComponent<LineRenderer>();
                    if (selectionActive)
                    {
                        lineRenderer.sharedMaterial = activeGridLineMaterial;
                    }
                    else
                    {
                        lineRenderer.sharedMaterial = selectedGridLineMaterial;
                    }
                    lineRenderer.positionCount = yDivisions + 1;

                    selectedColumnLines.Add(lineRenderer);

                    for (int j = 0; j < yDivisions + 1; j++)
                    {
                        int index = j * (xDivisions + 1) + i;
                        lineRenderer.SetPosition(j, transform.position + vertices[index] + new Vector3(0, 0, -NEAR_FAR_INSET * 0.75f));
                    }
                }

            }
        }

        public void CreateSelectedColumnLine()
        {

            ClearSelectedColumnLines();
            if (selectedVertex < 0) return;
            if (selectedColumnLinesContainer.childCount == 0)
            {
                GameObject gridLinePrefab = Resources.Load("Prefabs/Grid Line") as GameObject;
                GameObject gridLine;

                int selectedColumn = selectedVertex % (xDivisions + 1);

                gridLine = (GameObject)Instantiate(gridLinePrefab, Vector3.zero, Quaternion.identity);
                gridLine.transform.SetParent(selectedColumnLinesContainer);
                gridLine.name = "Column Grid Line (" + selectedColumn + ")";
                LineRenderer lineRenderer = gridLine.GetComponent<LineRenderer>();
                if (selectionActive)
                {
                    lineRenderer.sharedMaterial = activeGridLineMaterial;
                }
                else
                {
                    lineRenderer.sharedMaterial = selectedGridLineMaterial;
                }
                lineRenderer.positionCount = yDivisions + 1;

                selectedRowLines.Add(lineRenderer);

                for (int i = 0; i < yDivisions + 1; i++)
                {
                    int index = (i * (xDivisions + 1)) + selectedColumn;
                    lineRenderer.SetPosition(i, transform.position + vertices[index] + new Vector3(0, 0, -NEAR_FAR_INSET * 0.75f));
                }

            }
        }

        public void CreateSelectedPoint()
        {
            CreateSelectedColumnLine();
            CreateSelectedRowLine();
        }

        public void CornerEditMode()
        {
            HighlightSelection();
            editMode = MeshEditMode.CORNERS;
        }
        public void RowEditMode()
        {

            HighlightSelection();
            editMode = MeshEditMode.ROWS;

        }
        public void ColumnEditMode()
        {
            HighlightSelection();
            editMode = MeshEditMode.COLUMNS;
        }
        public void PointEditMode()
        {

            HighlightSelection();
            editMode = MeshEditMode.POINTS;
        }

        public void UpdateSelectedLines()
        {
            ClearSelectedRowLines();
            ClearSelectedColumnLines();

            switch (editMode)
            {
                case MeshEditMode.CORNERS:
                    CreateSelectedRowLines();
                    CreateSelectedColumnLines();
                    break;
                case MeshEditMode.ROWS:
                    CreateSelectedRowLine();
                    break;
                case MeshEditMode.COLUMNS:
                    CreateSelectedColumnLine();
                    break;
                case MeshEditMode.POINTS:
                    CreateSelectedPoint();
                    break;
                case MeshEditMode.NONE:
                default:
                    break;
            }
            
        }
        

        public void ToggleSelectedGrid()
        {
            if (showSelectedGrid) HideSelectedGrid();
            else ShowSelectedGrid();
        }
        public void ShowSelectedGrid()
        {
            if (selectedRowLinesContainer) selectedRowLinesContainer.gameObject.SetActive(true);
            if (selectedColumnLinesContainer) selectedColumnLinesContainer.gameObject.SetActive(true);

            showSelectedGrid = true;
        }
        public void HideSelectedGrid()
        {
            if (selectedRowLinesContainer) selectedRowLinesContainer.gameObject.SetActive(false);
            if (selectedColumnLinesContainer) selectedColumnLinesContainer.gameObject.SetActive(false);

            showSelectedGrid = false;
        }

        #endregion


        public void SetEditMode(MeshEditMode mode)
        {
            
            DeactivateSelection();
            
            editMode = mode;
            
            UpdateSelectedLines();
            
            HighlightSelection();
            
        }
        public void ActivateSelection()
        {
            selectionActive = true;
            if (selectedVertex < 0) return;

            selectedControlPoint.Activate();

            switch (editMode)
            {

                case MeshEditMode.ROWS:
                    for (int i = 0; i < selectedRowLines.Count; i++)
                    {
                        selectedRowLines[i].sharedMaterial = activeGridLineMaterial;
                    }
                    break;
                case MeshEditMode.COLUMNS:
                    for (int i = 0; i < selectedColumnLines.Count; i++)
                    {
                        selectedColumnLines[i].sharedMaterial = activeGridLineMaterial;
                    }
                    break;
                case MeshEditMode.POINTS:
                    for (int i = 0; i < selectedRowLines.Count; i++)
                    {
                        selectedRowLines[i].sharedMaterial = activeGridLineMaterial;
                    }
                    for (int i = 0; i < selectedColumnLines.Count; i++)
                    {
                        selectedColumnLines[i].sharedMaterial = activeGridLineMaterial;
                    }
                    break;
                case MeshEditMode.CORNERS:
                case MeshEditMode.NONE:
                default:
                    break;
            }
        }
        public void DeactivateSelection()
        {
            selectionActive = false;
            if (selectedVertex < 0) return;


            selectedControlPoint.Select();

            for (int i = 0; i < selectedRowLines.Count; i++)
            {
                selectedRowLines[i].sharedMaterial = selectedGridLineMaterial;
            }
            for (int i = 0; i < selectedColumnLines.Count; i++)
            {
                selectedColumnLines[i].sharedMaterial = selectedGridLineMaterial;
            }


            switch (editMode)
            {
                case MeshEditMode.CORNERS:

                    break;
                case MeshEditMode.ROWS:

                    break;
                case MeshEditMode.COLUMNS:

                    break;
                case MeshEditMode.POINTS:

                    break;
                case MeshEditMode.NONE:
                default:
                    break;
            }
        }

        public void HighlightSelection()
        {
            
            selectedControlPoint.gameObject.SetActive(!(selectedVertex<0)&& 
                showSelectedControlPoints && 
                (editMode==MeshEditMode.CORNERS||editMode==MeshEditMode.POINTS));
            
            if (selectedVertex < 0)
            {
                return;
            }

            //focus on selected control point
            //controlPoints[selectedVertex].Select();
            if (controlPoints.Count == 0) UpdateMeshVertices();
            if (selectedVertex>=0&&selectedVertex<controlPoints.Count) selectedControlPoint.transform.position = controlPoints[selectedVertex].transform.position;
            
        }

        public void UpdateMeshVertices()
        {
            //calculate corners positions as anchors
            int topLeftIndex = 0;
            int topRightIndex = xDivisions;
            int bottomLeftIndex = (xDivisions + 1) * yDivisions;
            int bottomRightIndex = bottomLeftIndex + xDivisions;

            vertices[topLeftIndex] = new Vector3(-width / 2f, height / 2f, 0f) + new Vector3(cornerOffset[0].x,cornerOffset[0].y,0f);
            vertices[topRightIndex] = new Vector3(width / 2f, height / 2f, 0f) + new Vector3(cornerOffset[1].x, cornerOffset[1].y, 0f); ;
            vertices[bottomLeftIndex] = new Vector3(-width / 2f, -height / 2f, 0f) + new Vector3(cornerOffset[2].x, cornerOffset[2].y, 0f); ;
            vertices[bottomRightIndex] = new Vector3(width / 2f, -height / 2f, 0f) + new Vector3(cornerOffset[3].x, cornerOffset[3].y, 0f); ;

            float dx, dy;
            //top edge linear interpolation
            dx = (vertices[topRightIndex].x - vertices[topLeftIndex].x) / (float)xDivisions;
            dy = (vertices[topRightIndex].y - vertices[topLeftIndex].y) / (float)xDivisions;

            for (int i = 1; i < xDivisions; i++)
            { 
                vertices[i] = new Vector3(vertices[topLeftIndex].x + (dx * i), vertices[topLeftIndex].y + (dy * i), 0f);
            }
            
            //bottom edge linear interpolation
            dx = (vertices[bottomRightIndex].x - vertices[bottomLeftIndex].x) / (float)xDivisions;
            dy = (vertices[bottomRightIndex].y - vertices[bottomLeftIndex].y) / (float)xDivisions;
            for (int i = 1; i < xDivisions; i++)
            {
                int index = bottomLeftIndex + i;
                vertices[index] = new Vector3(vertices[bottomLeftIndex].x + (dx * i), vertices[bottomLeftIndex].y + (dy * i), 0f);
            }


            //left edge linear interpolation
            dx = (vertices[bottomLeftIndex].x - vertices[topLeftIndex].x) / (float)yDivisions;
            dy = (vertices[bottomLeftIndex].y - vertices[topLeftIndex].y) / (float)yDivisions;

            for (int i = 1; i < yDivisions; i++)
            {
                int index = (xDivisions+1) * i;
                vertices[index] = new Vector3(vertices[topLeftIndex].x + (dx * i), vertices[topLeftIndex].y + (dy * i), 0f);
            }

            //right edge linear interpolation
            dx = (vertices[bottomRightIndex].x - vertices[topRightIndex].x) / (float)yDivisions;
            dy = (vertices[bottomRightIndex].y - vertices[topRightIndex].y) / (float)yDivisions;

            for (int i = 1; i < yDivisions; i++)
            {
                int index = ((xDivisions + 1) * i) + xDivisions;
                vertices[index] = new Vector3(vertices[topRightIndex].x + (dx * i), vertices[topRightIndex].y + (dy * i), 0f);
            }
            
            //middle points interpolation
            for (int i = 1; i < yDivisions; i++)
            {
                for (int j = 1; j < xDivisions; j++)
                {
                    int index = i * (xDivisions + 1) + j;

                    int xLeftIndex = (Mathf.FloorToInt((float)index / ((float)xDivisions+1f))) * (xDivisions+1);
                    int xRightIndex = xLeftIndex + xDivisions;
                    int yTopIndex = index % (xDivisions+1);
                    int yBottomIndex = yTopIndex + ((xDivisions+1)*yDivisions);

                    dx = (vertices[xRightIndex].x - vertices[xLeftIndex].x) / (float)xDivisions;
                    dy = (vertices[yBottomIndex].y - vertices[yTopIndex].y) / (float)yDivisions;
                                        
                    vertices[index] = new Vector3(vertices[xLeftIndex].x + (dx * j), vertices[yTopIndex].y + (dy * i), 0f);
                }
            }

            //add row offset
            //int rowIndex = Mathf.FloorToInt(selectedVertex / (xDivisions + 1));
            /*
            for (int i = 0; i < yDivisions + 1; i++)
            {
                for (int j = 0; j < xDivisions + 1; j++)
                {
                    int index = i * (xDivisions + 1) + j;
                    vertices[index] += new Vector3(rowOffset[i].x,rowOffset[i].y,0f);
                }
            }


            //add column offset
            for (int i = 0; i < yDivisions+ 1; i++)
            {
                for (int j = 0; j < xDivisions + 1; j++)
                {
                    int index = i * (xDivisions + 1) + j;
                    vertices[index] += new Vector3(columnOffset[j].x, columnOffset[j].y, 0f);
                }
            }
            */
            //add point offset
            for (int i = 0; i < yDivisions + 1; i++)
            {
                for (int j = 0; j < xDivisions + 1; j++)
                {
                    int index = i * (xDivisions + 1) + j;
                    vertices[index] += new Vector3(pointOffset[index].x, pointOffset[index].y, 0f);
                }
            }


            if (controlPoints.Count == 0) CreateControlPointsContainer();
            
            for (int i = 0; i < yDivisions + 1; i++)
            {
                for (int j = 0; j < xDivisions + 1; j++)
                {
                    int index = i * (xDivisions + 1) + j;
                    controlPoints[index].transform.localPosition = vertices[index];
                }
            }
            

            /*
            Mesh newMesh = new Mesh();
            newMesh.vertices = vertices;
            newMesh.triangles = triangles;
            newMesh.uv = uv;
            newMesh.normals = normals;
            */
            if (meshFilter.sharedMesh != null){
                if (vertices.Length == meshFilter.sharedMesh.vertices.Length){
                    meshFilter.sharedMesh.vertices = vertices;
                }
                
            }
        }

        public void CreateMesh()
        {
            
            //clear existing data
            ClearControlPoints();
            ClearBaseGridLines();

            int vertexCount = (xDivisions + 1) * (yDivisions + 1);

            //only change tweak offsets if X Divisions Change
            if (xDivisions != prevXDivision || yDivisions != prevYDivision ||
                width != prevWidth || height != prevHeight )
            {
//                rowOffset = new Vector2[yDivisions + 1];
//                columnOffset = new Vector2[xDivisions + 1];
                pointOffset = new Vector2[(xDivisions + 1) * (yDivisions + 1)];
            }

            //create additional arrays to store offset information
            

            vertices = new Vector3[vertexCount];

            CreateControlPointsContainer();

            //Update vertex information
            UpdateMeshVertices();
            
            #region Normals, UV, Index
            //normals
            normals = new Vector3[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                normals[i] = new Vector3(0, 0, -1f);
            }

            //uv
            uv = new Vector2[vertexCount];

            for (int i = 0; i < yDivisions + 1; i++)
            {
                for (int j = 0; j < xDivisions + 1; j++)
                {
                    int index = i * (xDivisions + 1) + j;
                    float su = 0f;
                    float sv = 1f;
                    float du = 1f / (float)xDivisions;
                    float dv = 1f / (float)yDivisions;

                    uv[index] = new Vector3(su + ((float)j * du), sv - ((float)i * dv));
                    //Debug.Log(i+ " "+j+ " :"+uv[index]);
                }
            }

            //index
            triangles = new int[xDivisions * yDivisions * 2 * 3];
            int triangleIndex = 0;
            for (int i = 0; i < yDivisions; i++)
            {
                for (int j = 0; j < xDivisions; j++)
                {
                    int index = i * (xDivisions + 1) + j;
                    int right = index + 1;
                    int bottom = index + (xDivisions + 1);
                    int opposite = bottom + 1;

                    triangles[triangleIndex] = index;
                    triangleIndex++;
                    triangles[triangleIndex] = right;
                    triangleIndex++;
                    triangles[triangleIndex] = opposite;
                    triangleIndex++;
                    triangles[triangleIndex] = index;
                    triangleIndex++;
                    triangles[triangleIndex] = opposite;
                    triangleIndex++;
                    triangles[triangleIndex] = bottom;
                    triangleIndex++;

                }
            }

            #endregion

            CreateBaseGridLines();


            //create and assign mesh

            Mesh newMesh = new Mesh();
            newMesh.vertices = vertices;
            newMesh.triangles = triangles;
            newMesh.uv = uv;
            newMesh.normals = normals;

            meshFilter.mesh = newMesh;
            

            //position offset
            planeDistance = Mathf.Clamp(planeDistance, targetCamera.nearClipPlane + NEAR_FAR_INSET, targetCamera.farClipPlane - NEAR_FAR_INSET);
            transform.localPosition = new Vector3(0, 0, planeDistance);

            //control points show
            if (showControlPoints) ShowControlPoints();
            else HideControlPoints();

            if (showSelectedControlPoints) ShowSelectedControlPoints();
            else HideSelectedControlPoints();

            prevXDivision = xDivisions;
            prevYDivision = yDivisions;
            prevWidth = width;
            prevHeight = height;
        }

        public void ResetOffsets()
        {
            for(int i = 0; i < 4; i++)
            {
                cornerOffset[i] = Vector2.zero;
            }
            /*
            for (int i = 0; i < yDivisions + 1; i++)
            {
                rowOffset[i] = Vector2.zero;
            }
            for (int i = 0; i < xDivisions + 1; i++)
            {
                columnOffset[i] = Vector2.zero;
            }
            */
            for (int i = 0; i < (xDivisions + 1) * (yDivisions + 1); i++)
            {
                pointOffset[i] = Vector2.zero;
            }
        }
        
        public void Refresh()
        {
            BlendRefresh();
        }

        public void UpdateUI()
        {
    
        }

        public void ShowProjectorIndex()
        {
            timer = 0f;
            animator.SetBool("visible", true);
        }
        public void HidewProjectorIndex()
        {
            animator.SetBool("visible", false);
            timer = 0f;
        }
        void Update()
        {
            if (animator.GetBool("visible"))
            {
                timer += Time.deltaTime;
                if (timer >= indexAppearDuration)
                {
                    HidewProjectorIndex();
                }
            }
        }



        void OnDrawGizmos()
        {

        }

    }

}
