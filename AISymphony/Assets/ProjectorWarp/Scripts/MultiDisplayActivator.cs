using UnityEngine;
using System.Collections;

namespace MultiProjectorWarpSystem
{
    public class MultiDisplayActivator : MonoBehaviour
    {
        public static MultiDisplayActivator instance;

        void Awake()
        {
            /*
            if (instance == null)
            {
                DontDestroyOnLoad(gameObject);
                instance = this;
                Load();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
             */
        }


        void Start()
        {
            //Debug.Log("displays connected: " + Display.displays.Length);
            // Display.displays[0] is the primary, default display and is always ON.
            // Check if additional displays are available and activate each.

            if (Display.displays.Length > 1)
            {
                for (int i = 1; i < Display.displays.Length; i++)
                {
                    Display.displays[i].Activate();
                }
            }
        }

        void Load()
        {

        }

        void Save()
        {

        }
        void Update()
        {

        }
    }
}


