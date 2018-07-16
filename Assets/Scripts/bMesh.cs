using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bMesh : MonoBehaviour
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
    private Mesh targetMesh;
	
	void Start()
	{
        targetMesh = GetComponent<MeshFilter>().mesh;

        // No Mesh target
        if (targetMesh == null)
        {
            throw new MissingComponentException("No MeshFilter component to bisect.");
        }
        else
        {
            ResetMesh();
        }
	}
    
    IEnumerator ExpandTransition(float numSeconds, HashSet<ActiveNode<Vector3>> verticesToBeTranslated, Vector3 translation)
    {
        float endTime = Time.realtimeSinceStartup + numSeconds;
        float currentTime = Time.realtimeSinceStartup;
        
        List<Vector3> originalVertices = new List<Vector3>();
        foreach (ActiveNode<Vector3> node in verticesToBeTranslated)
        {
            originalVertices.Add(new Vector3(node.data.x, node.data.y, node.data.z));
        }
        
        while (currentTime < endTime)
        {
            float t = 1.0f - ((endTime - currentTime) / numSeconds);
            int _i = 0;
            foreach (ActiveNode<Vector3> node in verticesToBeTranslated)
            {
                node.data = Vector3.Lerp(originalVertices[_i], originalVertices[_i] + translation, t);
                _i++;
            }
            
            RegenerateMesh();
            
            currentTime = Time.realtimeSinceStartup;
            yield return new WaitForFixedUpdate();
        }
        
        int i = 0;
        foreach (ActiveNode<Vector3> node in verticesToBeTranslated)
        {
            node.data = originalVertices[i] + translation;
            i++;
        }
    }
	
    public void Expand(b_Plane bisectPlane, b_Plane bisectPlane2, float timeToExpand=-1.0f)
    {
        Vector3 translation = bisectPlane2.location - bisectPlane.location;
        
        // Transform the planes to local space for the mesh
        b_Plane bisectPlaneLocal;
        bisectPlaneLocal.location = this.transform.InverseTransformPoint(bisectPlane.location);
        bisectPlaneLocal.normal = this.transform.InverseTransformDirection(bisectPlane.normal);
        bisectPlaneLocal.uPlane = new Plane(bisectPlaneLocal.normal, bisectPlaneLocal.location);

        // Transform translation to local space
        translation = this.transform.InverseTransformDirection(translation);
        Vector3 translationSide = bisectPlaneLocal.location + translation;
        
        int triangleLen = triangles.ActiveCount;
        int verticesLen = vertices.ActiveCount;
        
        HashSet<ActiveNode<Vector3>> verticesToBeTranslated = new HashSet<ActiveNode<Vector3>>();
        
        ActiveNode<Triangle> it = triangles.GetRootNode().nextActiveNode;
        for (int i = 0; i < triangleLen; i++)
        {
            TriangleBisection(bisectPlaneLocal, translationSide, it, verticesToBeTranslated);
            it = it.nextActiveNode;
        }
        
       
        
        /*
        ActiveNode<Vector3> it2 = vertices.GetRootNode().nextActiveNode;
        for (int i = 0; i < verticesLen; i++)
        {
            // If we're on the proper side of the plane
            if (bisectPlaneLocal.uPlane.SameSide(it2.data, translationSide))
            {
                // Translate the vertex
                it2.data += translation;
            }
            
            it2 = it2.nextActiveNode;
        }
        
        // Translate (in an alternating fashion) the remaining new triangles
        bool doTranslate = false;
        while (!it2.isRootNode)
        {
            if (doTranslate)
            {
                it2.data += translation;
            }
            
            doTranslate = !doTranslate;
            it2 = it2.nextActiveNode;
        }
        */
        
        if (timeToExpand < 0.0f)
        {
            foreach (ActiveNode<Vector3> node in verticesToBeTranslated)
            {
                node.data += translation;
            }
            RegenerateMesh();
        }
        else
        {
            RegenerateMesh();
            StartCoroutine(ExpandTransition(timeToExpand, verticesToBeTranslated, translation));
        }
    }
	
    private void TriangleBisection(b_Plane plane, Vector3 translationSide, ActiveNode<Triangle> triangleNode,
        HashSet<ActiveNode<Vector3>> verticesToBeTranslated,
        HashSet<ActiveNode<Triangle>> trianglesNotTranslated=null)
    {
        Triangle triangle = triangleNode.data;
        ActiveNode<Vector3>[] ints;
        
        // Check if the plane intersects the triangle, output intersections to 'ints'
        // Vertices are automatically created in the 'vertices' list by this function
        if (PlaneTriangleIntersection(plane, triangle, out ints, true) == 0)
        {
            // If no intersections...
            if (plane.uPlane.SameSide(triangle.GetVertex(0), translationSide))
            {
                // This is a triangle that needs to be translated
                for (int i = 0; i < 3; i++)
                {
                    // Add it's vertices to the list to be translated
                    verticesToBeTranslated.Add(triangle.GetNode(i));
                }
            }
            else
            {
                // This is a triangle that should not be translated
                trianglesNotTranslated.Add(triangleNode);
            }
            
            // Return, we don't care about this triangle beyond which side of the plane it's on
            return;
        }
        
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

        // Check if the single point is on the translation side
        bool translatedVertex = plane.uPlane.SameSide(triangle.GetVertex(singleInd), translationSide) ? true : false;
        
        // Get the generated intersection points
        ActiveNode<Vector3> single1 = LineIntersectLookup(triangle.GetVertexIndex(singleInd), triangle.GetVertexIndex((singleInd + 1) % 3));
        ActiveNode<Vector3> single1T = single1.nextActiveNode;
        ActiveNode<Vector3> single2 = LineIntersectLookup(triangle.GetVertexIndex(singleInd), triangle.GetVertexIndex((singleInd + 2) % 3));
        ActiveNode<Vector3> single2T = single2.nextActiveNode;
        
        // Add the single1T and single2T to the list
        verticesToBeTranslated.Add(single1T);
        verticesToBeTranslated.Add(single2T);
        
        // If the single point is not on the translation side
        if (!translatedVertex)
        {
            // Switch the intersection points (switch the references, so the triangles are made properly)
            single1 = single1.nextActiveNode;
            single1T = single1T.prevActiveNode;
            single2 = single2.nextActiveNode;
            single2T = single2T.prevActiveNode;
            
            // singleInd+1 and singleInd+2 need to be translated, add them to the list
            verticesToBeTranslated.Add(triangle.GetNode((singleInd + 1) % 3));
            verticesToBeTranslated.Add(triangle.GetNode((singleInd + 2) % 3));
        }
        else
        {
            // SingleInd needs to be translated
            verticesToBeTranslated.Add(triangle.GetNode(singleInd));
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
        triangle.SetNodes(triangle.GetNode(singleInd), single1T, single2T);
    }
    
    
    
    
    
	/*
	    Helper Functions
	*/
    
    public void ResetMesh()
    {
        vertices = new ActiveList<Vector3>();
        triangles = new ActiveList<Triangle>();
        
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
    
    private void RegenerateMesh()
    {
        List<Vector3> newVertices = new List<Vector3>();
        int[] newTriangles = new int[triangles.ActiveCount * 3];
        
        vertices.CopyActiveTo(newVertices);
        
        ActiveNode<Triangle> it = triangles.GetRootNode().nextActiveNode;
        int ind = 0;
        while (!it.isRootNode)
        {
            newTriangles[ind] = it.data.GetNode(0).activeIndex;
            newTriangles[ind + 1] = it.data.GetNode(1).activeIndex;
            newTriangles[ind + 2] = it.data.GetNode(2).activeIndex;
            
            it = it.nextActiveNode;
            ind += 3;
        }
        
        targetMesh.SetVertices(newVertices);
        targetMesh.SetTriangles(newTriangles, 0);
        
        targetMesh.RecalculateNormals();
        targetMesh.RecalculateTangents();
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
        int nullCount = 0;
        
        Vector3 vec;        
        for (int i = 0; i < 3; i++)
        {
            int j = (i + 1) % 3;
            
            int vi = triangle.GetVertexIndex(i);
            int vj = triangle.GetVertexIndex(j);
            
            if (LineIntersectLookup(vi, vj) == lineLookupBlank)
            {
                nullCount++;
                continue;
            }
            else if (LineIntersectLookup(vi, vj) != null)
            {
                intersection[i] = LineIntersectLookup(vi, vj);
            }
            else if (PlaneSegmentIntersection(plane, triangle.GetVertex(i), triangle.GetVertex(j), out vec) == 1)
            {
                intersection[i] = vertices.Add(new Vector3(vec.x, vec.y, vec.z), true);
                SetLineIntersect(vi, vj, intersection[i]);
                
                if (duplicateIntersectionPoints)
                {
                    vertices.Add(new Vector3(vec.x, vec.y, vec.z), true);
                }
            }
            else
            {
                SetLineIntersect(vi, vj, lineLookupBlank);
                nullCount++;
            }
        }
		
        if (nullCount > 1)
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
