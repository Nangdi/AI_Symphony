using UnityEngine;
using System.Collections;

namespace MultiProjectorWarpSystem {
    public class ControlPoint : MonoBehaviour
    {
        public enum State { UNSELECTED, SELECTED, ACTIVE };
        public State state;

        Vector3 selectedScale = new Vector3(0.1f, 0.1f, 0.1f);
        Vector3 unselectedScale = new Vector3(0.05f, 0.05f, 0.05f);
        public Material activeMaterial;
        public Material selectedMaterial;
        public Material unselectedMaterial;

        MeshRenderer meshRenderer;

        void Start()
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }

        void Update()
        {
        
        }

        public void Activate()
        {
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = activeMaterial;
            transform.localScale = selectedScale;
            state = State.ACTIVE;
        }

        public void Select()
        {
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = selectedMaterial;
            transform.localScale = selectedScale;
            state = State.SELECTED;
            
        }
        public void Unselect()
        {
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = unselectedMaterial;
            transform.localScale = unselectedScale;
            state = State.UNSELECTED;
        }


    }

}

