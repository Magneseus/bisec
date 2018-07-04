using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Triangle
{
	private ActiveNode<Vector3> n1, n2, n3;
	public Vector3 p1 { get { return n1.data; } }
	public Vector3 p2 { get { return n2.data; } }
	public Vector3 p3 { get { return n3.data; } }
	
	public Triangle(ActiveNode<Vector3> t1, ActiveNode<Vector3> t2, ActiveNode<Vector3> t3)
	{
		n1 = t1;
		n2 = t2;
		n3 = t3;
	}
	
	public Triangle(ActiveList<Vector3> vertices, int t1, int t2, int t3)
	{
		n1 = vertices.GetNodeAt(t1);
		n2 = vertices.GetNodeAt(t2);
		n3 = vertices.GetNodeAt(t3);
	}
	
	public Vector3 GetVertex(int index)
	{
		switch (index)
		{
			case 0:
				return p1;
			case 1:
				return p2;
			case 2:
				return p3;
		}
		
		throw new UnityException("Bisec: Triangle vertex out of bounds.");
	}
	
	public void SetVertex(int index, Vector3 value)
	{
		switch (index)
		{
			case 0:
				n1.data = value;
				break;
			case 1:
				n2.data = value;
				break;
			case 2:
				n2.data = value;
				break;
		}
		
		throw new UnityException("Bisec: Triangle vertex out of bounds.");
	}
	
	public int GetVertexIndex(int index)
	{
		switch (index)
		{
			case 0:
				return n1.activeIndex;
			case 1:
				return n2.activeIndex;
			case 2:
				return n3.activeIndex;
		}
		
		throw new UnityException("Bisec: Triangle vertex out of bounds.");
	}
}
