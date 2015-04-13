using UnityEngine;
using System;

public class PlayerCamera : CustomMonoBehaviour  
{
	private PlayerManager player;
	private PlayerInput input;
	
	[Header("Input")]
	[Range(0, 1500)]public int lookSensitivity;
	public float zRotationSensitivity;
	
	[Header("Misc.")]
	public float zAutoRotationSpeed;
	public float distanceFromGroundCameraOrientationThreshold;
	public LayerMask thirdPersonCameraLocationMask;
	public bool inFirstPerson = true;
	public GameObject playerWeaponMesh;
	
	[Header("Positioning")]
	public MyCurveControlledBob headbob = new MyCurveControlledBob();
	public Vector3 offset3rdPersonPivot;
	public Vector3 cameraHeadOffset;
	public Vector3 thirdPersonCameraOffset;
	public float camera3rdPersonDistance;
	public float cameraAngleDistanceFactor;
	[Range(0, 5)] public float thirdPersonBobbleFactor;
	public float deadPivotDistance;
	
	[NonSerialized] public Vector3 prevTransformForward;
	
	private float camUpWorldUpAngle;
	private float rotationX;
	private float angleBasedDistance;
	private float camGravAngle;
	private Vector3 currentPivot;
	
	private Transform rotationTransform { get { return player.alive ? player.t : transform; } }
	
	public SkinnedMeshRenderer characterRenderer;
	
	private Vector3 headBobble;
	
	//player.camera references this CameraController script. player.camera.t is a convenient reference to its Transform
	[HideInInspector] public Transform t;
	[HideInInspector] public  float rotationY;
	
	
	
	void Awake()
	{
		player = GetComponentInParent<PlayerManager> ();
		input = player.input;
		t = transform;
	}
	
	void Start ()
	{
		zAutoRotationSpeed /= 1000.0f;
		
		transform.rotation = player.transform.rotation;
	}
	
	void Update () 
	{	
		PreUpdateSetVariables ();
		
		TogglePerspectiveMode ();
		
		ApplyLookInput ();
		
		CameraUprightOrient ();
		
		PositionCameraInteractionDetector ();
	}
	
	void LateUpdate()
	{
		SetCameraPosition ();
	}
	
	
	void PreUpdateSetVariables()
	{
		currentPivot = player.alive 
			?	player.motion.transform.TransformPoint (offset3rdPersonPivot)
				:	player.ragdoll.deathPivot.position + -player.gravity.vector * deadPivotDistance;
		
		headBobble = player.ragdoll.head.position - player.body.position;
	}
	
	void ApplyLookInput ()
	{
		//rotates around local X axis using up/down look input
		rotationX = input.lookVector.y * lookSensitivity * Time.deltaTime;
		//rotates around local Y axis using left/right look input
		rotationY = input.lookVector.x * lookSensitivity * Time.deltaTime;
		
		if (inFirstPerson)
		{
			if (player.state.orientPlayer) 
			{
				float camGravityAngle = Vector3.Angle (transform.forward, player.gravity.gravityVector);
				//keeps player from looking up or down beyond local Y axis
				rotationX = Mathf.Clamp (rotationX, -179 + camGravityAngle, camGravityAngle - 1);
				
				//rotate player around local gravity axis (player's local Y axis)
				transform.parent.RotateAround (transform.position, -player.gravity.gravityVector, rotationY);
				transform.rotation *= Quaternion.Euler (rotationX, 0, 0);
			}
			else 
			{
				if (input.toggleGravity.Held)
					transform.rotation *= Quaternion.Euler (0, 0, -input.lookVector.x * zRotationSensitivity * Time.deltaTime);
				else
					//rotate player freely around camera's local Y axis
					transform.rotation *= Quaternion.Euler (0, rotationY, 0);
				
				transform.rotation *= Quaternion.Euler (rotationX, 0, 0);
			}
		}
		else
		{
			if (player.state.orientPlayer) 
			{
				float camGravityAngle = Vector3.Angle (transform.forward, player.gravity.gravityVector);
				//keeps player from looking up or down beyond local Y axis
				rotationX = Mathf.Clamp (rotationX, -179f + camGravityAngle, camGravityAngle - 1f);
			}
			//rotate camera and/or player around local gravity axis (depending on whether player is alive)
			rotationTransform.RotateAround (player.motion.transform.position, -player.gravity.gravityVector, rotationY);
			rotationTransform.RotateAround(player.motion.transform.position, Vector3.Cross (transform.forward, player.gravity.vector), rotationX);
			/*}
			else 
			{
				if (player.gravity.gravToggleTimer >= (input.buttonPressThreshold / 2) && InputX.Pressed(input.toggleGravity))
					transform.rotation *= Quaternion.Euler (0, 0, -input.lookVector.x * zRotationSensitivity * Time.deltaTime);
				else
					//rotate player freely around camera's local Y axis
					transform.rotation *= Quaternion.Euler (0, rotationY, 0);
				
				transform.rotation *= Quaternion.Euler (rotationX, 0, 0);
			}*/
		}
	}
	
	
	
	void CameraUprightOrient () 
	{
		if (player.state.orientPlayer)
		{
			//rotate the player more slowly when farther away, and faster as you get closer
			float rotationFactor = distanceFromGroundCameraOrientationThreshold / player.gravity.distanceFromGround;
			
			Quaternion uprightTargetRotation = Quaternion.LookRotation (transform.forward, -player.gravity.distanceFromCollider);
			transform.rotation = Quaternion.Lerp(transform.rotation, uprightTargetRotation, zAutoRotationSpeed * rotationFactor);
		}
	}
	
	void SetCameraPosition()
	{
		camGravAngle = Vector3.Angle (-transform.forward, player.gravity.gravityVector);
		angleBasedDistance = Mathf.Clamp (camGravAngle - 90, 0 , 90) / 90 * cameraAngleDistanceFactor;
		
		if (inFirstPerson) 
			transform.position = player.body.position + player.motion.transform.TransformVector(headbob.GetHeadBob(player.motion.velocity.magnitude) + cameraHeadOffset);
		else 
		{
			transform.position = currentPivot + (-transform.forward * (camera3rdPersonDistance + angleBasedDistance)) + ((headBobble + thirdPersonCameraOffset) * thirdPersonBobbleFactor);
			
			//prevent camera clipping
			RaycastHit camWallDetector;
			if (Physics.Raycast (player.motion.transform.TransformPoint(offset3rdPersonPivot), -transform.forward, out camWallDetector, camera3rdPersonDistance, thirdPersonCameraLocationMask))
			{
				transform.position = camWallDetector.point + (transform.forward / 2);
			}
		}
	}
	
	void TogglePerspectiveMode()
	{
		if (InputX.Down (input.switchViewMode))
		{
			inFirstPerson = !inFirstPerson;
			
			if (inFirstPerson)
				characterRenderer.enabled = false;
			else 
				characterRenderer.enabled = true;
		}
	}
	
	[Header("Interaction Detection")]
	public Transform interactionDetector;
	public float firstPersonDetectorOffset;
	public float thirdPersonDetectorOffset;
	
	void PositionCameraInteractionDetector()
	{
		interactionDetector.position = player.state.inFirstPerson 
			?	transform.position + (transform.forward * firstPersonDetectorOffset)
				:	currentPivot + (transform.forward * thirdPersonDetectorOffset);
	}
	
}