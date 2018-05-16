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

    private int[][] lineLookupTable;

	// 
	void Start () {
        targetMesh = GetComponent<MeshFilter>().mesh;

        // No Mesh target
        if (targetMesh == null)
        {
            throw new MissingComponentException("No MeshFilter component to bisect.");
        }
        else
        {
            NewMeshBuild();
        }
    }

    private void NewMeshBuild()
    {
        lineLookupTable = new int[targetMesh.vertexCount][];
        for (int i = 0; i < lineLookupTable.Length; i++)
        {
            lineLookupTable[i] = new int[targetMesh.vertexCount];
            for (int j = 0; j < lineLookupTable[i].Length; j++)
            {
                lineLookupTable[i][j] = -1;
            }
        }
    }

    /*
     * When expanding, should achieve a O(n) time, but could possibly reduce
     * by treating like a graph, jumping through nodes and only taking ones
     * that have a chance of intersecting. O(log n)?
     * 
     * Will have a edge case where line is laying on the plane?
     */
    public void Expand(b_Plane bisectPlane, Vector3 translation)
    {
        if (targetMesh == null)
            return;

        // Unity plane for ease of use
        Plane uPlane = new Plane(bisectPlane.normal, bisectPlane.location);
        Vector3 translationSide = bisectPlane.location + translation;

        // Mesh Information
        Vector3[] verts = targetMesh.vertices;
        int[] triangles = targetMesh.triangles;

        // New Mesh Additions
        List<Vector3> newVerts = new List<Vector3>();
        List<int> newTriangles = new List<int>();

        // Intersections
        Vector3[] intersectons = new Vector3[3];

        // Points
        int[] pi = new int[3];
        Vector3[] pv = new Vector3[3];

        // Iterate through all triangles
        for (int i = 0; i < triangles.Length; i += 3)
        {
            // Get intersection of triangles and plane
            pi[0] = triangles[i];
            pi[1] = triangles[i + 1];
            pi[2] = triangles[i + 2];
            pv[0] = verts[pi[0]];
            pv[1] = verts[pi[1]];
            pv[2] = verts[pi[2]];

            // Iterate through each line
            for (int j = 0; j < 3; j++)
            {
                // Check if the line has been bisected previously
                int jpo = (j + 1) % 3;
                int ind = LineIntersectLookup(pi[j], pi[jpo]);

                // If done previously and no intersection, skip
                if (ind == -2)
                {
                    continue;
                }
                // If not done previously
                else if (ind == -1)
                {
                    // Look for an intersection
                    if (PlaneSegmentIntersection(
                        bisectPlane, 
                        pv[pi[j]], 
                        pv[pi[jpo]], 
                        out intersectons[j]) == 1)
                    {
                        // Set the lookup table for intersects
                        SetLineIntersect(pi[j], pi[jpo], verts.Length + newVerts.Count);

                        // Generate new vertices
                        newVerts.Add(new Vector3(
                            intersectons[j].x, 
                            intersectons[j].y, 
                            intersectons[j].z));
                        newVerts.Add(new Vector3(
                            intersectons[j].x + translation.x, 
                            intersectons[j].y + translation.y,
                            intersectons[j].z + translation.z));

                        // Move original vertex that is on the "expanding" side
                        // by specified translation
                        if (uPlane.SameSide(pv[j], translationSide))
                        {
                            pv[j] += translation;
                        }
                        else
                        {
                            pv[jpo] += translation;
                        }
                    }
                    // If no bisection, continue
                    else
                    {
                        SetLineIntersect(pi[j], pi[jpo], -2);
                        intersectons[j] = Vector3.zero;
                        continue;
                    }
                }
                
                // If done previously, with an intersection
                
            } // END OF LINE ITERATION
        }
    }

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
