using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MultiProjectorWarpSystem;
using UnityEngine.EventSystems;

public class ColorSampleToWorldCursor : MonoBehaviour {

	public ProjectionWarpSystem warpSystem;
	public Canvas cartesianCanvas;
	public Canvas worldSpaceCanvas;
	Camera targetCamera;
	

	Vector3 point;
	Color pixelColor;
	Vector2 mousePos;
	

	bool cartesianDirty = false;

	Texture2D cartesianTexture;
	void Start () {
		//assign to first camera
		targetCamera = warpSystem.sourceCameras[0];
		worldSpaceCanvas.worldCamera = targetCamera;
		cartesianCanvas.gameObject.SetActive(false);
		RefreshCartesian();
	}
	
	
	public void RefreshCartesian(){
		cartesianDirty = true;
	}
	void Update () {
		if(targetCamera==null){
			targetCamera = warpSystem.sourceCameras[0];
		}
		else{
			Rect nearPlane = NearPlaneDimensions(targetCamera);
			cartesianCanvas.GetComponent<RectTransform>().sizeDelta = new Vector2(nearPlane.xMax-nearPlane.xMin, nearPlane.yMax-nearPlane.yMin);

			cartesianCanvas.transform.position = targetCamera.transform.position + (targetCamera.transform.forward * (targetCamera.nearClipPlane+0.0001f));

			//billboard cursor
			cartesianCanvas.transform.LookAt(cartesianCanvas.transform.position + targetCamera.transform.rotation * Vector3.forward,
            	targetCamera.transform.rotation * Vector3.up);

			if(cartesianDirty){
				cartesianCanvas.gameObject.SetActive(true);
				warpSystem.sourceCameras[0].Render();
				RenderTexture renderTexture = new RenderTexture(Screen.width,Screen.height, 16, RenderTextureFormat.ARGB32);
				warpSystem.projectionCameras[0].targetCamera.targetTexture = renderTexture;
				cartesianTexture = RTImage(warpSystem.projectionCameras[0].targetCamera);
				warpSystem.projectionCameras[0].targetCamera.targetTexture = null;
				cartesianCanvas.gameObject.SetActive(false);
				cartesianDirty = false;
			}
			
		}
		if(cartesianCanvas.worldCamera==null) cartesianCanvas.worldCamera = warpSystem.sourceCameras[0];
		if(worldSpaceCanvas.worldCamera==null) worldSpaceCanvas.worldCamera = warpSystem.sourceCameras[0];

		if(Input.GetMouseButtonDown(0)){
			
		}

		if(cartesianTexture!=null){
			pixelColor = cartesianTexture.GetPixel((int)Input.mousePosition.x, (int)Input.mousePosition.y);

			if(pixelColor.g==0f||pixelColor.b==0f) mousePos = new Vector2(-1,-1);
			else mousePos = new Vector2(pixelColor.g*targetCamera.pixelWidth,(1f-pixelColor.b)*targetCamera.pixelHeight);
			
		}
		
		point = targetCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, targetCamera.nearClipPlane));

		PointerEventData pointerData = new PointerEventData(EventSystem.current);
		
		//pointerData.position = Input.mousePosition;
		pointerData.position = new Vector3(mousePos.x, mousePos.y,0f);
		
		List<RaycastResult> results = new List<RaycastResult>();
		 
		EventSystem.current.RaycastAll(pointerData, results);
		
		if (results.Count > 0) {
			string dbg = "Root Element: {0} \n GrandChild Element: {1}";
			Debug.Log(string.Format(dbg, results[results.Count-1].gameObject.name,results[0].gameObject.name));
			//Debug.Log("Root Element: "+results[results.Count-1].gameObject.name);
			//Debug.Log("GrandChild Element: "+results[0].gameObject.name);
			results.Clear();
			
		}
	}
	void OnGUI(){
		/*
        Event currentEvent = Event.current;
        currentEvent.mousePosition = new Vector2(0,0);

		GUILayout.BeginArea(new Rect(20, 20, 250, 120));
        GUILayout.Label("Screen pixels: " + targetCamera.pixelWidth + ":" + targetCamera.pixelHeight);
        GUILayout.Label("Mouse position: " + mousePos);
		GUILayout.Label("Color: " + pixelColor.ToString("F3"));
        GUILayout.Label("World position: " + point.ToString("F3"));
        GUILayout.EndArea();
		 */
		/*
        // Get the mouse position from Event.
        // Note that the y position from Event is inverted.
        mousePos.x = currentEvent.mousePosition.x;
        mousePos.y = targetCamera.pixelHeight - currentEvent.mousePosition.y;

		point = targetCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, targetCamera.nearClipPlane));
		*/
	}

	Texture2D ToTexture2D(RenderTexture rTex)
	{
		Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.RGB24, false);
		RenderTexture.active = rTex;
		tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
		tex.Apply();
		return tex;
	}

	Texture2D RTImage(Camera camera)
    {
        // The Render Texture in RenderTexture.active is the one
        // that will be read by ReadPixels.
        var currentRT = RenderTexture.active;
        RenderTexture.active = camera.targetTexture;

        // Render the camera's view.
        camera.Render();

        // Make a new texture and read the active Render Texture into it.
        Texture2D image = new Texture2D(camera.targetTexture.width, camera.targetTexture.height);
        image.ReadPixels(new Rect(0, 0, camera.targetTexture.width, camera.targetTexture.height), 0, 0);
        image.Apply();

        // Replace the original active Render Texture.
        RenderTexture.active = currentRT;
        return image;
    }
	

	void OnDrawGizmos(){
		Gizmos.color = Color.yellow;
		if(targetCamera!=null)
			Gizmos.DrawLine(targetCamera.transform.position, targetCamera.transform.position + ((point-targetCamera.transform.position)*10f));
	}


	Rect NearPlaneDimensions(Camera cam)
    {
        Rect r = new Rect();
        float a = cam.nearClipPlane;//get length
        float A = cam.fieldOfView * 0.5f;//get angle
        A = A * Mathf.Deg2Rad;//convert tor radians
        float h = (Mathf.Tan(A) * a);//calc height
        float w = (h / cam.pixelHeight) * cam.pixelWidth;//deduct width
 
        r.xMin = -w;
        r.xMax = w;
        r.yMin = -h;
        r.yMax = h;
        return r;
    }

}
