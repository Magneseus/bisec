using System.Collections.Generic;
using UnityEngine;

public class MeshHistory
{
	private struct Triangle_Change
	{
		public ActiveNode<Triangle> node;
		public Triangle oldData;
		public bool activityToggle;
		public bool newNode;
	}
	
	private struct Vertex_Change
	{
		public ActiveNode<Vector3> node;
		public Vector3 oldData;
		public bool activityToggle;
		public bool newNode;
	}
	
	public int MaxCount;
	private List<List<Triangle_Change>> triangleStack;
	private List<List<Vertex_Change>> vertexStack;
	private List<Triangle_Change> runningTriangleChanges;
	private List<Vertex_Change> runningVertexChanges;
	
	public MeshHistory(int MaxCount=0)
	{
		this.MaxCount = MaxCount;
		
		triangleStack = new List<List<Triangle_Change>>();
		vertexStack = new List<List<Vertex_Change>>();
		runningTriangleChanges = new List<Triangle_Change>();
		runningVertexChanges = new List<Vertex_Change>();
	}
	
	public void PushChanges()
	{
		if (MaxCount <= 0)
			return;
		
		if (triangleStack.Count >= MaxCount)
		{
			triangleStack.RemoveAt(0);
			vertexStack.RemoveAt(0);
		}
		
		triangleStack.Add(runningTriangleChanges);
		vertexStack.Add(runningVertexChanges);
		
		runningTriangleChanges = new List<Triangle_Change>();
		runningVertexChanges = new List<Vertex_Change>();
	}
	
	public void PopChanges(ActiveList<Triangle> triangleList, ActiveList<Vector3> vertexList)
	{
		if (triangleStack.Count <= 0)
			return;
		
		runningTriangleChanges = triangleStack[triangleStack.Count-1];
		triangleStack.RemoveAt(triangleStack.Count-1);
		runningVertexChanges = vertexStack[vertexStack.Count-1];
		vertexStack.RemoveAt(vertexStack.Count-1);
		
		runningTriangleChanges.Reverse();
		runningVertexChanges.Reverse();
		
		foreach (Triangle_Change t in runningTriangleChanges)
		{
			if (t.oldData != null)
			{
				t.node.data = t.oldData;
			}
			
			if (t.activityToggle)
			{
				triangleList.SetActivity(t.node, t.node.IsActive());
			}
			
			if (t.newNode)
			{
				triangleList.Remove(t.node);
			}
		}
		
		foreach (Vertex_Change v in runningVertexChanges)
		{
			if (v.oldData != null)
			{
				v.node.data = v.oldData;
			}
			
			if (v.activityToggle)
			{
				vertexList.SetActivity(v.node, v.node.IsActive());
			}
			
			if (v.newNode)
			{
				vertexList.Remove(v.node);
			}
		}
		
		runningTriangleChanges = new List<Triangle_Change>();
		runningVertexChanges = new List<Vertex_Change>(); 
	}
	
	public void AddChange(ActiveNode<Triangle> node, Triangle oldData, bool activityToggle, bool newNode=false)
	{
		if (MaxCount <= 0)
			return;
		
		Triangle_Change t;
		t.node = node;
		if (oldData != null)
			t.oldData = oldData.GetCopy();
		else
			t.oldData = null;
		t.activityToggle = activityToggle;
		t.newNode = newNode;
		
		runningTriangleChanges.Add(t);
	}
	
	public void AddChange(ActiveNode<Vector3> node, Vector3 oldData, bool activityToggle, bool newNode=false)
	{
		if (MaxCount <= 0)
			return;
		
		Vertex_Change v;
		v.node = node;
		v.oldData = new Vector3(oldData.x, oldData.y, oldData.z);
		v.activityToggle = activityToggle;
		v.newNode = newNode;
		
		runningVertexChanges.Add(v);
	}
}
