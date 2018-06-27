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
	
    private int[][] lineLookupTable;
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
        
        ResetLineLookupTable();
	}
	
	
    private void PlaneTriangleBisection(b_Plane plane, Vector3 translationSide, Triangle triangle, bool duplicateIntersectionPoints=false,
        out Triangle[] triangles,
        out Triangle[] trianglesOnTranslationSide)
    {
        triangles = null;
        trianglesOnTranslationSide = null;
        object[] ints;
        
        // Check prev intersections?
        if (PlaneTriangleIntersection(plane, triangle, out ints) == 0)
            return;
        
        Vector3[] intersections = (Vector3[]) ints;
        
        // Find the point that is on it's own
        int singleInd = -1;
        for (singleInd = 0; singleInd < 3; singleInd++)
        {
            if (ints[singleInd] == null)
            {
                singleInd = (singleInd + 2) % 3;
                break;
            }
        }

        int translatedVertex = uPlane.SameSide(pv[singleInd], translationSide) ? 1 : 0;

        int single1 = LineIntersectLookup(pi[singleInd], pi[(singleInd + 1) % 3]);
        int single2 = LineIntersectLookup(pi[singleInd], pi[(singleInd + 2) % 3]);
        
        // Remake the triangle (possibly with duplicate intersections [when expanding])
        
    }
	
	/*
	    Helper Functions
	*/
    
     
    /*
     * Checks if a line has been previously bisected during an expansion operation.
     * Returns -1 if no bisection occurred.
     * 
     * Returns the positive integer representing the index of the newly created
     * vertex that was at the location of the bisection.
     * 
     * The returned index + 1 represents the index of the secondary vertex created
     * and translated.
     */
    private int LineIntersectLookup(int vertInd1, int vertInd2)
    {
        return lineLookupTable[vertInd1][vertInd2];
    }

    // Make sure to use LineIntersectLookup first
    private void SetLineIntersect(int vertInd1, int vertInd2, int newVertInd)
    {
        lineLookupTable[vertInd1][vertInd2] = newVertInd;
        lineLookupTable[vertInd2][vertInd1] = newVertInd;
    }
    
    private void ResetLineLookupTable()
    {
        // Create the line lookup table
        lineLookupTable = new int[vertices.ActiveCount][];
        for (int i = 0; i < lineLookupTable.Length; i++)
        {
            lineLookupTable[i] = new int[vertices.ActiveCount];
            for (int j = 0; j < lineLookupTable[i].Length; j++)
            {
                lineLookupTable[i][j] = -1;
            }
        }
    }
	
	private static int PlaneTriangleIntersection(b_Plane plane, Triangle triangle, 
        out object[] intersection)
	{
		intersection = new object[3];
        intersection[0] = null;intersection[1] = null;intersection[2] = null;
        
        Vector3 vec;
		if (PlaneSegmentIntersection(plane, triangle.p1, triangle.p2, out vec) == 1)
            intersection[0] = (object) vec;
        if (PlaneSegmentIntersection(plane, triangle.p2, triangle.p3, out vec) == 1)
            intersection[1] = (object) vec;
        if (PlaneSegmentIntersection(plane, triangle.p3, triangle.p1, out vec) == 1)
            intersection[2] = (object) vec;
		
        if (intersection[0] == null && intersection[1] == null)
            return 0;
        
		return 1;
	}
	
	// 0 -> no intersection
    // 1 -> single point intersection
    // 2 -> segment is laying on plane, inf. intersections
    private static int PlaneSegmentIntersection(b_Plane plane, Vector3 p1, Vector3 p2, 
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
