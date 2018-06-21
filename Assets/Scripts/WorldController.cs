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
    private Bisec.b_Plane plane2_;

    private List<int> oldTriangles;
    private List<Vector3> oldVerts;

    // Use this for initialization
    void Start ()
    {
        oldVerts = new List<Vector3>(cube.GetComponent<MeshFilter>().mesh.vertices);
        oldTriangles = new List<int>(cube.GetComponent<MeshFilter>().mesh.triangles);
    }
	
	// Update is called once per frame
	void Update ()
    {
		if (Input.GetKeyDown(KeyCode.E))
        {
            translate = planeTwo.transform.position - planeOne.transform.position;

            plane_.location = planeOne.transform.position;
            plane_.normal = Vector3.up;
            plane_.normal = planeOne.transform.rotation * plane_.normal;

            plane2_.location = planeTwo.transform.position;
            plane2_.normal = Vector3.up;
            plane2_.normal = planeOne.transform.rotation * plane2_.normal;

            //cube.Expand(plane_, plane2_);
            cube.Contract(plane_, plane2_, singleObject:true);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            cube.GetComponent<MeshFilter>().mesh.SetTriangles(oldTriangles, 0);
            cube.GetComponent<MeshFilter>().mesh.SetVertices(oldVerts);

            cube.GetComponent<MeshFilter>().mesh.RecalculateNormals();
            cube.GetComponent<MeshFilter>().mesh.RecalculateTangents();
            cube.NewMeshBuild();
        }
	}
}
