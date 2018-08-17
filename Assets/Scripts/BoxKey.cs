using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxKey : MonoBehaviour
{
	Light boxLight;
	public GameObject door;
	public Color inactiveLightColor;
	public Color activeLightColor;
	
	// Use this for initialization
	void Start ()
	{
		boxLight = GetComponentInChildren<Light>();
	}
	
	void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.tag.Equals("Key"))
		{
			door.SetActive(false);
			boxLight.color = activeLightColor;
		}
	}
	
	void OnTriggerExit(Collider other)
	{
		if (other.gameObject.tag.Equals("Key"))
		{
			door.SetActive(true);
			boxLight.color = inactiveLightColor;
		}
	}
}
