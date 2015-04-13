using UnityEngine;
using System.Collections;

public class PlayerCollision : MonoBehaviour {

	PlayerManager player;

	void Awake ()
	{
		player = GetComponentInParent<PlayerManager>();
	}

	void OnCollisionEnter(Collision collision)
	{
		player.life.CollisionDamage(collision.collider);

		player.state.reachedGravSource = collision.collider == player.gravity.source;
	}

	void Update()
	{

	}
}
