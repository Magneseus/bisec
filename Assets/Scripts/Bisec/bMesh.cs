﻿using System.Collections;
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
    
    public bool AxisAlignedBisection = true;
    public int MaxHistoryCount = 10;
    public float timerMultiplier = 1.0f;
	
    private ActiveNode<bVertex>[][] lineLookupTable;
    private ActiveNode<bVertex>[][] lineLookupTable2;
	private ActiveList<bVertex> vertices;
	private ActiveList<bTriangle> triangles;
    private ActiveNode<bVertex> lineLookupBlank;
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
        
        timeToContract *= timerMultiplier;
        
        // Transform the planes to local space for the mesh
        b_Plane bisectPlaneLocal;
        bisectPlaneLocal.location = this.transform.InverseTransformPoint(bisectPlane.location);
        bisectPlaneLocal.normal = this.transform.InverseTransformDirection(bisectPlane.normal);
        b_Plane bisectPlaneLocal2;
        bisectPlaneLocal2.location = this.transform.InverseTransformPoint(bisectPlane2.location);
        bisectPlaneLocal2.normal = this.transform.InverseTransformDirection(bisectPlane2.normal);
        
        // Transform translation to local space
        Vector3 translation = this.transform.InverseTransformPoint(bisectPlane2.location) - bisectPlaneLocal.location;
        
        if (AxisAlignedBisection)
        {
            Vector3 dif = new Vector3(translation.x, translation.y, translation.z);
            dif.x = Mathf.Abs(dif.x);
            dif.y = Mathf.Abs(dif.y);
            dif.z = Mathf.Abs(dif.z);
            
            // X axis aligned
            if (dif.x > dif.y && dif.x > dif.z)
            {
                translation.y = 0;
                translation.z = 0;
                
                bisectPlaneLocal.normal.y = 0;
                bisectPlaneLocal.normal.z = 0;
                bisectPlaneLocal2.normal.y = 0;
                bisectPlaneLocal2.normal.z = 0;
                
                bisectPlaneLocal2.location = bisectPlaneLocal.location + translation;
            }
            // Y axis aligned
            else if (dif.y > dif.z)
            {
                translation.x = 0;
                translation.z = 0;
                
                bisectPlaneLocal.normal.x = 0;
                bisectPlaneLocal.normal.z = 0;
                bisectPlaneLocal2.normal.x = 0;
                bisectPlaneLocal2.normal.z = 0;
                
                bisectPlaneLocal2.location = bisectPlaneLocal.location + translation;
            }
            // Z axis aligned
            else
            {
                translation.x = 0;
                translation.y = 0;
                
                bisectPlaneLocal.normal.x = 0;
                bisectPlaneLocal.normal.y = 0;
                bisectPlaneLocal2.normal.x = 0;
                bisectPlaneLocal2.normal.y = 0;
                
                bisectPlaneLocal2.location = bisectPlaneLocal.location + translation;
            }
        }
        
        bisectPlaneLocal.uPlane = new Plane(bisectPlaneLocal.normal, bisectPlaneLocal.location);
        bisectPlaneLocal2.uPlane = new Plane(bisectPlaneLocal2.normal, bisectPlaneLocal2.location);
        
        Vector3 translationSide = bisectPlaneLocal.location + translation.normalized;
        Vector3 translationSide2 = bisectPlaneLocal2.location - translation.normalized;
        
        int triangleLen = triangles.ActiveCount;
        int verticesLen = vertices.ActiveCount;
        
        HashSet<ActiveNode<bVertex>> verticesToBeTranslated = new HashSet<ActiveNode<bVertex>>();
        HashSet<ActiveNode<bVertex>> verticesToBeTranslated2 = new HashSet<ActiveNode<bVertex>>();
        List<ActiveNode<bTriangle>> trianglesInBetween = new List<ActiveNode<bTriangle>>();
        List<ActiveNode<bTriangle>> trianglesNotTranslated = new List<ActiveNode<bTriangle>>();
        
        ActiveNode<bTriangle> it = triangles.GetRootNode().nextActiveNode;
        for (int i = 0; i < triangleLen; i++)
        {
            bTriangleBisection(bisectPlaneLocal, translationSide, it, verticesToBeTranslated, trianglesNotTranslated);
            foreach (ActiveNode<bTriangle> node in trianglesNotTranslated)
            {
                bTriangleBisection(bisectPlaneLocal2, translationSide2, node, verticesToBeTranslated2, trianglesInBetween, true);
            }
            
            trianglesNotTranslated.Clear();
            it = it.nextActiveNode;
        }
        
        verticesToBeTranslated.Clear();
        
        if (timeToContract <= 0.0f)
        {
            int x = trianglesInBetween.Count;
            for (int i = 0; i < x; i++)
            {
                ActiveNode<bTriangle> node = trianglesInBetween[i];
                ChangebTriangle(node, null, true);
                //triangles.SetActivity(node, false);
            }
            foreach (ActiveNode<bVertex> node in verticesToBeTranslated2)
            {
                bVertex newVert = node.data.GetCopy();
                newVert.vertex -= translation;
                ChangeVertex(node, newVert, node.data);
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
    
    IEnumerator ContractTransition(float numSeconds, HashSet<ActiveNode<bVertex>> verticesToBeTranslated, List<ActiveNode<bTriangle>> trianglesToRemove, Vector3 translation)
    {
        float endTime = Time.realtimeSinceStartup + numSeconds;
        float currentTime = Time.realtimeSinceStartup;
        
        List<Vector3> originalVertices = new List<Vector3>();
        foreach (ActiveNode<bVertex> node in verticesToBeTranslated)
        {
            originalVertices.Add(new Vector3(node.data.vertex.x, node.data.vertex.y, node.data.vertex.z));
        }
        
        while (currentTime < endTime)
        {
            float t = 1.0f - ((endTime - currentTime) / numSeconds);
            int _i = 0;
            foreach (ActiveNode<bVertex> node in verticesToBeTranslated)
            {
                node.data.vertex = Vector3.Lerp(originalVertices[_i], originalVertices[_i] + translation, t);
                _i++;
            }
            
            RegenerateMesh();
            
            currentTime = Time.realtimeSinceStartup;
            yield return new WaitForEndOfFrame();
        }
        
        int i = 0;
        foreach (ActiveNode<bVertex> node in verticesToBeTranslated)
        {
            bVertex newVert = node.data.GetCopy();
            newVert.vertex = originalVertices[i] + translation;
            node.data.vertex = originalVertices[i];
            
            ChangeVertex(node, newVert, node.data);
            //node.data = originalVertices[i] + translation;
            i++;
        }
        int x = trianglesToRemove.Count;
        for (int j = 0; j < x; j++)
        {
            ActiveNode<bTriangle> node = trianglesToRemove[j];
            ChangebTriangle(node, null, true);
            //triangles.SetActivity(node, false);
        }
        
        RegenerateMesh();
        history.PushChanges();
    }
	
    public void Expand(b_Plane bisectPlane, b_Plane bisectPlane2, float timeToExpand=-1.0f)
    {
        ResetLineLookupTable();
        
        timeToExpand *= timerMultiplier;
        
        // Transform the planes to local space for the mesh
        b_Plane bisectPlaneLocal;
        bisectPlaneLocal.location = this.transform.InverseTransformPoint(bisectPlane.location);
        bisectPlaneLocal.normal = this.transform.InverseTransformDirection(bisectPlane.normal);

        // Transform translation to local space
        Vector3 translation = this.transform.InverseTransformPoint(bisectPlane2.location) - bisectPlaneLocal.location;
        
        if (AxisAlignedBisection)
        {
            Vector3 dif = new Vector3(translation.x, translation.y, translation.z);
            dif.x = Mathf.Abs(dif.x);
            dif.y = Mathf.Abs(dif.y);
            dif.z = Mathf.Abs(dif.z);
            
            // X axis aligned
            if (dif.x > dif.y && dif.x > dif.z)
            {
                translation.y = 0;
                translation.z = 0;
                
                bisectPlaneLocal.normal.y = 0;
                bisectPlaneLocal.normal.z = 0;
            }
            // Y axis aligned
            else if (dif.y > dif.z)
            {
                translation.x = 0;
                translation.z = 0;
                
                bisectPlaneLocal.normal.x = 0;
                bisectPlaneLocal.normal.z = 0;
            }
            // Z axis aligned
            else
            {
                translation.x = 0;
                translation.y = 0;
                
                bisectPlaneLocal.normal.x = 0;
                bisectPlaneLocal.normal.y = 0;
            }
        }
        
        bisectPlaneLocal.uPlane = new Plane(bisectPlaneLocal.normal, bisectPlaneLocal.location);
        
        Vector3 translationSide = bisectPlaneLocal.location + translation;
        
        int triangleLen = triangles.ActiveCount;
        int verticesLen = vertices.ActiveCount;
        
        HashSet<ActiveNode<bVertex>> verticesToBeTranslated = new HashSet<ActiveNode<bVertex>>();
        
        ActiveNode<bTriangle> it = triangles.GetRootNode().nextActiveNode;
        for (int i = 0; i < triangleLen; i++)
        {
            bTriangleBisection(bisectPlaneLocal, translationSide, it, verticesToBeTranslated);
            it = it.nextActiveNode;
        }
        
        
        if (timeToExpand <= 0.0f)
        {
            foreach (ActiveNode<bVertex> node in verticesToBeTranslated)
            {
                bVertex newVert = node.data.GetCopy();
                newVert.vertex += translation;
                ChangeVertex(node, newVert, node.data);
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
    
    IEnumerator ExpandTransition(float numSeconds, HashSet<ActiveNode<bVertex>> verticesToBeTranslated, Vector3 translation)
    {
        float endTime = Time.realtimeSinceStartup + numSeconds;
        float currentTime = Time.realtimeSinceStartup;
        
        List<Vector3> originalVertices = new List<Vector3>();
        foreach (ActiveNode<bVertex> node in verticesToBeTranslated)
        {
            originalVertices.Add(new Vector3(node.data.vertex.x, node.data.vertex.y, node.data.vertex.z));
        }
        
        while (currentTime < endTime)
        {
            float t = 1.0f - ((endTime - currentTime) / numSeconds);
            int _i = 0;
            foreach (ActiveNode<bVertex> node in verticesToBeTranslated)
            {
                node.data.vertex = Vector3.Lerp(originalVertices[_i], originalVertices[_i] + translation, t);
                _i++;
            }
            
            RegenerateMesh();
            
            currentTime = Time.realtimeSinceStartup;
            yield return new WaitForEndOfFrame();
        }
        
        int i = 0;
        foreach (ActiveNode<bVertex> node in verticesToBeTranslated)
        {
            bVertex newVert = node.data.GetCopy();
            newVert.vertex = originalVertices[i] + translation;
            node.data.vertex = originalVertices[i];
            
            ChangeVertex(node, newVert, node.data);
            //node.data = originalVertices[i] + translation;
            i++;
        }
        
        RegenerateMesh();
        history.PushChanges();
    }
	
    
    
	/*
	    Helper Functions
	*/
    
    private ActiveNode<bVertex> AddVertex(bVertex vertex, bool active=true)
    {
        ActiveNode<bVertex> ret = vertices.Add(vertex, active);
        history.AddChange(ret, ret.data, false, true);
        
        return ret;
    }
    
    private void ChangeVertex(ActiveNode<bVertex> node, bVertex newVal, bVertex oldVal, bool activityToggle=false)
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
    
    private ActiveNode<bTriangle> AddbTriangle(bTriangle triangle, bool active=true)
    {
        ActiveNode<bTriangle> ret = triangles.Add(triangle, active);
        history.AddChange(ret, null, false, true);
        
        return ret;
    }
    
    private void ChangebTriangle(ActiveNode<bTriangle> node, bTriangle newVal, bool activityToggle=false)
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
    
    private void bTriangleBisection(b_Plane plane, Vector3 translationSide, ActiveNode<bTriangle> triangleNode,
        HashSet<ActiveNode<bVertex>> verticesToBeTranslated,
        List<ActiveNode<bTriangle>> trianglesInBetween=null,
        bool useSecondLookupTable=false)
    {
        bTriangle triangle = triangleNode.data;
        ActiveNode<bVertex>[] ints;
        
        bool duplicateIntersectionPoints = trianglesInBetween == null;
        
        // Check if the plane intersects the triangle, output intersections to 'ints'
        // Vertices are automatically created in the 'vertices' list by this function
        if (PlanebTriangleIntersection(plane, triangle, out ints, duplicateIntersectionPoints, useSecondLookupTable) == 0)
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
        ActiveNode<bVertex> single1 = ints[singleInd];
        ActiveNode<bVertex> single1T = single1.nextActiveNode;
        ActiveNode<bVertex> single2 = ints[(singleInd + 2) % 3];
        ActiveNode<bVertex> single2T = single2.nextActiveNode;
        
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
            AddbTriangle(new bTriangle(single1T, single1, single2));
            AddbTriangle(new bTriangle(single2T, single1T, single2));
        }

        // Make the quad "base" of the bisected triangle
        ActiveNode<bTriangle> t1 = AddbTriangle(new bTriangle(
                triangle.GetNode((singleInd + 1) % 3),
                triangle.GetNode((singleInd + 2) % 3),
                single1), true);
        ActiveNode<bTriangle> t2 = AddbTriangle(new bTriangle(
                single2,
                single1,
                triangle.GetNode((singleInd + 2) % 3)), true);
        
        // Remake the "tip" of the triangle
        ChangebTriangle(triangleNode, new bTriangle(triangle.GetNode(singleInd), single1T, single2T));
        
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
        vertices = new ActiveList<bVertex>();
        triangles = new ActiveList<bTriangle>();
        history = new MeshHistory(MaxHistoryCount);
        
        for (int i = 0; i < targetMesh.vertices.Length; i++)
		{
			vertices.Add(new bVertex(targetMesh.vertices[i], targetMesh.uv[i]));
		}
		
		for (int i = 0; i < targetMesh.triangles.Length; i += 3)
		{
			triangles.Add(new bTriangle(vertices,
					targetMesh.triangles[i],
					targetMesh.triangles[i+1],
					targetMesh.triangles[i+2]));
		}
        
        lineLookupBlank = new ActiveNode<bVertex>();
        lineLookupBlank.isRootNode = true;
        ResetLineLookupTable();
    }
    
    private void RegenerateMesh(bool reverse=false)
    {
        List<bVertex> newBVertices = new List<bVertex>();
        int[] newbTriangles = new int[triangles.ActiveCount * 3];
        
        vertices.CopyActiveTo(newBVertices);
        
        Vector3[] newVertices = new Vector3[newBVertices.Count];
        Vector2[] newUV = new Vector2[newBVertices.Count];
        
        int ind = 0;
        foreach (bVertex v in newBVertices)
        {
            newVertices[ind] = v.vertex;
            newUV[ind] = v.uv;
            
            ind++;
        }
        
        ActiveNode<bTriangle> it = triangles.GetRootNode().nextActiveNode;
        ind = 0;
        while (!it.isRootNode)
        {
            newbTriangles[ind] = it.data.GetNode(0).activeIndex;
            newbTriangles[ind + 1] = it.data.GetNode(1).activeIndex;
            newbTriangles[ind + 2] = it.data.GetNode(2).activeIndex;
            
            it = it.nextActiveNode;
            ind += 3;
        }
        
        /*
        if (!reverse)
        {
            targetMesh.SetVertices(newVertices);
            targetMesh.SetbTriangles(newbTriangles, 0);
        }
        else
        {
            targetMesh.SetbTriangles(newbTriangles, 0);
            targetMesh.SetVertices(newVertices);
        }
        */
        targetMesh.Clear();
        targetMesh.vertices = newVertices;
        targetMesh.uv = newUV;
        targetMesh.triangles = newbTriangles;
        
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
    private ActiveNode<bVertex> LineIntersectLookup(int vertInd1, int vertInd2, bool use2=false)
    {
        if (vertInd1 >= lineLookupTable.Length || vertInd2 >= lineLookupTable.Length)
            return null;
        
        if (use2)
            return lineLookupTable2[vertInd1][vertInd2];
        
        return lineLookupTable[vertInd1][vertInd2];
    }

    // Make sure to use LineIntersectLookup first
    private void SetLineIntersect(int vertInd1, int vertInd2, ActiveNode<bVertex> newVert, bool use2=false)
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
        lineLookupTable = new ActiveNode<bVertex>[vertices.ActiveCount][];
        for (int i = 0; i < lineLookupTable.Length; i++)
        {
            lineLookupTable[i] = new ActiveNode<bVertex>[vertices.ActiveCount];
            for (int j = 0; j < lineLookupTable[i].Length; j++)
            {
                lineLookupTable[i][j] = null;
            }
        }
        
        lineLookupTable2 = new ActiveNode<bVertex>[vertices.ActiveCount][];
        for (int i = 0; i < lineLookupTable2.Length; i++)
        {
            lineLookupTable2[i] = new ActiveNode<bVertex>[vertices.ActiveCount];
            for (int j = 0; j < lineLookupTable2[i].Length; j++)
            {
                lineLookupTable2[i][j] = null;
            }
        }
    }
	
	private int PlanebTriangleIntersection(b_Plane plane, bTriangle triangle, 
        out ActiveNode<bVertex>[] intersection,
        bool duplicateIntersectionPoints=false,
        bool useSecondLookupTable=false)
	{
		intersection = new ActiveNode<bVertex>[3];
        intersection[0] = null;intersection[1] = null;intersection[2] = null;
        int nullCount = 0;
        
        Vector3 vec;
        float interp;    
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
            else if (PlaneSegmentIntersection(plane, triangle.GetVertex(i), triangle.GetVertex(j), out vec, out interp) == 1)
            {
                bVertex newVert = new bVertex(
                    new Vector3(vec.x, vec.y, vec.z), 
                    Vector2.Lerp(triangle.GetUV(i), triangle.GetUV(j), interp)
                    );
                    
                intersection[i] = AddVertex(newVert, true);
                SetLineIntersect(vi, vj, intersection[i], !useSecondLookupTable);
                
                if (duplicateIntersectionPoints)
                {
                    AddVertex(new bVertex(
                            new Vector3(vec.x, vec.y, vec.z), 
                            Vector2.Lerp(triangle.GetUV(i), triangle.GetUV(j), interp)), 
                        true);
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
        out Vector3 intersection, out float interp)
    {
        const float coplanar_margin_err = 0.00001f;

        // Default intersection to 0,0,0
        intersection = new Vector3(0, 0, 0);
        interp = -1.0f;

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
        interp = coplanar / parallel;
        if (interp < 0 || interp > 1)
            return 0;

        // Intersection calculation
        intersection = p1 + (interp * line);

        return 1;
    }
}
