using UnityEngine;
using System.Collections;

public class InteractionManager : MonoBehaviour 
{
	private PlayerManager player;
	private PlayerInput input;
	
	public LayerMask detectorMask;
	
	[HideInInspector] public EquippableWeapon weapon;
	[HideInInspector] public int weaponCount = 0;
	[HideInInspector] public RaycastHit rayHit;
	[HideInInspector] public Collider interactableObject;


	
	void Awake()
	{
		player = GetComponentInParent<PlayerManager> ();
		input = player.input;
	}
	
	void Update () 
	{
		
		if (interactableObject && (weapon ? weapon.itemName != interactableObject.name : true))
			
			if (InputX.Down (input.interact) || (!player.actions.equippedWeapon && player.actions.bAutoEquip))
		{
			weapon = interactableObject.gameObject.GetComponent<EquippableWeapon>();
			player.actions.equippedWeapon = Instantiate (weapon.equippedWeapon, weapon.equippedWeapon.transform.localPosition, weapon.equippedWeapon.transform.localRotation) as GameObject;
			//not how this should continue to work
			player.actions.equippedWeapon.transform.SetParent(player.camera.t, false);
			player.actions.projectileSpawn = player.actions.equippedWeapon.GetComponent<WeaponBehavior>().projectileSpawn;
			Destroy(interactableObject.gameObject);
			interactableObject = null;
			weapon = null;		
		}
	}
}
