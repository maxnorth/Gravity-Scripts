using UnityEngine;
using System.Collections;

public class PlayerWallRunning : MonoBehaviour 
{
	private PlayerManager player;
	
	void Awake()
	{
		player = GetComponentInParent<PlayerManager> ();
	}

	void Update ()
	{
		}

	void OnCollisionEnter(Collision cal)
	{
		//print ("enter");
	}

	void OnCollisionStay(Collision col)
	{
		//print ("stay");
	}

	void OnCollisionExit(Collision cil)
	{
		//print ("exit");
	}
}
