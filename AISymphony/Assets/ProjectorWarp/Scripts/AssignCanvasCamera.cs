using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MultiProjectorWarpSystem;

namespace MultiProjectorWarpSystem {

    public class AssignCanvasCamera : MonoBehaviour
    {
        public ProjectionWarpSystem projectionWarpSystem;
        [Range(1,8)]
        public int cameraIndex;
        public Canvas targetCanvas;

        void Start()
        {
            
        }

        void Update()
        {
            targetCanvas.worldCamera = projectionWarpSystem.sourceCameras[cameraIndex - 1].GetComponent<Camera>();
        }
    }
}