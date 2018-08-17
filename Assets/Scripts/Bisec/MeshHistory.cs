using System.Collections.Generic;
using UnityEngine;

public class MeshHistory
{
	private struct bTriangle_Change
	{
		public ActiveNode<bTriangle> node;
		public bTriangle oldData;
		public bool activityToggle;
		public bool newNode;
	}
	
	private struct Vertex_Change
	{
		public ActiveNode<bVertex> node;
		public bVertex oldData;
		public bool activityToggle;
		public bool newNode;
	}
	
	public int MaxCount;
	private List<List<bTriangle_Change>> triangleStack;
	private List<List<Vertex_Change>> vertexStack;
	private List<bTriangle_Change> runningbTriangleChanges;
	private List<Vertex_Change> runningVertexChanges;
	
	public MeshHistory(int MaxCount=0)
	{
		this.MaxCount = MaxCount;
		
		triangleStack = new List<List<bTriangle_Change>>();
		vertexStack = new List<List<Vertex_Change>>();
		runningbTriangleChanges = new List<bTriangle_Change>();
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
		
		triangleStack.Add(runningbTriangleChanges);
		vertexStack.Add(runningVertexChanges);
		
		runningbTriangleChanges = new List<bTriangle_Change>();
		runningVertexChanges = new List<Vertex_Change>();
	}
	
	public void PopChanges(ActiveList<bTriangle> triangleList, ActiveList<bVertex> vertexList)
	{
		if (triangleStack.Count <= 0)
			return;
		
		runningbTriangleChanges = triangleStack[triangleStack.Count-1];
		triangleStack.RemoveAt(triangleStack.Count-1);
		runningVertexChanges = vertexStack[vertexStack.Count-1];
		vertexStack.RemoveAt(vertexStack.Count-1);
		
		runningbTriangleChanges.Reverse();
		runningVertexChanges.Reverse();
		
		foreach (bTriangle_Change t in runningbTriangleChanges)
		{
			if (t.oldData != null)
			{
				t.node.data = t.oldData;
			}
			
			if (t.activityToggle)
			{
				triangleList.SetActivity(t.node, !t.node.IsActive());
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
				vertexList.SetActivity(v.node, !v.node.IsActive());
			}
			
			if (v.newNode)
			{
				vertexList.Remove(v.node);
			}
		}
		
		runningbTriangleChanges = new List<bTriangle_Change>();
		runningVertexChanges = new List<Vertex_Change>(); 
	}
	
	public void AddChange(ActiveNode<bTriangle> node, bTriangle oldData, bool activityToggle, bool newNode=false)
	{
		if (MaxCount <= 0)
			return;
		
		bTriangle_Change t;
		t.node = node;
		if (oldData != null)
			t.oldData = oldData.GetCopy();
		else
			t.oldData = null;
		t.activityToggle = activityToggle;
		t.newNode = newNode;
		
		runningbTriangleChanges.Add(t);
	}
	
	public void AddChange(ActiveNode<bVertex> node, bVertex oldData, bool activityToggle, bool newNode=false)
	{
		if (MaxCount <= 0)
			return;
		
		Vertex_Change v;
		v.node = node;
		v.oldData = oldData.GetCopy();
		v.activityToggle = activityToggle;
		v.newNode = newNode;
		
		runningVertexChanges.Add(v);
	}
}
