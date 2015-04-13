using UnityEngine;
using System.Collections;

public class PlayerLife : MonoBehaviour {

	private PlayerManager player;
	

	[Header("Health Settings")]
	public float healthCap;
	public LifeState lifeState;
	public float currentHealth;
	private float health 
	{ 
		get {return currentHealth;} 
		set 
		{
			if (value > 0) currentHealth = value; else player.alive = false;
		}
	}


	[Header("Spawning")]
	public float respawnTimeLimit;
	//exposed spawn characteristics to be assigned to the SpawnPoint[] spawn
	public Transform playerPrefab;
	public Transform[] spawnPoints = {default(Transform)};
	public Collider[] spawnGravitySources = {default(Collider)};
	public bool[] spawnGravityOn = {false};
	//contains all information used by spawn system
	private SpawnPoint[] spawn;

	void InitializeSpawnArray()
	{
		spawn = new SpawnPoint[spawnPoints.Length];
		for (int i = 0; i < spawn.Length; i++) 
			spawn [i] = new SpawnPoint (spawnPoints [i], spawnGravitySources [i], spawnGravityOn [i]);
	}

	[Header("Collision Settings")]
	public float collisionDmgFactor;
	public float velocityHarmThreshold;
	

	private Transform[] playerBodyParts;
	private float respawnTimer;

	//debug
	private Vector3 objectPosition;
	private Vector3 playerPosition;
	private Vector3 objectVelocity;
	private Vector3 playerVelocity;


	void Awake()
	{
		player = GetComponentInParent<PlayerManager> ();
		lifeState = new LifeState (this);
		InitializeSpawnArray ();
	}

	void Start()
	{
		//setting this boolean property spawns/kills the player character
		player.alive = true;
	}

	void Update()
	{
		if (!player.alive) CountDownToRespawn();
		/*else 
		{
			for (int i = 0; i < player.character.ragdollRigidbodies.Length ; i++)
			{
				player.character.ragdollRigidbodies[i].transform.SetEqualTo(player.character.characterRigidbodies[i].transform);

				print (player.character.ragdollRigidbodies[i] + " " + player.character.ragdollRigidbodies[i]);

					//if (player.character.ragdollTransforms[i].GetComponent<Rigidbody>()) ragdollTransforms[i].GetComponent<Rigidbody>().velocity = player.motion.velocity;
			}
		}*/

		PlayerInstaKill (InputCode.H);
	}

	void LateUpdate()
	{
		/*
		Debug.DrawRay (playerPosition + Vector3.up, playerVelocity * 3, Color.cyan);
		Debug.DrawRay (objectPosition, objectVelocity * 3, Color.magenta);
		Debug.DrawRay (playerPosition, (objectVelocity - playerVelocity) * 3, Color.green);
		print ("Player " + playerVelocity.magnitude + " Object " + objectVelocity.magnitude + " Difference " + (objectVelocity - playerVelocity).magnitude);
		*/
	}

	void PlayerInstaKill(InputCode inputCode)
	{
		if (InputX.Down (inputCode))
			player.alive = false;
	}
	

	public void CollisionDamage(Collider collider)
	{
		if (collider.GetComponent<Rigidbody>())
		{
			Rigidbody rigidbody = collider.GetComponent<Rigidbody>();
			Vector3 projectedVelocity = Vector3.Project (player.motion.prevVelocity, rigidbody.velocity);
			Vector3 relativeVelocity = rigidbody.velocity - projectedVelocity;
			
			if (relativeVelocity.sqrMagnitude >= velocityHarmThreshold.Square())
			{
				health -= Mathf.Clamp (relativeVelocity.magnitude - velocityHarmThreshold, 0, Mathf.Infinity) * rigidbody.mass * collisionDmgFactor ;
				print (Time.frameCount + "Damage " + (relativeVelocity.magnitude - velocityHarmThreshold) * rigidbody.mass * collisionDmgFactor);
				
				/*playerPosition = transform.position;
				objectPosition = collision.transform.position;
				playerVelocity = projectedVelocity;
				objectVelocity = collision.rigidbody.velocity;*/
			}
		}
	}

	void CountDownToRespawn()
	{
		if (respawnTimer < respawnTimeLimit)
			respawnTimer += Time.deltaTime;
		else
		{
			player.alive = true;
			respawnTimer = 0;
		}
	}


	public void SpawnPlayer ()
	{
		//set player health
		currentHealth = healthCap;

		//activate player motion
		player.body.gameObject.SetActive(true);

		//instantiate character's rigged mesh
		player.character = (Transform)Instantiate(playerPrefab, playerPrefab.position, playerPrefab.rotation);

		//parent character to player body
		player.character.SetParent (player.body, false);

		//associate scripts with PlayerManager
		player.animation = player.character.GetComponent<AnimationManager>();
		player.ragdoll = player.character.GetComponent<PlayerRagdoll> ();

		//initialize ragdoll and animation script fields
		player.animation.InitializeFields (player);
		player.ragdoll.InitializeFields ();

		for (int i = 0; i < player.ragdoll.rigidbodies.Length ; i++)
		{
			player.ragdoll.rigidbodies[i].isKinematic = true;
			player.ragdoll.rigidbodies[i].useGravity = false;
		}

		player.animation.enabled = true;
		player.animation.animator.enabled = true;

		player.camera.characterRenderer = player.character.GetComponentInChildren<SkinnedMeshRenderer> ();
		player.camera.characterRenderer.enabled = !player.camera.inFirstPerson;

		int randomSpawn = (int)Random.Range (0, spawn.Length);
		player.body.position = spawn[randomSpawn].position;
		player.body.rotation = spawn[randomSpawn].rotation;
		player.camera.t.rotation = spawn[randomSpawn].rotation;
		player.gravity.source = spawn[randomSpawn].initialGravSource;
		player.gravity.on = spawn[randomSpawn].startingGravOn;
	}


	public void KillPlayer()
	{
		currentHealth = 0;

		player.animation.animator.enabled = false;

		player.ragdoll.transform.parent = null;

		for (int i = 0; i < player.ragdoll.rigidbodies.Length ; i++)
			player.ragdoll.rigidbodies[i].isKinematic = false;

		player.ragdoll.gravityOn = player.gravity.on;
		player.ragdoll.gravitySource = player.gravity.source;

		player.ragdoll.deathVelocity = player.motion.GetComponent<Rigidbody>().velocity;
		player.ragdoll.lastDeathVelocityFrame = Time.frameCount + player.ragdoll.framesBeforeRelease;
		player.ragdoll.transferVelocity = true;

		//this prevents player from possessing their death velocity upon respawn
		player.body.GetComponent<Rigidbody>().velocity = Vector3.zero;

		player.body.gameObject.SetActive(false);

		player.camera.inFirstPerson = false;
		player.camera.characterRenderer.enabled = true;


	}

	public class LifeState
	{
		private PlayerLife playerLife;
		private bool currentState;
		public bool alive
		{
			get {return currentState;} 
			set 
			{
				if (value != currentState)
				{
					currentState = value;
					if (value) playerLife.SpawnPlayer();
					else playerLife.KillPlayer();
				}
			}
		}

		//constructor
		public LifeState(PlayerLife container) 
		{
			playerLife = container;
		}
	}

}



public class SpawnPoint
{
	public Vector3 position;
	public Quaternion rotation;
	public Collider initialGravSource;
	public bool startingGravOn;

	public SpawnPoint(Transform t, Collider gravSource, bool gravOn)
	{
		position = t.position;
		rotation = t.rotation;
		initialGravSource = gravSource;
		startingGravOn = gravOn;
	}
}
