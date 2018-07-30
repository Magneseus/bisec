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
    
    public int MaxHistoryCount = 5;
	
    private ActiveNode<Vector3>[][] lineLookupTable;
    private ActiveNode<Vector3>[][] lineLookupTable2;
	private ActiveList<Vector3> vertices;
	private ActiveList<Triangle> triangles;
    private ActiveNode<Vector3> lineLookupBlank;
    private Mesh targetMesh;
    private MeshCollider targetMeshCollider;
    
    private MeshHistory history;
	
	void Start()
	{
        targetMesh = GetComponent<MeshFilter>().mesh;
        targetMeshCollider = GetComponent<MeshCollider>();

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
    
    public void Undo()
    {
        history.PopChanges(triangles, vertices);
        RegenerateMesh(true);
        
        // Propagate through children
        foreach (bMesh mesh in this.GetComponentsInChildren<bMesh>())
        {
            if (mesh != this)
                mesh.Undo();
        }
    }
    
    public void Contract(b_Plane bisectPlane, b_Plane bisectPlane2, float timeToContract=-1.0f)
    {
        ResetLineLookupTable();
        
        // Transform the planes to local space for the mesh
        b_Plane bisectPlaneLocal;
        bisectPlaneLocal.location = this.transform.InverseTransformPoint(bisectPlane.location);
        bisectPlaneLocal.normal = this.transform.InverseTransformDirection(bisectPlane.normal);
        bisectPlaneLocal.uPlane = new Plane(bisectPlaneLocal.normal, bisectPlaneLocal.location);
        b_Plane bisectPlaneLocal2;
        bisectPlaneLocal2.location = this.transform.InverseTransformPoint(bisectPlane2.location);
        bisectPlaneLocal2.normal = this.transform.InverseTransformDirection(bisectPlane2.normal);
        bisectPlaneLocal2.uPlane = new Plane(bisectPlaneLocal2.normal, bisectPlaneLocal2.location);

        // Transform translation to local space
        Vector3 translation = this.transform.InverseTransformPoint(bisectPlane2.location) - bisectPlaneLocal.location;
        Vector3 translationSide = bisectPlaneLocal.location + translation;
        Vector3 translationSide2 = bisectPlaneLocal2.location - translation;
        
        int triangleLen = triangles.ActiveCount;
        int verticesLen = vertices.ActiveCount;
        
        HashSet<ActiveNode<Vector3>> verticesToBeTranslated = new HashSet<ActiveNode<Vector3>>();
        HashSet<ActiveNode<Vector3>> verticesToBeTranslated2 = new HashSet<ActiveNode<Vector3>>();
        List<ActiveNode<Triangle>> trianglesInBetween = new List<ActiveNode<Triangle>>();
        List<ActiveNode<Triangle>> trianglesNotTranslated = new List<ActiveNode<Triangle>>();
        
        ActiveNode<Triangle> it = triangles.GetRootNode().nextActiveNode;
        for (int i = 0; i < triangleLen; i++)
        {
            TriangleBisection(bisectPlaneLocal, translationSide, it, verticesToBeTranslated, trianglesNotTranslated);
            foreach (ActiveNode<Triangle> node in trianglesNotTranslated)
            {
                TriangleBisection(bisectPlaneLocal2, translationSide2, node, verticesToBeTranslated2, trianglesInBetween, true);
            }
            
            trianglesNotTranslated.Clear();
            it = it.nextActiveNode;
        }
        
        verticesToBeTranslated.Clear();
        
        if (timeToContract < 0.0f)
        {
            int x = trianglesInBetween.Count;
            for (int i = 0; i < x; i++)
            {
                ActiveNode<Triangle> node = trianglesInBetween[i];
                ChangeTriangle(node, null, true);
                //triangles.SetActivity(node, false);
            }
            foreach (ActiveNode<Vector3> node in verticesToBeTranslated2)
            {
                ChangeVertex(node, node.data - translation, node.data);
                //node.data -= translation;
            }
            
            RegenerateMesh();
            history.PushChanges();
        }
        else
        {
            RegenerateMesh();
            StartCoroutine(ContractTransition(timeToContract, verticesToBeTranslated2, trianglesInBetween, -translation));
        }
        
        // Propagate through children
        foreach (bMesh mesh in this.GetComponentsInChildren<bMesh>())
        {
            if (mesh != this)
                mesh.Contract(bisectPlane, bisectPlane2, timeToContract);
        }
    }
    
    IEnumerator ContractTransition(float numSeconds, HashSet<ActiveNode<Vector3>> verticesToBeTranslated, List<ActiveNode<Triangle>> trianglesToRemove, Vector3 translation)
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
            yield return new WaitForEndOfFrame();
        }
        
        int i = 0;
        foreach (ActiveNode<Vector3> node in verticesToBeTranslated)
        {
            ChangeVertex(node, originalVertices[i] + translation, originalVertices[i]);
            //node.data = originalVertices[i] + translation;
            i++;
        }
        int x = trianglesToRemove.Count;
        for (int j = 0; j < x; j++)
        {
            ActiveNode<Triangle> node = trianglesToRemove[j];
            ChangeTriangle(node, null, true);
            //triangles.SetActivity(node, false);
        }
        
        RegenerateMesh();
        history.PushChanges();
    }
	
    public void Expand(b_Plane bisectPlane, b_Plane bisectPlane2, float timeToExpand=-1.0f)
    {
        ResetLineLookupTable();
        
        // Transform the planes to local space for the mesh
        b_Plane bisectPlaneLocal;
        bisectPlaneLocal.location = this.transform.InverseTransformPoint(bisectPlane.location);
        bisectPlaneLocal.normal = this.transform.InverseTransformDirection(bisectPlane.normal);
        bisectPlaneLocal.uPlane = new Plane(bisectPlaneLocal.normal, bisectPlaneLocal.location);

        // Transform translation to local space
        Vector3 translation = this.transform.InverseTransformPoint(bisectPlane2.location) - bisectPlaneLocal.location;
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
        
        if (timeToExpand < 0.0f)
        {
            foreach (ActiveNode<Vector3> node in verticesToBeTranslated)
            {
                ChangeVertex(node, node.data + translation, node.data);
                //node.data += translation;
            }
            RegenerateMesh();
            history.PushChanges();
        }
        else
        {
            RegenerateMesh();
            StartCoroutine(ExpandTransition(timeToExpand, verticesToBeTranslated, translation));
            RegenerateMesh();
        }
        
        // Propagate through children
        foreach (bMesh mesh in this.GetComponentsInChildren<bMesh>())
        {
            if (mesh != this)
                mesh.Expand(bisectPlane, bisectPlane2, timeToExpand);
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
            yield return new WaitForEndOfFrame();
        }
        
        int i = 0;
        foreach (ActiveNode<Vector3> node in verticesToBeTranslated)
        {
            ChangeVertex(node, originalVertices[i] + translation, originalVertices[i]);
            //node.data = originalVertices[i] + translation;
            i++;
        }
        
        RegenerateMesh();
        history.PushChanges();
    }
	
    
    
	/*
	    Helper Functions
	*/
    
    private ActiveNode<Vector3> AddVertex(Vector3 vertex, bool active=true)
    {
        ActiveNode<Vector3> ret = vertices.Add(vertex, active);
        history.AddChange(ret, ret.data, false, true);
        
        return ret;
    }
    
    private void ChangeVertex(ActiveNode<Vector3> node, Vector3 newVal, Vector3 oldVal, bool activityToggle=false)
    {
        history.AddChange(node, oldVal, activityToggle);
        
        if (newVal != null)
        {
            node.data = newVal;
        }
        
        if (activityToggle)
        {
            vertices.SetActivity(node, !node.IsActive());
        }
    }
    
    private ActiveNode<Triangle> AddTriangle(Triangle triangle, bool active=true)
    {
        ActiveNode<Triangle> ret = triangles.Add(triangle, active);
        history.AddChange(ret, null, false, true);
        
        return ret;
    }
    
    private void ChangeTriangle(ActiveNode<Triangle> node, Triangle newVal, bool activityToggle=false)
    {
        history.AddChange(node, node.data, activityToggle);
        
        if (newVal != null)
        {
            node.data = newVal;
        }
        
        if (activityToggle)
        {
            triangles.SetActivity(node, !node.IsActive());
        }
    }
    
    private void TriangleBisection(b_Plane plane, Vector3 translationSide, ActiveNode<Triangle> triangleNode,
        HashSet<ActiveNode<Vector3>> verticesToBeTranslated,
        List<ActiveNode<Triangle>> trianglesInBetween=null,
        bool useSecondLookupTable=false)
    {
        Triangle triangle = triangleNode.data;
        ActiveNode<Vector3>[] ints;
        
        bool duplicateIntersectionPoints = trianglesInBetween == null;
        
        // Check if the plane intersects the triangle, output intersections to 'ints'
        // Vertices are automatically created in the 'vertices' list by this function
        if (PlaneTriangleIntersection(plane, triangle, out ints, duplicateIntersectionPoints, useSecondLookupTable) == 0)
        {
            // If no intersections...
            if (plane.uPlane.SameSide(triangle.GetVertex(0), translationSide))
            {
                if (trianglesInBetween != null)
                {
                    trianglesInBetween.Add(triangleNode);
                }
                else
                {
                    // This is a triangle that needs to be translated
                    for (int i = 0; i < 3; i++)
                    {
                        // Add it's vertices to the list to be translated
                        verticesToBeTranslated.Add(triangle.GetNode(i));
                    }
                }
            }
            else
            {
                if (useSecondLookupTable)
                {
                    // This is a triangle that needs to be translated
                    for (int i = 0; i < 3; i++)
                    {
                        // Add it's vertices to the list to be translated
                        verticesToBeTranslated.Add(triangle.GetNode(i));
                    }
                }
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
        ActiveNode<Vector3> single1 = ints[singleInd];
        ActiveNode<Vector3> single1T = single1.nextActiveNode;
        ActiveNode<Vector3> single2 = ints[(singleInd + 2) % 3];
        ActiveNode<Vector3> single2T = single2.nextActiveNode;
        
        // Add the single1T and single2T to the list
        if (!useSecondLookupTable)
        {
            verticesToBeTranslated.Add(single1T);
            verticesToBeTranslated.Add(single2T);
        }
        
        // If the single point is not on the translation side
        if (!translatedVertex)
        {
            // Switch the intersection points (switch the references, so the triangles are made properly)
            if (duplicateIntersectionPoints)
            {
                single1 = single1.nextActiveNode;
                single1T = single1T.prevActiveNode;
                single2 = single2.nextActiveNode;
                single2T = single2T.prevActiveNode;
            }
            
            
            if (!useSecondLookupTable)
            {
                // singleInd+1 and singleInd+2 need to be translated, add them to the list
                verticesToBeTranslated.Add(triangle.GetNode((singleInd + 1) % 3));
                verticesToBeTranslated.Add(triangle.GetNode((singleInd + 2) % 3));
            }
            else
            {
                // SingleInd needs to be translated
                verticesToBeTranslated.Add(triangle.GetNode(singleInd));
            }
        }
        else
        {
            if (!useSecondLookupTable)
            {
                // SingleInd needs to be translated
                verticesToBeTranslated.Add(triangle.GetNode(singleInd));
            }
            else
            {
                // singleInd+1 and singleInd+2 need to be translated, add them to the list
                verticesToBeTranslated.Add(triangle.GetNode((singleInd + 1) % 3));
                verticesToBeTranslated.Add(triangle.GetNode((singleInd + 2) % 3));
            }
        }
        
        if (!duplicateIntersectionPoints)
        {
            single1T = single1;
            single2T = single2;
            
            verticesToBeTranslated.Add(single1);
            verticesToBeTranslated.Add(single2);
        }
        
        /////// Remake the triangle (possibly with duplicate intersections [when expanding])
        
        // Make the new quad bridging the parts of the triangle
        if (duplicateIntersectionPoints)
        {
            AddTriangle(new Triangle(single1T, single1, single2));
            AddTriangle(new Triangle(single2T, single1T, single2));
        }

        // Make the quad "base" of the bisected triangle
        ActiveNode<Triangle> t1 = AddTriangle(new Triangle(
                triangle.GetNode((singleInd + 1) % 3),
                triangle.GetNode((singleInd + 2) % 3),
                single1), true);
        ActiveNode<Triangle> t2 = AddTriangle(new Triangle(
                single2,
                single1,
                triangle.GetNode((singleInd + 2) % 3)), true);
        
        // Remake the "tip" of the triangle
        ChangeTriangle(triangleNode, new Triangle(triangle.GetNode(singleInd), single1T, single2T));
        
        if (trianglesInBetween != null)
        {
            if (translatedVertex)
            {
                trianglesInBetween.Add(triangleNode);
            }
            else
            {
                trianglesInBetween.Add(t1);
                trianglesInBetween.Add(t2);
            }
        }
    }
    
    public void ResetMesh()
    {
        vertices = new ActiveList<Vector3>();
        triangles = new ActiveList<Triangle>();
        history = new MeshHistory(MaxHistoryCount);
        
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
    
    private void RegenerateMesh(bool reverse=false)
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
        
        if (!reverse)
        {
            targetMesh.SetVertices(newVertices);
            targetMesh.SetTriangles(newTriangles, 0);
        }
        else
        {
            targetMesh.SetTriangles(newTriangles, 0);
            targetMesh.SetVertices(newVertices);
        }
        
        targetMesh.RecalculateNormals();
        targetMesh.RecalculateTangents();
        targetMesh.RecalculateBounds();
        
        if (targetMeshCollider != null)
        {
            targetMeshCollider.sharedMesh = targetMesh;
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
    private ActiveNode<Vector3> LineIntersectLookup(int vertInd1, int vertInd2, bool use2=false)
    {
        if (vertInd1 >= lineLookupTable.Length || vertInd2 >= lineLookupTable.Length)
            return null;
        
        if (use2)
            return lineLookupTable2[vertInd1][vertInd2];
        
        return lineLookupTable[vertInd1][vertInd2];
    }

    // Make sure to use LineIntersectLookup first
    private void SetLineIntersect(int vertInd1, int vertInd2, ActiveNode<Vector3> newVert, bool use2=false)
    {
        if (vertInd1 >= lineLookupTable.Length || vertInd2 >= lineLookupTable.Length)
            return;
            
        if (use2)
        {
            lineLookupTable2[vertInd1][vertInd2] = newVert;
            lineLookupTable2[vertInd2][vertInd1] = newVert;
        }
        else
        {
            lineLookupTable[vertInd1][vertInd2] = newVert;
            lineLookupTable[vertInd2][vertInd1] = newVert;
        }
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
        
        lineLookupTable2 = new ActiveNode<Vector3>[vertices.ActiveCount][];
        for (int i = 0; i < lineLookupTable2.Length; i++)
        {
            lineLookupTable2[i] = new ActiveNode<Vector3>[vertices.ActiveCount];
            for (int j = 0; j < lineLookupTable2[i].Length; j++)
            {
                lineLookupTable2[i][j] = null;
            }
        }
    }
	
	private int PlaneTriangleIntersection(b_Plane plane, Triangle triangle, 
        out ActiveNode<Vector3>[] intersection,
        bool duplicateIntersectionPoints=false,
        bool useSecondLookupTable=false)
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
            
            if (LineIntersectLookup(vi, vj, !useSecondLookupTable) == lineLookupBlank)
            {
                nullCount++;
                continue;
            }
            else if (LineIntersectLookup(vi, vj, !useSecondLookupTable) != null)
            {
                intersection[i] = LineIntersectLookup(vi, vj, !useSecondLookupTable);
            }
            else if (PlaneSegmentIntersection(plane, triangle.GetVertex(i), triangle.GetVertex(j), out vec) == 1)
            {
                intersection[i] = AddVertex(new Vector3(vec.x, vec.y, vec.z), true);
                SetLineIntersect(vi, vj, intersection[i], !useSecondLookupTable);
                
                if (duplicateIntersectionPoints)
                {
                    AddVertex(new Vector3(vec.x, vec.y, vec.z), true);
                }
            }
            else
            {
                SetLineIntersect(vi, vj, lineLookupBlank, !useSecondLookupTable);
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
