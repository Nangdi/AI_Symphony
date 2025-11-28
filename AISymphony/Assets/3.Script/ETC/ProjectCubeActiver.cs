using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectCubeActiver : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        gameObject.SetActive(JsonManager.instance.gameSettingData.activeCube);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
