using UnityEngine;
using System.Collections;


public class PlayerActions : CustomMonoBehaviour 
{ 
	private PlayerManager player;
	private PlayerInput input;

	[Header("Shooting")]
	public GameObject equippedWeapon;
	public float fireRate = 3;
	public float bulletReach = 500;
	public float bulletForce = 1;
	public float bulletDamage = 1;
	public bool blFullyAutomatic; 
	public bool bRigidbodyProjectile = false;
	public LayerMask bulletLayerMask;
	

	[Header("Rigidbody Projectile")]
	public float projectileSpeed = 10;
	public float projectileSpread = 0;
	public Transform projectileFolder;
	public float projectileSpawnVelocityFactor;
	
	[Header("Melee")]
	public float meleeReach;
	public float meleeForce;

	[Header("Object Interaction")]
	//Referenced by interactionDetection.cs
	public bool bAutoEquip;


	private float triggerTime = 0;
	private float timeBetweenShots;
	private RaycastHit bulletHitInfo;
	private RaycastHit meleeHitInfo;

	public Transform testProjectile;
	public Rigidbody projectile;
	public Transform projectileSpawn;

	void Awake()
	{ 
		player = GetComponentInParent<PlayerManager> ();
		input = player.input;
	}

	// Use this for initialization

	void Start () 
	{


 		
		if (equippedWeapon)
		{
			equippedWeapon = Instantiate (equippedWeapon, equippedWeapon.transform.localPosition, equippedWeapon.transform.localRotation) as GameObject;
			equippedWeapon.transform.SetParent(player.camera.t, false);
			projectileSpawn = equippedWeapon.GetComponent<WeaponBehavior>().projectileSpawn;
			projectile = equippedWeapon.GetComponent<WeaponBehavior>().projectile;
		}
	}
	
	// Update is called once per frame
	void LateUpdate () 
	{
		Shoot ();

		MeleeObject ();
	}
	

	public void Shoot ()
	{
		timeBetweenShots = 1 / fireRate;

		if (((blFullyAutomatic && (InputX.Axis(input.shoot) > 0))) || (!blFullyAutomatic && InputX.Down(input.shoot)))
		{
			triggerTime += Time.deltaTime;

			while (triggerTime >= timeBetweenShots)
			{
				if (triggerTime >= timeBetweenShots) triggerTime -= timeBetweenShots;

				if (bRigidbodyProjectile) InstantiateProjectile ();
				else RaycastBullet ();

			}
		}
		else
		{

			if (triggerTime < timeBetweenShots) 
			{
				triggerTime += Time.deltaTime;
				if (triggerTime > timeBetweenShots) triggerTime = timeBetweenShots;
			}
		}
	
	}

	public void MeleeObject()
	{
		if (InputX.Down(input.punch))
		{
			if (Physics.Raycast(player.camera.t.position + (player.camera.t.forward / 2), player.camera.t.forward, out meleeHitInfo, meleeReach) && meleeHitInfo.rigidbody)
			{
				meleeHitInfo.rigidbody.AddForceAtPosition(player.camera.t.forward * meleeForce, meleeHitInfo.point, ForceMode.Impulse);
			}
		}
	}

	void InstantiateProjectile ()
	{
		Rigidbody currentProjectile;
		Vector2 randomPoint = Random.insideUnitCircle * projectileSpread;
		Vector3 randomVector = new Vector3(randomPoint.x, randomPoint.y, 1.0f);
		Vector3 shootVector = projectileSpawn.TransformDirection (randomVector).normalized;
		currentProjectile = Instantiate(testProjectile.GetComponent<Rigidbody> (), projectileSpawn.position, projectileSpawn.rotation) as Rigidbody;
		//print (currentProjectile);
		currentProjectile.AddForce ((shootVector * projectileSpeed) + rigidbody.velocity, ForceMode.VelocityChange);
		currentProjectile.transform.SetParent (projectileFolder);
	}

	void RaycastBullet ()
	{
		if (Physics.Raycast (player.camera.t.position, player.camera.t.forward, out bulletHitInfo, bulletReach, bulletLayerMask))
			if (bulletHitInfo.rigidbody)
			{ 
				if (bulletHitInfo.transform.GetComponentInParent<Animator> ())
				{
					bulletHitInfo.transform.GetComponentInParent<Animator> ().enabled = false;
					foreach (Rigidbody rb in bulletHitInfo.transform.GetComponentInParent<Animator> ().GetComponentsInChildren<Rigidbody>())
					{
						rb.isKinematic = false;
					}
						
				}
		
				bulletHitInfo.rigidbody.AddForceAtPosition(player.camera.t.forward * bulletForce, bulletHitInfo.point, ForceMode.Impulse);
			}
	}
}
