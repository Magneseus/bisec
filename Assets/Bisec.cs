using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bisec : MonoBehaviour {

    public struct b_Plane
    {
        public Vector3 location;
        public Vector3 normal;
    }

    private Mesh targetMesh;

	// Use this for initialization
	void Start () {
        targetMesh = GetComponent<MeshFilter>().mesh;

        Debug.Log("Sub mesh count: " + targetMesh.subMeshCount);
        Debug.Log("Vertex count: " + targetMesh.vertexCount);
    }
	
	// Update is called once per frame
	void Update () {
	}

    /*
     * When bisecting, should achieve a O(n) time, but could possibly reduce
     * by treating like a graph, jumping through nodes and only taking ones
     * that have a chance of intersecting. O(log n)?
     * 
     * Will have a edge case where line is laying on the plane.
     */
    void Bisect(Plane bisectPlane)
    {
       
    }

    // 0 -> no intersection
    // 1 -> single point intersection
    // 2 -> segment is laying on plane, inf. intersections
    public static int PlaneSegmentIntersection(b_Plane plane, Vector3 p1, Vector3 p2, 
        out Vector3 intersection)
    {
        const float coplanar_margin_err = 0.001f;

        // Default intersection to 0,0,0
        intersection = new Vector3(0, 0, 0);

        Vector3 line = p2 - p1;
        Vector3 dist = p1 - plane.location;

        float parallel = Vector3.Dot(plane.normal, line);
        float coplanar = -Vector3.Dot(plane.normal, dist);

        // Are they parallel?
        if (Mathf.Abs(parallel) < coplanar_margin_err)
        {
            // Does the segment lay in the plane?
            if (coplanar == 0)
                return 2;
            // Parallel and does not lay in plane, no intersection
            else
                return 0;
        }

        // Non-parallel, check for intersection
        float intersection_interp = coplanar / parallel;
        if (intersection_interp < 0 || intersection_interp > 1)
            return 0;

        // Intersection calculation
        intersection = p1 + (intersection_interp * line);

        return 1;
    }
}
