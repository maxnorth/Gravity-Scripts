using UnityEngine;
using System.Collections;

public class AnimationManager : MonoBehaviour {

	private PlayerManager player;
	private PlayerInput input;
	[HideInInspector]
	public Animator animator;

	//store StringHash for each Animator variable
	private int forwardInputHash = Animator.StringToHash ("forwardInput");
	private int rightInputHash = Animator.StringToHash ("rightInput");
	private int blAimingHash = Animator.StringToHash ("blAiming");
	private int jumpedTriggerHash = Animator.StringToHash ("jumpedTrigger");
	private int blTouchingGroundHash = Animator.StringToHash ("blTouchingGround");
	private int distanceFromGroundHash = Animator.StringToHash ("distanceFromGround");

	//private int Hash = Animator.StringToHash ("");


	//Declare properties for reading/writing to animator variables
	public float forwardInput { get { return animator.GetFloat (forwardInputHash); } set { animator.SetFloat (forwardInputHash, value); } }
	public float rightInput { get { return animator.GetFloat (rightInputHash); } set { animator.SetFloat (rightInputHash, value); } }
	public bool blAiming { get { return animator.GetBool (blAimingHash); } set { animator.SetBool (blAimingHash, value); } }
	public bool blTouchingGround { get { return animator.GetBool (blTouchingGroundHash); } set { animator.SetBool (blTouchingGroundHash, value); } }
	public float distanceFromGround { get { return animator.GetFloat (distanceFromGroundHash); } set { animator.SetFloat (distanceFromGroundHash, value); } }


	//triggers (write only)
	public bool jumpedTrigger { set { if (value) animator.SetTrigger (jumpedTriggerHash); } }


	//public  { get { return animator.Get (); } set { animator.Set (, value); } }
	//public   { if (value) set { animator.SetTrigger (); } }

	public void InitializeFields(PlayerManager playerManager)
	{
		player = playerManager;
		input = player.input;
		animator = GetComponent<Animator> ();
	}

	void Update()
	{
		forwardInput = input.motionVector.z;
		rightInput = input.motionVector.x;
		blAiming = InputX.Pressed(input.shoot);
		blTouchingGround = player.state.touchingGround;
		distanceFromGround = player.gravity.distanceFromGround;
	}

}
