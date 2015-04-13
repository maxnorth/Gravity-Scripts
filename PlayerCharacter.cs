using UnityEngine;
using System.Collections;

public class PlayerCharacter : MonoBehaviour {

	PlayerManager player;

	public Transform ragdollPrefab;
	public Transform head;

	public Rigidbody[] ragdollRigidbodies;
	public Rigidbody[] characterRigidbodies;



	public void SwapWithRagdoll () 
	{
		if (!player) player = GetComponentInParent<PlayerManager>();

		//player.ragdoll = ((Transform)Instantiate (ragdollPrefab, ragdollPrefab.position, ragdollPrefab.rotation)).GetComponent<PlayerRagdoll> ();
		//characterTransforms = GetComponentsInChildren<Transform> ();
		//ragdollTransforms = player.ragdoll.GetComponentsInChildren<Transform> ();

		/*for (int i = 0; i < characterTransforms.Length ; i++)
		{
			ragdollTransforms[i].localPosition = characterTransforms[i].localPosition;
			ragdollTransforms[i].localRotation = characterTransforms[i].localRotation;
			ragdollTransforms[i].localScale = characterTransforms[i].localScale;

			print (ragdollTransforms[i] + " " + characterTransforms[i]);

			if (ragdollTransforms[i].GetComponent<Rigidbody>()) ragdollTransforms[i].GetComponent<Rigidbody>().velocity = player.motion.velocity;
		}*/

		CopyTransforms (transform, player.ragdoll.transform);

		player.ragdoll.gravityOn = player.gravity.on;


	}

	public void CopyTransforms(Transform src, Transform dest)
	{
		// error checking
		if (!src)
		{
			Debug.LogError("AdvancedRagdoll.CopyTransforms() " + name + " passed null src!");
			return;
		}
		
		if (!dest)
		{
			Debug.LogError("AdvancedRagdoll.CopyTransforms() " + name + " passed null dest!");
			return;
		}
		
		// copy position
		dest.localPosition = src.localPosition;
		// copy rotation
		dest.localRotation = src.localRotation;
		// copy scale
		dest.localScale = src.localScale;
		
		// copy children
		foreach (Transform sc in src)
		{
			Transform dc = dest.Find(sc.name);
			
			if (dc)
				CopyTransforms(sc, dc);
		}
	}
}
