using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Triangle
{
	private ActiveNode<bVertex> n1, n2, n3;
	public Vector3 p1 { get { return n1.data.vertex; } }
	public Vector3 p2 { get { return n2.data.vertex; } }
	public Vector3 p3 { get { return n3.data.vertex; } }
	
	public Triangle(ActiveNode<bVertex> t1, ActiveNode<bVertex> t2, ActiveNode<bVertex> t3)
	{
		n1 = t1;
		n2 = t2;
		n3 = t3;
	}
	
	public Triangle(ActiveList<bVertex> vertices, int t1, int t2, int t3)
	{
		n1 = vertices.GetNodeAt(t1);
		n2 = vertices.GetNodeAt(t2);
		n3 = vertices.GetNodeAt(t3);
	}
	
	public Triangle GetCopy()
	{
		Triangle t = new Triangle(n1, n2, n3);
		return t;
	}
	
	public ActiveNode<bVertex> GetNode(int index)
	{
		switch (index)
		{
			case 0:
				return n1;
			case 1:
				return n2;
			case 2:
				return n3;
		}
		
		throw new UnityException("Bisec: Triangle vertex out of bounds.");
	}
	
	public void SetNodes(ActiveNode<bVertex> n1, ActiveNode<bVertex> n2, ActiveNode<bVertex> n3)
	{
		this.n1 = n1;
		this.n2 = n2;
		this.n3 = n3;
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
				n1.data.vertex = value;
				break;
			case 1:
				n2.data.vertex = value;
				break;
			case 2:
				n2.data.vertex = value;
				break;
		}
		
		throw new UnityException("Bisec: Triangle vertex out of bounds.");
	}
	
	public Vector2 GetUV(int index)
	{
		switch (index)
		{
			case 0:
				return n1.data.uv;
			case 1:
				return n2.data.uv;
			case 2:
				return n3.data.uv;
		}
		
		throw new UnityException("Bisec: Triangle uv out of bounds.");
	}
	
	public void SetUV(int index, Vector2 value)
	{
		switch (index)
		{
			case 0:
				n1.data.uv = value;
				break;
			case 1:
				n2.data.uv = value;
				break;
			case 2:
				n2.data.uv = value;
				break;
		}
		
		throw new UnityException("Bisec: Triangle uv out of bounds.");
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
