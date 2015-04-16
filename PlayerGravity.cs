using UnityEngine;
using System.Collections;

public class PlayerGravity : CustomMonoBehaviour 
{
	private PlayerManager player;
	private PlayerInput input;
	
	[Header("Optional Non Default Assignment")]
		[Tooltip("Player specific. A Universal can be set in the game object with the Global script component (Generally applied to an object at the highest tier of the Scene Hierarchy).")] 
	public Collider startingGravitySource;

	[Header("General")]
	public bool gravityOn;
	public float gravMultiplier = 1;
	public float gravRaycastLength;
	public float gravPointNearnessThreshold;

	//reduces velocity orthogonal to gravity
	public bool gravityVelocityFocus;
	public float orthogonalVelocityReduction;

	[Header("Select Gravity")]
	private bool didSelectObject;
	private ObjectGravity selectedGravObject;
	private RaycastHit objectGravRayHit;

	[Header("Lengths")]
	[HideInInspector] public Vector3 distanceFromCollider;

	[HideInInspector] public float distanceFromGround;
	private Vector3 distanceFromSelectedPoint;
	[HideInInspector] public Vector3 distanceToGroundSurface;

	[Header("Gravity Info")]
	[HideInInspector] public Collider gravitySource;
	[HideInInspector] public Vector3 gravityVector;
	[HideInInspector] public RaycastHit gravRayHit;
	[HideInInspector] public bool bPointGrav;

	[Header("Rigidbody Gravity Source")]
	private Quaternion gravSourcePrevRotation;
	private Vector3 gravSourcePrevPosition;

	//previous values for reference
	[HideInInspector] public Vector3 prevGravityVector;
	[HideInInspector] public Collider prevGravitySource;
	private bool prevGravityOn;

	//based on current gravityVector and prevGravityVector 
	[HideInInspector] public Vector3 rotationAxis;
	[HideInInspector] public float rotationAngle;
	
	//reference shortcuts (player.gravity.vector  vs  player.gravity.gravityVector)
	public Vector3 vector { get { return gravityVector; } set { gravityVector = value; } } // read only
	public Collider source { get { return gravitySource; } set { gravitySource = value; } }
	public bool on { get { return gravityOn; } set { gravityOn = value; } }




	void Awake()
	{
		player = GetComponentInParent<PlayerManager> ();
		input = player.input;

		if (!startingGravitySource) startingGravitySource = GetComponentInParent<Global>().startingGravitySourceCollider;
		gravitySource = startingGravitySource;
	}

	void Start()
	{
	}

	void FixedUpdate()
	{
		HandleMultiColliderGravSource ();
		
		MovingGravitySource ();

		ApplyGravity ();

		ToggleGravity();

		SelectGravity ();

		StoreVariables ();
	}

	void LateUpdate ()
	{
	
	}

	void ApplyGravity()
	{
		if (gravityOn)
		{
			distanceFromCollider = gravitySource.NearestPoint(transform.position) - transform.position;

			if (gravRayHit.collider) distanceFromSelectedPoint = gravRayHit.point - transform.position;

			Physics.Raycast (new Ray(transform.position, distanceFromCollider), out player.state.groundSurfaceRayHit, Mathf.Infinity, player.motion.ignorePlayerMask);
			distanceToGroundSurface = player.state.groundSurfaceRayHit.point - transform.position;

			if (distanceToGroundSurface.sqrMagnitude < player.motion.startOrientationDistance.Squared())
			{

				player.state.orientCamera = true;
				//Make sure this is correct
				distanceFromGround = player.state.groundSurfaceRayHit.distance;
				
				if (bPointGrav && gravitySource.Raycast (new Ray(transform.position, distanceFromSelectedPoint), out player.state.groundSurfaceRayHit, gravPointNearnessThreshold))
				{
					bPointGrav = false;
				}
			}
			else if (!player.state.reachedGravSource && gravitySource != startingGravitySource) player.state.orientCamera = false;
			
			

			gravityVector = bPointGrav ? distanceFromSelectedPoint : distanceFromCollider;
			gravityVector.Normalize();
			
			rigidbody.AddForce (gravityVector * Global.gravIntensity * (player.state.touchingWall ? 0.75f : 1), ForceMode.Acceleration);
			
			if (gravityVelocityFocus && !player.state.reachedGravSource) 
				rigidbody.ReduceVelocityOrthogonalTo (gravityVector, orthogonalVelocityReduction);
		}
	}

	void StoreVariables()
	{
		rotationAngle = Vector3.Angle (prevGravityVector, vector);
		rotationAxis = Vector3.Cross (prevGravityVector, vector);
	}
	

	public void ToggleGravity()
	{
		if (input.toggleGravity.Tapped)
		{
			if (!gravityOn) gravitySource = startingGravitySource;
			else player.state.orientCamera = false;
			gravityOn = !gravityOn;
			player.motion.bAutoOrientMotion = false;
			player.state.reachedGravSource = false;
		}
	}



	public void SelectGravity()
	{ 
		if (InputX.Down(input.selectGravity))
			didSelectObject = Physics.Raycast (player.camera.t.position, player.camera.t.forward, out gravRayHit, Mathf.Infinity, player.motion.ignorePlayerMask);

		if (didSelectObject)
		{
			//if you selected new grav source for player
			if (input.selectGravity.Tapped)
			{ 
				//fresh start for player
				rigidbody.isKinematic = false;
				player.state.reachedGravSource = false;
				player.motion.bAutoOrientMotion = false;

				//store new gravity info
				gravitySource = gravRayHit.collider;
				gravityOn = true;

				//only true if original grav source not selected and is not a rigidbody object
				bPointGrav = (gravRayHit.collider != startingGravitySource) && !gravRayHit.collider.GetComponent<Rigidbody> ();

				//prepare rigidbody source handling... i think? *************************
				gravSourcePrevRotation = gravitySource.transform.rotation;

				//I want to phase this out... here to make sure nothing breaks **********************
				player.state.touchingGround = false;

				//consumed selection
				didSelectObject = false;
			}
			//if you selected new grav source for rigidbody object
			else if (gravRayHit.rigidbody && Physics.Raycast(player.camera.t.position, player.camera.t.forward, out objectGravRayHit, Mathf.Infinity, player.motion.ignorePlayerMask) && (gravRayHit.collider != objectGravRayHit.collider))
			{
				selectedGravObject = gravRayHit.collider.transform.GetComponent <ObjectGravity> ();
				selectedGravObject.gravitySource = objectGravRayHit.collider;
				selectedGravObject.bGravOn = true;

				//consumed selection
				didSelectObject = false;
			}
			//may be unnecessary. just guarantees predictable bool behavior.
			else if (InputX.Up(input.selectGravity)) didSelectObject = false;
		}
	}
	
	
	void HandleMultiColliderGravSource()
	{
		if (player.state.reachedGravSource && gravitySource.transform.parent.name != "Static Objects")
		{
			Vector3 nearestPointDistance = Vector3.up * 100000;
			foreach (Transform child in gravitySource.transform.parent)
			{ 
				Vector3 currentNearest = child.GetComponent<Collider> ().NearestPoint (transform.position);
				if ((currentNearest - transform.position).sqrMagnitude < (nearestPointDistance - transform.position).sqrMagnitude)
				{
					gravitySource = child.GetComponent<Collider>();
					nearestPointDistance = currentNearest;
				}
			}
			
			if (gravitySource != prevGravitySource) player.motion.SetAutoOrientMotion ();
		}
	}

	void MovingGravitySource ()
	{
		if (gravityOn && gravitySource.GetComponent<Rigidbody>())
		{
			if (player.state.reachedGravSource)
			{	float gravSourceRotationAngle;
				Vector3 gravSourceRotationAxis;
				transform.position += gravitySource.transform.position - gravSourcePrevPosition;
				(gravitySource.transform.rotation * Quaternion.Inverse(gravSourcePrevRotation)).ToAngleAxis(out gravSourceRotationAngle, out gravSourceRotationAxis);
				transform.parent.RotateAround(gravitySource.transform.position, gravSourceRotationAxis, gravSourceRotationAngle);
				
			}
			else
			{
				
			}
			
			//once used, prepare for next cycle
			gravSourcePrevPosition = gravitySource.transform.position;
			gravSourcePrevRotation = gravitySource.transform.rotation;
		}
	}
}
