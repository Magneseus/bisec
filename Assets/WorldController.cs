using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldController : MonoBehaviour
{
    public Bisec cube;
    public GameObject planeOne;
    public GameObject planeTwo;

    private Vector3 translate;
    private Bisec.b_Plane plane_;

    // Use this for initialization
    void Start ()
    {
        
    }
	
	// Update is called once per frame
	void Update ()
    {
		if (Input.GetKeyDown(KeyCode.E))
        {
            translate = planeOne.transform.position - planeTwo.transform.position;

            plane_.location = planeOne.transform.position;
            plane_.normal = Vector3.up;
            plane_.normal = planeOne.transform.rotation * plane_.normal;

            cube.Expand(plane_, translate);
        }
	}
}
