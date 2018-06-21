using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bisec_Mesh
{
    public struct b_unity_mesh
    {
        public List<Vector3> vertices;
        public List<int> triangles;
    }

    public struct b_triangle
    {
        int p1;
        int p2;
        int p3;
    }

    public Bisec_Mesh(Vector3[] vertices, int[] triangles)
    {

    }

    public b_unity_mesh GetMeshInfo()
    {
        return new b_unity_mesh();
    }

    

}
