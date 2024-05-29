using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForTest_cam : MonoBehaviour
{
    Camera cam;
    float size;
    public GameObject d;
	private void Awake()
	{
        cam = this.GetComponent<Camera>();
        size = cam.orthographicSize;
        float width = Screen.width, height = Screen.height;
        float size_width = size * (width / height), size_height = size;

        for (int i = 0; i < 4; i++)
        {
            float x = 0, y = 0;
            GameObject dummy = GameObject.Instantiate(d);
            switch (i)
            {
                case 0: // left top
                    x = -size_width;
                    y = size_height;
                    break; 
                case 1: // right top
                    x = size_width;
                    y = size_height;
                    break; 
                case 2: // left bottom
                    x = -size_width;
                    y = -size_height;
                    break; 
                case 3: // right bottom
                    x = size_width;
                    y = -size_height;
                    break;
            }
            dummy.transform.position = this.transform.position + new Vector3(x,0,y);
            
        }
	}
	// Start is called before the first frame update
	void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
