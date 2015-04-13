using UnityEngine;
using System.Collections;

public class PlayerManager : CustomMonoBehaviour {

	//Player script references. These make PlayerData a convenient hub of script data for each player
	[HideInInspector] public PlayerActions actions;
	[HideInInspector] public PlayerMotion motion;
	[HideInInspector] public PlayerGravity gravity;
	[HideInInspector] public PlayerWallRunning wallRunning;
	[HideInInspector] public new PlayerCamera camera;
	[HideInInspector] public InteractionManager interaction;
	[HideInInspector] public new AnimationManager animation;
	[HideInInspector] public PlayerInput input;
	[HideInInspector] public PlayerLife life;
	[HideInInspector] public PlayerRagdoll ragdoll;
	[HideInInspector] public LastToUpdate lastUpdate;

	[HideInInspector] public Transform t;
	[HideInInspector] public Transform body;
	[HideInInspector] public Transform character;


	//properties
	public bool alive {get { return life.lifeState.alive; } set {life.lifeState.alive = value;} }

	//Player state machine enums and instances
	[HideInInspector] public enum GravityType { Static, Rigidbody, Inactive }
	
	[HideInInspector] public GravityType gravityType;

	//Player state bools
	public PlayerStates state;

	public class PlayerStates 
	{
		private PlayerManager player;
		public bool orientPlayer;
		public bool touchingWall;
		public bool touchingGround;
		public RaycastHit groundSurfaceRayHit;
		public bool reachedGravSource;
		public bool alive {get {return this.player.life.lifeState.alive;} set {this.player.life.lifeState.alive = value;}}
		public bool inFirstPerson {get {return this.player.camera.inFirstPerson;} set {this.player.camera.inFirstPerson = value;}}

		public PlayerStates(PlayerManager pM) {this.player = pM;}
	}

	
	void Awake()
	{
		actions = GetComponentInChildren<PlayerActions>();
		motion = GetComponentInChildren<PlayerMotion>();
		gravity = GetComponentInChildren<PlayerGravity>();
		wallRunning = GetComponentInChildren<PlayerWallRunning>();
		camera = GetComponentInChildren<PlayerCamera>();
		interaction = GetComponentInChildren<InteractionManager>();
		input = GetComponentInChildren<PlayerInput>();
		lastUpdate = GetComponentInChildren<LastToUpdate>();
		life = GetComponentInChildren<PlayerLife>();
		state = new PlayerStates (this);

		
		t = transform;
		body = motion.transform;

	}
	

	void Start()
	{
	}

}
