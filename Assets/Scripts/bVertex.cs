using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bVertex
{
	public Vector3 vertex;
	public Vector2 uv;
	
	private bVertex() {}
	
	public bVertex(Vector3 vert, Vector2 _uv)
	{
		vertex = vert;
		uv = _uv;
	}
	
	
	//public static bVertex operator +(bVertex v, bVertex v1)
	//{
			
	//}
	
	public bVertex GetCopy()
	{
		bVertex newVert = new bVertex();
		newVert.vertex = new Vector3(vertex.x, vertex.y, vertex.z);
		newVert.uv = new Vector2(uv.x, uv.y);
		
		return newVert;
	}
}

