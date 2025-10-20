using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MultiProjectorWarpSystem;
using UnityEngine.EventSystems;

public class ScreenToWorldCursor : MonoBehaviour {

	public Transform cursor;
	public ProjectionWarpSystem warpSystem;
	public Canvas worldSpaceCanvas;

	Vector3 point;
	Camera targetCamera;

	void Start () {
		//assign to first camera
		targetCamera = warpSystem.sourceCameras[0];
		worldSpaceCanvas.worldCamera = targetCamera;
	}
	void OnGUI(){
        Event currentEvent = Event.current;
        Vector2 mousePos = new Vector2();

        // Get the mouse position from Event.
        // Note that the y position from Event is inverted.
        mousePos.x = currentEvent.mousePosition.x;
        mousePos.y = targetCamera.pixelHeight - currentEvent.mousePosition.y;

		point = targetCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, targetCamera.nearClipPlane));

		
        GUILayout.BeginArea(new Rect(20, 20, 250, 120));
        GUILayout.Label("Screen pixels: " + targetCamera.pixelWidth + ":" + targetCamera.pixelHeight);
        GUILayout.Label("Mouse position: " + mousePos);
        GUILayout.Label("World position: " + point.ToString("F3"));
        GUILayout.EndArea();
		
		cursor.transform.position = point;
		
		//billboard cursor
		cursor.transform.LookAt(cursor.transform.position + targetCamera.transform.rotation * Vector3.forward,
            targetCamera.transform.rotation * Vector3.up);
		
	}	
	void OnDrawGizmos(){
		Gizmos.color = Color.yellow;
		if(targetCamera!=null)
			Gizmos.DrawLine(targetCamera.transform.position, targetCamera.transform.position + ((point-targetCamera.transform.position)*10f));
	}
	void Update () {
		//assign camera at runtime since the warp system generates it from configuration file
		if(targetCamera==null) targetCamera = warpSystem.sourceCameras[0];
		if(worldSpaceCanvas.worldCamera==null) worldSpaceCanvas.worldCamera = warpSystem.sourceCameras[0];
	}
}
