using UnityEngine;
using System.Collections;

public class PlayerRagdoll : MonoBehaviour 
{	
	private PlayerManager player;
	private PlayerInput input;

	[Header("Reference Transforms")]
	public Transform deathPivot;
	public Transform head;

	[HideInInspector] 
	public Rigidbody[] rigidbodies;

	[Header("Gravity")]
	public bool gravityOn;
	[Range(0, 5)]
	public float additionalGravForce;
	[HideInInspector]
	public Vector3 gravityVector;
	public Collider gravitySource;
	
	[Header("Death Velocity"), Range(0, 10)] 
	public int framesBeforeRelease;
	[HideInInspector]
	public Vector3 deathVelocity;
	[HideInInspector]
	public int lastDeathVelocityFrame;
	public bool transferVelocity;
	

	void Update () 
	{
		TransferPlayerMotion ();
		RagdollGravity ();
	}

	public void InitializeFields ()
	{
		player = GetComponentInParent<PlayerManager> ();
		input = player.input;
		rigidbodies = GetComponentsInChildren<Rigidbody> ();
	}	
	
	void RagdollGravity()
	{
		if (gravityOn) 
		{
			gravityVector = (gravitySource.NearestPoint(deathPivot.position) - deathPivot.position).normalized;

			for (int i = 0; i < rigidbodies.Length ; i++)
			{
				rigidbodies[i].ApplyGravity(gravityVector * additionalGravForce);
			}
		}
	}

	void TransferPlayerMotion()
	{
		if (Time.frameCount <= lastDeathVelocityFrame)
		{
			for (int i = 0; i < rigidbodies.Length ; i++)
			{
				rigidbodies[i].velocity = deathVelocity;
			}
		}

	}

}