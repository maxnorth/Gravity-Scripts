using UnityEngine;
using System.Collections;

public class InteractionDetection : MonoBehaviour {

	private PlayerManager player;
	
	void Awake ()
	{
		player = GetComponentInParent<PlayerManager>();
	}
	
	void OnTriggerEnter(Collider interactable)
	{
		if (player.interaction.weaponCount++ == 0) player.interaction.interactableObject = interactable;
	}
	
	void OnTriggerExit()
	{
		if (--player.interaction.weaponCount == 0) player.interaction.interactableObject = null;
	}
	
	void OnTriggerStay()
	{
		if (player.interaction.weaponCount > 1)
		{
			if (Physics.Raycast (transform.parent.position, transform.parent.forward, out player.interaction.rayHit, 2, player.interaction.detectorMask))
				
				player.interaction.interactableObject = player.interaction.rayHit.collider;
		}
	}
}