using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bMesh
{
	public struct b_Plane
    {
        public Vector3 location;
        public Vector3 normal;
        public Plane uPlane;
    }
	
    private ActiveNode<Vector3>[][] lineLookupTable;
	private ActiveList<Vector3> vertices;
	private ActiveList<Triangle> triangles;
    private ActiveNode<Vector3> lineLookupBlank;
	
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
        
        lineLookupBlank = new ActiveNode<Vector3>();
        lineLookupBlank.isRootNode = true;
        ResetLineLookupTable();
	}
	
	
    private void PlaneTriangleBisection(b_Plane plane, Vector3 translationSide, Triangle triangle)
    {
        ActiveNode<Vector3>[] ints;
        
        if (PlaneTriangleIntersection(plane, triangle, out ints, true) == 0)
            return;
        
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

        bool translatedVertex = plane.uPlane.SameSide(triangle.GetVertex(singleInd), translationSide) ? true : false;

        ActiveNode<Vector3> single1 = LineIntersectLookup(triangle.GetVertexIndex(singleInd), triangle.GetVertexIndex((singleInd + 1) % 3));
        ActiveNode<Vector3> single1T = single1.nextActiveNode;
        ActiveNode<Vector3> single2 = LineIntersectLookup(triangle.GetVertexIndex(singleInd), triangle.GetVertexIndex((singleInd + 2) % 3));
        ActiveNode<Vector3> single2T = single2.nextActiveNode;
        
        if (!translatedVertex)
        {
            single1 = single1.nextActiveNode;
            single1T = single1T.prevActiveNode;
            single2 = single2.nextActiveNode;
            single2T = single2T.nextActiveNode;
        }
        
        // Remake the triangle (possibly with duplicate intersections [when expanding])
        // Make the new quad bridging the parts of the triangle
        triangles.Add(new Triangle(single1T, single1, single2));
        triangles.Add(new Triangle(single2T, single1T, single2));

        // Make the quad "base" of the bisected triangle
        triangles.Add(new Triangle(
                triangle.GetNode((singleInd + 1) % 3),
                triangle.GetNode((singleInd + 2) % 3),
                single1));
        triangles.Add(new Triangle(
                single2,
                single1,
                triangle.GetNode((singleInd + 2) % 3)));
        
        // Remake the "tip" of the triangle
        triangle.SetNodes(ints[singleInd], single1T, single2T);
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
    private ActiveNode<Vector3> LineIntersectLookup(int vertInd1, int vertInd2)
    {
        return lineLookupTable[vertInd1][vertInd2];
    }

    // Make sure to use LineIntersectLookup first
    private void SetLineIntersect(int vertInd1, int vertInd2, ActiveNode<Vector3> newVert)
    {
        lineLookupTable[vertInd1][vertInd2] = newVert;
        lineLookupTable[vertInd2][vertInd1] = newVert;
    }
    
    private void ResetLineLookupTable()
    {
        // Create the line lookup table
        lineLookupTable = new ActiveNode<Vector3>[vertices.ActiveCount][];
        for (int i = 0; i < lineLookupTable.Length; i++)
        {
            lineLookupTable[i] = new ActiveNode<Vector3>[vertices.ActiveCount];
            for (int j = 0; j < lineLookupTable[i].Length; j++)
            {
                lineLookupTable[i][j] = null;
            }
        }
    }
	
	private int PlaneTriangleIntersection(b_Plane plane, Triangle triangle, 
        out ActiveNode<Vector3>[] intersection,
        bool duplicateIntersectionPoints=false)
	{
		intersection = new ActiveNode<Vector3>[3];
        intersection[0] = null;intersection[1] = null;intersection[2] = null;
        
        Vector3 vec;        
        for (int i = 0; i < 3; i++)
        {
            int j = (i + 1) % 3;
            
            if (LineIntersectLookup(i, j) == lineLookupBlank)
            {
                continue;
            }
            else if (LineIntersectLookup(i, j) != null)
            {
                intersection[i] = LineIntersectLookup(i, j);
            }
            else if (PlaneSegmentIntersection(plane, triangle.GetVertex(i), triangle.GetVertex(j), out vec) == 1)
            {
                intersection[i] = vertices.Add(vec, true);
                SetLineIntersect(triangle.GetVertexIndex(i), triangle.GetVertexIndex(j), intersection[i]);
                
                if (duplicateIntersectionPoints)
                {
                    vertices.Add(new Vector3(vec.x, vec.y, vec.z), true);
                }
            }
            else
            {
                SetLineIntersect(triangle.GetVertexIndex(i), triangle.GetVertexIndex(j), lineLookupBlank);
            }
        }
		
        if (intersection[0] == null && intersection[1] == null)
        {
            return 0;
        }
        
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
