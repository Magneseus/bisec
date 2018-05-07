using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Line : MonoBehaviour {

    public Vector3 l1;
    public Vector3 l2;

    public Transform plane;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        Vector3 m = Input.mousePosition;
        m.z = 5.0f;

        if (Input.GetMouseButtonDown(0))
        {
            l1 = Camera.main.ScreenToWorldPoint(m);
        }
        else if (Input.GetMouseButtonDown(1))
        {
            l2 = Camera.main.ScreenToWorldPoint(m);
        }

	}

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(l1, l2);

        Bisec.b_Plane plane_;
        plane_.location = plane.position;
        plane_.normal = Vector3.up;
        plane_.normal = plane.transform.rotation * plane_.normal;

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(new Vector3(0, 0, 0), plane_.normal);

        Vector3 intersec;
        Bisec.PlaneSegmentIntersection(plane_, l1, l2, out intersec);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(intersec, 0.2f);
    }
}
