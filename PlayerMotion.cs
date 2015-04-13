using UnityEngine;
using System;


public class PlayerMotion : CustomMonoBehaviour 
{
	private PlayerManager player;
	private PlayerInput input;
	public Transform t;

	public Transform feet;
	public Vector3 feetOffset;

	[Header("Motion")]
	public float motionSensitivity;
	public float runMultiplier;
	public float groundControl = 1.0f;
	public float airSpeedLimit = 5;
	public float jumpSensitivity = 5;
	public float kinematicMotionRatio = 5;
	public float forwardHandlingThreshold = .01f;
	public TimedInput timedJump;


	[Header("Zero G")]
	public float orientationThresholdLength;
	public float rotationSpeed;
	public float inertialMotionFocus;
	public float zeroGMotionSensitivity;
	

	[Header("Wall Walking")]
	public float autoOrientationMotionThreshold;
	public float forwardHandlingRotationFactor;
	public float lookBasedAdjustmentFactor;
	public float autoOrientDifferenceThreshold;
	public float cornerVelocityAdjustmentFactor;
	public float maxCornerVelocityAdjustment;
	
	[Header("Misc.")]
	public float wallRunRotationRate = 5;
	public LayerMask ignorePlayerMask;

	[HideInInspector] public bool bAutoOrientMotion;

	private RaycastHit edgeGroundedHit;
	private RaycastHit touchingGroundHit;
	private RaycastHit otherBuildingComponentHit;
	private Quaternion playerOrientationTarget;
	private Vector3 localVelocity;
	private Vector3 velocityChange;

	private bool blCanJump;
	private bool legalGroundSeparation; 
	private float maxGroundSpeed;
	private Quaternion rotationTarget;
	private RaycastHit nearestPointRayHit;
	private bool blSetAutoOrientMotion;
	private Vector3 currentForwardHandlingVector;
	private Vector3 storedForwardHandlingVector;
	private Vector3 axisVector;
	private Vector3 storedGravityVector;
	private Quaternion forwardHandlingQuat;
	private Vector3 worldHandlingAxis;
	private Vector3 orientedForwardVector;

	private bool blAltMotionDirection = false;
	private Vector3 referencedCameraDirection;

	private bool blPrevOnSurface;
	private Vector3 forwardRotationAxis;
	private Vector3 localTargetMotion;

	private float rotationFactor;

	private bool touchingGround;

	private bool jump {get { if (input.jumpTimer < input.delayThreshold) { input.jumpTimer += 10; return true;} return false;} }


	private Vector3 motionVector;

	[HideInInspector] public Vector3 prevPosition;
	[HideInInspector] public Vector3 prevVelocity;

	public Vector3 velocity { get {return GetComponent<Rigidbody>().velocity;} set {GetComponent<Rigidbody>().velocity = value;} }


	
	[HideInInspector] public Vector3 rightMotion;
	[HideInInspector] public Vector3 forwardMotion;
	[HideInInspector] public Vector3 previousPlayerVelocity;



	[HideInInspector] public bool bRunning = false;
	[HideInInspector] public Collision wallCollision;
	[HideInInspector] public Collision groundCollision;



	//ModifyVelocityAroundCorner modifyVelocityAroundCorner = new ModifyVelocityAroundCorner ();



	void MyDebug()
	{

	}


	void Awake ()
	{
		player = GetComponentInParent<PlayerManager> ();
		input = player.input;
		t = transform;
		timedJump = new TimedInput (input.jump);
	}
	// Use this for initialization
	void Start () 
	{

	}
	
	// Update is called once per frame
	void Update () 
	{
		MyDebug ();
	}
	
	//End of Update
	void FixedUpdate () 
	{
		SetPlayerOrientation ();
		

		SetAltMotionDirection ();

		//CheckForSlopedFloor ();
		
		GeneralMotion ();

		ZeroGInertiaDampening ();

		ModifyVelocityAroundCorner ();
		
		//runs automatically. state handling not set up
		StayGroundedOverEdge ();
	}



	void GeneralMotion()
	{
		motionVector = (Vector3.right * InputX.Axis (input.moveSideways)) + (Vector3.forward * InputX.Axis (input.moveForward));
		localTargetMotion = ((rightMotion * motionVector.x) + (forwardMotion * motionVector.z * (bRunning ? runMultiplier : 1))) * motionSensitivity;

		if (InputX.Down(input.run)) bRunning = true;
		if (motionVector.z < 0.8f) bRunning = false; 


		if (player.gravity.on)
		{
			if (!player.gravity.source.GetComponent<Rigidbody>())
				StaticGravSourceMotion();
			
			else 
				RigidbodyGravSourceMotion();
		}
		else 
		{
			ZeroGMotion();
		}
		

		feet.position = transform.TransformPoint (feetOffset);

	}


	void StaticGravSourceMotion()
	{
		//if checked gravityOn && !gravitySource.rigidbody

		if (player.state.touchingGround)
		{ 
			localVelocity = transform.InverseTransformDirection(rigidbody.velocity);
			velocityChange = transform.InverseTransformDirection(localTargetMotion) - localVelocity;
			velocityChange = Vector3.ClampMagnitude(velocityChange, groundControl);
			velocityChange.y = /*jump*/ timedJump.Tapped && player.state.touchingGround ? jumpSensitivity : 0;
			velocityChange = transform.TransformDirection (velocityChange);
			
			rigidbody.AddForce(velocityChange, ForceMode.VelocityChange);
		}
		else 
		{ 
			//playerOrientation Target is based on the current gravity vector and camera.t.forward. It's where player and camera always aim to rotate to
			//This operation converts the velocity vector to the local space of the pre-inverted quaternion
			localVelocity = Quaternion.Inverse(playerOrientationTarget) * rigidbody.velocity;
			
			localVelocity.Set	(Mathf.Clamp (localVelocity.x / airSpeedLimit, -1, 1), 
			                   0,
			                   Mathf.Clamp (localVelocity.z / airSpeedLimit, -1, 1));
			
			velocityChange = new Vector3 	(Mathf.Clamp(motionVector.x, -1 - localVelocity.x, 1 - localVelocity.x),
			                               0,
			                               Mathf.Clamp(motionVector.z, -1 - localVelocity.z, 1 - localVelocity.z));
			
			velocityChange = playerOrientationTarget * velocityChange * motionSensitivity;
			
			rigidbody.AddForce(velocityChange, ForceMode.Acceleration);
		}
	}

	void RigidbodyGravSourceMotion()
	{
		//if checked gravityOn && gravitySource.rigidbody
		if (player.state.touchingGround)
		{
			if (!legalGroundSeparation && player.state.reachedGravSource) rigidbody.isKinematic = true;
			localVelocity = transform.InverseTransformDirection(rigidbody.velocity);
			velocityChange = transform.InverseTransformDirection(localTargetMotion) - localVelocity;
			velocityChange = Vector3.ClampMagnitude(velocityChange, groundControl);
			//velocityChange.y = blJump && state.touchingGround ? jumpSensitivity : 0;
			velocityChange = transform.TransformDirection (velocityChange);
			transform.position += velocityChange / kinematicMotionRatio;
			
			if (jump)
			{
				rigidbody.isKinematic = false;
				rigidbody.AddForce((-player.gravity.vector.normalized * jumpSensitivity));// + player.gravity.source.rigidbody.velocity, ForceMode.VelocityChange);
			}
			
		}
	}

	void ZeroGMotion()
	{
		Vector3 targetMotion = ((player.camera.t.right * motionVector.x) + ((input.toggleGravity.Held ? player.camera.t.up : player.camera.t.forward) * motionVector.z)) * zeroGMotionSensitivity;

		rigidbody.ReduceOppositionalVelocity (targetMotion.normalized, targetMotion.magnitude * inertialMotionFocus * Time.deltaTime / 10);

		rigidbody.AddForce(targetMotion, ForceMode.Acceleration);
	}
		
	void SetPlayerOrientation()
	{	
	if (player.state.orientCamera)
	{
		CalculateGravityRotationFactor();

			if (player.state.touchingGround)
				playerOrientationTarget = Quaternion.LookRotation (player.gravity.distanceFromCollider, forwardMotion) * Quaternion.Euler (-90, 0, 0);
			else
				playerOrientationTarget = Quaternion.LookRotation (player.gravity.distanceFromCollider, player.camera.t.forward) * Quaternion.Euler (-90, 0, 0);

			transform.rotation = Quaternion.Lerp (transform.rotation, playerOrientationTarget, rotationSpeed * rotationFactor * Time.deltaTime);
		}
		else transform.rotation = Quaternion.Lerp (transform.rotation, player.camera.t.rotation, rotationSpeed * Time.deltaTime);
	}

	void CalculateGravityRotationFactor()
	{
		rotationFactor = orientationThresholdLength / player.gravity.distanceFromGround;
	}

	void OnCollisionEnter(Collision collision)
	{
		if (player.gravity.on && Vector3.Angle (collision.contacts[0].normal, -player.gravity.vector) < 45)
		{
			groundCollision = collision;
			player.state.touchingGround = true;
			player.state.touchingWall = false;
			legalGroundSeparation = false;
		}
		if (collision.collider == player.gravity.source)
		{
			player.gravity.bPointGrav = false;
			player.state.reachedGravSource = true;
			player.state.touchingGround = true;
		}



			/*else 	
		{
			bTouchingWall = true;
			wallCollision = collision;
		}*/
	}

	void OnCollisionExit(Collision collision)
	{
		if (collision.collider == player.gravity.source)
			player.state.touchingGround = false;


	}

	void OnCollisionStay(Collision collision)
	{
		/*if (collision.rigidbody) 
		{
			collision.rigidbody.velocity = Vector3.zero;
			collision.rigidbody.angularVelocity = Vector3.zero;
		}
		if (wallCollision.collider == player.gravity.source)
		{
			bTouchingWall = false;
			bReachedGravSource = true;
			bJumped = false;
			bGravOn = true;
			bRunning = true;
			state.touchingGround = true;
			state.orientCamerantPlayer = true;
			bTouchingWall = false;
		}*/
	}

	void WallRun()
	{
		if (wallCollision.collider.transform.parent.name != "Non Static Objects")
		{
			if (jump) 
			{
				rigidbody.AddForce((wallCollision.contacts[0].normal - player.gravity.vector) * jumpSensitivity * 50);
			}
			player.camera.t.rotation = Quaternion.Slerp (player.camera.t.rotation, Quaternion.LookRotation(player.camera.t.forward, wallCollision.contacts[0].normal - (3 * player.gravity.vector)), wallRunRotationRate);
		}
	}


	void SetAltMotionDirection ()
	{

		if (player.state.orientCamera && player.state.reachedGravSource)
		{
			Vector3 localGravity = player.gravity.source.transform.InverseTransformDirection(-player.gravity.vector);
			bool blOnEdge = !((Mathf.Abs(localGravity.x) <= forwardHandlingThreshold && Mathf.Abs(localGravity.y) <= forwardHandlingThreshold) || (Mathf.Abs(localGravity.y) <= forwardHandlingThreshold && Mathf.Abs(localGravity.z) <= forwardHandlingThreshold) || (Mathf.Abs(localGravity.z) <= forwardHandlingThreshold && Mathf.Abs(localGravity.x) <= forwardHandlingThreshold));

			if (!blOnEdge)
			{
				if (bAutoOrientMotion)
				{
					bAutoOrientMotion &= AdjustAutoOrientation();

					blPrevOnSurface = true;

					//builds quaternion out of left/right look input
					forwardHandlingQuat *= Quaternion.Euler (0, player.camera.rotationY * forwardHandlingRotationFactor, 0);
					Vector3 forwardRotationAxis = Vector3.Cross (worldHandlingAxis, -player.gravity.vector);
					float forwardRotationAngle = Vector3.Angle(-player.gravity.vector, worldHandlingAxis);
					orientedForwardVector = (forwardHandlingQuat * Quaternion.AngleAxis(forwardRotationAngle, Quaternion.Inverse (forwardHandlingQuat) * forwardRotationAxis)) * Vector3.forward;
					
					forwardMotion = orientedForwardVector.normalized;
					
				}
				else //set forward to plane projection of camera.forward onto the gravity vector
				{
					//forwardMotion = (player.camera.t.forward - (Vector3.Dot (player.camera.t.forward, player.gravity.vector)) * player.gravity.vector).normalized;
					forwardMotion = Vector3.ProjectOnPlane(player.camera.t.forward, player.gravity.vector).normalized;
				}
				
			}
			else 
			{	//this needs one more bool for the case in which the forward vector is already auto oriented, and then the player walks around a corner. I think...
				if (blPrevOnSurface || !bAutoOrientMotion) 
				{
					SetAutoOrientMotion ();
				}

				forwardHandlingQuat *= Quaternion.Euler (0, player.camera.rotationY * forwardHandlingRotationFactor, 0);
					
				forwardRotationAxis = Vector3.Cross (worldHandlingAxis, -player.gravity.vector);
				float forwardRotationAngle = Vector3.Angle (worldHandlingAxis, -player.gravity.vector);
					
				orientedForwardVector = (forwardHandlingQuat * Quaternion.AngleAxis(forwardRotationAngle, Quaternion.Inverse (forwardHandlingQuat) * forwardRotationAxis)) * Vector3.forward;

				if (AdjustAutoOrientation())				    
					forwardMotion = orientedForwardVector.normalized;
				else 
				{
					bAutoOrientMotion = false;
					forwardMotion = (player.camera.t.forward - (Vector3.Dot (player.camera.t.forward, player.gravity.vector)) * player.gravity.vector).normalized;
				}
					
			}
			
		}
		
		rightMotion = Vector3.Cross (forwardMotion, player.gravity.vector).normalized;
	}

	void StayGroundedOverEdge()
	{
		if (player.state.reachedGravSource) velocity = rotatedVelocity;
	}

	public Vector3 rotatedVelocity 
	{
		get 
		{
			if (velocity != Vector3.zero)
			{
				Quaternion velocityQuat = Quaternion.LookRotation (velocity, -player.gravity.vector);
				return (velocityQuat * Quaternion.AngleAxis (-Vector3.Angle (player.gravity.vector, player.gravity.prevGravityVector), Quaternion.Inverse (velocityQuat) * Vector3.Cross (player.gravity.vector, player.gravity.prevGravityVector))) * Vector3.forward * velocity.magnitude;
			}
			return Vector3.zero;
		}
	}

	public void SetAutoOrientMotion ()
	{
		//All variables used by SetAltMotionDirection(), but this function may be called elsewhere to prepare SetAltMotionDirection()
		bAutoOrientMotion = true;
		storedForwardHandlingVector = (player.camera.prevTransformForward - (Vector3.Dot (player.camera.prevTransformForward, player.gravity.prevGravityVector)) * player.gravity.prevGravityVector).normalized;
		worldHandlingAxis = -player.gravity.prevGravityVector;
		forwardHandlingQuat = Quaternion.LookRotation(bAutoOrientMotion ? forwardMotion : storedForwardHandlingVector, worldHandlingAxis);
		referencedCameraDirection = player.camera.prevTransformForward;
	}
	
	bool AdjustAutoOrientation()
	{
		if (motionVector.sqrMagnitude < autoOrientationMotionThreshold) return false;
		if (Vector3.Angle (storedForwardHandlingVector, orientedForwardVector) < 3) return false;
		//if (Vector3.Angle (orientedForwardVector, player.camera.t.forward - (Vector3.Dot (player.camera.t.forward, gravityVector) * gravityVector)) < autoOrientDifferenceThreshold) return false;
		Vector3 straightForwardVector = Vector3.Cross (forwardRotationAxis, -player.gravity.vector);
		bool blAutoOrientedFacingEdge = (Vector3.Angle (orientedForwardVector, straightForwardVector) < 90);
		straightForwardVector *= blAutoOrientedFacingEdge ? 1 : -1;
		if (Vector3.Angle (player.camera.t.forward - (Vector3.Dot (player.camera.t.forward, player.gravity.vector) * player.gravity.vector), straightForwardVector) < Vector3.Angle(orientedForwardVector, straightForwardVector)) return false;

		float lookBasedAdjustmentQuantity = Vector3.Angle (referencedCameraDirection, player.camera.t.forward);
		Vector3 localStoredForwardVector = Quaternion.Inverse (transform.rotation) * storedForwardHandlingVector;
		forwardHandlingQuat *= Quaternion.Euler (0, localStoredForwardVector.x / Mathf.Abs (localStoredForwardVector.x) * lookBasedAdjustmentQuantity * lookBasedAdjustmentFactor / 100, 0);

		return true;
	}

	public bool blDampenInertia = true;
	public float dampenInertiaStrength = 1;

	void ZeroGInertiaDampening()
	{
		if (InputX.Down(input.toggleInertiaDampener))
		{
			blDampenInertia = !blDampenInertia;
		}
		
		if (!player.gravity.on && blDampenInertia && motionVector == Vector3.zero)
		{
			rigidbody.velocity /= (Mathf.Abs (dampenInertiaStrength) / 1000) + 1;
		}
	}

	Vector3 verticalVelocity;
	bool movingUp;
	float verticalSpeed;
	float adjustmentMagnitude;
	Vector3 horizontalVelocity;
	
	void ModifyVelocityAroundCorner()
	{
		//project velocity onto gravity
			//verticalVelocity = Vector3.Project (velocity, player.gravity.prevGravityVector);

		//store bool determining whether or not moving upward or downward
			//movingUp = Vector3.Angle (verticalVelocity, player.gravity.prevGravityVector) > 90;

		//store vertical velocity magnitude
			//verticalSpeed = verticalVelocity.magnitude;

		//multiply vertical velocity by angle of gravity vectors
			//adjustmentMagnitude = verticalSpeed * player.gravity.rotationAngle * cornerVelocityAdjustmentFactor * Time.deltaTime;
			//adjustmentMagnitude = Mathf.Clamp (Vector3.Project (velocity, player.gravity.prevGravityVector).magnitude * player.gravity.rotationAngle * cornerVelocityAdjustmentFactor * Time.deltaTime, 0, maxCornerVelocityAdjustment);
			//adjustmentMagnitude = Mathf.Clamp (adjustmentMagnitude, 0, maxCornerVelocityAdjustment);

		//add result (multiplied by intensity factor and clamped to max) to new upward velocity if projection was upward, or to forward if downward
		//if (movingUp)
		{
			velocity += -player.gravity.vector * Mathf.Clamp ((Vector3.Project (velocity, player.gravity.prevGravityVector).magnitude * player.gravity.rotationAngle * cornerVelocityAdjustmentFactor * Time.deltaTime), 0, maxCornerVelocityAdjustment);
		}
		/*else
		{
			horizontalVelocity = Vector3.ProjectOnPlane(velocity, player.gravity.prevGravityVector);
			velocity += horizontalVelocity.normalized * adjustmentMagnitude;
		}*/

	}

	
}


