using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bMesh
{
	public struct b_Plane
    {
        public Vector3 location;
        public Vector3 normal;
    }
	
	private ActiveList<Vector3> vertices;
	private ActiveList<Triangle> triangles;
	
	public bMesh(Mesh targetMesh)
	{
		foreach (Vector3 v in targetMesh.vertices)
		{
			vertices.Add(v);
		}
		
		for (int i = 0; i < targetMesh.triangles.Length; i += 3)
		{
			triangles.Add(new Triangle(vertices,
					targetMesh.triangles[i],
					targetMesh.triangles[i+1],
					targetMesh.triangles[i+2]));
		}
	}
	
	
	
	/*
		Helper Functions
	 */
	
	public static int PlaneTriangleIntersection(b_Plane plane, Triangle triangle, 
        out object intersection,
		out object intersection2,
		out object intersection3)
	{
		intersection = null;
		intersection2 = null;
		intersection3 = null;
        
        Vector3 vec;
		if (PlaneSegmentIntersection(plane, triangle.p1, triangle.p2, out vec) == 1)
            intersection = (object) vec;
        if (PlaneSegmentIntersection(plane, triangle.p2, triangle.p3, out vec) == 1)
            intersection2 = (object) vec;
        if (PlaneSegmentIntersection(plane, triangle.p3, triangle.p1, out vec) == 1)
            intersection3 = (object) vec;
		
		return 0;
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
