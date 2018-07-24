using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
	public GameObject boltPrefab;
	
	private GameObject exBolt1;
	private GameObject exBolt2;
	private GameObject exGo;
	private GameObject coBolt1;
	private GameObject coBolt2;
	private GameObject coGo;
	
	private bMesh[] tmp;
	
	// Use this for initialization
	void Start ()
	{
		exBolt1 = null;
		exBolt2 = null;
		coBolt1 = null;
		coBolt2 = null;
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (Input.GetMouseButtonDown(0))
		{
			RaycastHit hit;
			if (Physics.Raycast(this.transform.position, transform.TransformDirection(Vector3.forward), out hit))
			{
				if (exBolt1 == null)
				{
					exBolt1 = Instantiate(boltPrefab, hit.point, Quaternion.LookRotation(hit.point - this.transform.position, Vector3.up));
					exGo = hit.collider.gameObject;
				}
				else if (exBolt1 != null && exBolt2 == null)
				{
					exBolt2 = Instantiate(boltPrefab, hit.point, Quaternion.LookRotation(hit.point - this.transform.position, Vector3.up));
					
					Vector3 translate = exBolt2.transform.position - exBolt1.transform.position;
					
					bMesh.b_Plane plane;
					plane.location = exBolt1.transform.position;
					plane.normal = -translate.normalized;
					plane.uPlane = new Plane(plane.normal, plane.location);
					
					bMesh.b_Plane plane2;
					plane2.location = exBolt2.transform.position;
					plane2.normal = -translate.normalized;
					plane2.uPlane = new Plane(plane2.normal, plane2.location);
					
					while (exGo.GetComponent<bMesh>() != null && exGo.transform.parent != null)
					{
						exGo = exGo.transform.parent.gameObject;
					}
					tmp = exGo.GetComponentsInChildren<bMesh>();
					foreach (bMesh mesh in tmp)
					{
						mesh.Expand(plane, plane2, 1.0f);
					}
					
					Destroy(exBolt1);
					exBolt1 = null;
					Destroy(exBolt2);
					exBolt2 = null;
					exGo = null;
				}
			}
		}
		
		if (Input.GetMouseButtonDown(1))
		{
			RaycastHit hit;
			if (Physics.Raycast(this.transform.position, transform.TransformDirection(Vector3.forward), out hit))
			{
				if (coBolt1 == null)
				{
					coBolt1 = Instantiate(boltPrefab, hit.point, Quaternion.LookRotation(hit.point - this.transform.position, Vector3.up));
					coGo = hit.collider.gameObject;
				}
				else if (coBolt1 != null && coBolt2 == null)
				{
					coBolt2 = Instantiate(boltPrefab, hit.point, Quaternion.LookRotation(hit.point - this.transform.position, Vector3.up));
					
					Vector3 translate = coBolt2.transform.position - coBolt1.transform.position;
					
					bMesh.b_Plane plane;
					plane.location = coBolt1.transform.position;
					plane.normal = -translate.normalized;
					plane.uPlane = new Plane(plane.normal, plane.location);
					
					bMesh.b_Plane plane2;
					plane2.location = coBolt2.transform.position;
					plane2.normal = -translate.normalized;
					plane2.uPlane = new Plane(plane2.normal, plane2.location);
					
					while (coGo.GetComponent<bMesh>() != null && coGo.transform.parent != null)
					{
						coGo = coGo.transform.parent.gameObject;
					}
					tmp = coGo.GetComponentsInChildren<bMesh>();
					foreach (bMesh mesh in tmp)
					{
						mesh.Contract(plane, plane2, 1.0f);
					}
					
					Destroy(coBolt1);
					coBolt1 = null;
					Destroy(coBolt2);
					coBolt2 = null;
					coGo = null;
				}
			}
		}
		
		if (Input.GetKeyDown(KeyCode.R))
		{
			foreach (bMesh mesh in tmp)
			{
				mesh.Undo();
			}
		}
		
		if (Input.GetKeyDown(KeyCode.Space))
		{
			if (exBolt1 != null)
				Destroy(exBolt1);
			if (exBolt2 != null)
				Destroy(exBolt2);
			if (coBolt1 != null)
				Destroy(coBolt1);
			if (coBolt2 != null)
				Destroy(coBolt2);
			
			exBolt1 = null;
			exBolt2 = null;
			exGo = null;
			coBolt1 = null;
			coBolt2 = null;
			coGo = null;
		}
	}
}
