using UnityEngine;
using System;

public class PlayerInput : CustomMonoBehaviour 
{
	PlayerManager player;
	[SerializeField]
	int poo { get; set; }

	[Header("Settings")]
	public float buttonPressThreshold; 
	//public float rightStickSensitivity;
	//public float leftStickSensitivity;
	public float delayThreshold;
	[Header("Input Selection")]
	public InputProfile inputProfile;
	public InputCode[] jump = 					{InputCode.Space, InputCode.MacXboxButtonA};
	public InputCode[] run = 					{InputCode.None};
	public InputCode[] toggleInertiaDampener = 	{InputCode.None};
	public InputCode[] shoot = 					{InputCode.MouseClickLeft, InputCode.MacXboxRightTrigger};
	public InputCode[] punch = 					{InputCode.None};
	public InputCode[] interact = 				{InputCode.None};
	public InputCode[] switchViewMode = 		{InputCode.None};
	public InputCode[] allowVerticalMotion = 	{InputCode.None};
	public InputCode[] allowCameraRoll = 		{InputCode.None};
	public InputCode[] moveForward = 			{InputCode.None};
	public InputCode[] moveSideways = 			{InputCode.None};
	public InputCode[] lookVertical = 			{InputCode.None};
	public InputCode[] lookHorizontal = 		{InputCode.None};
	public InputCode[] toggleRagdollGravity = 	{InputCode.None};
	public InputCode[] toggleAnimator = 		{InputCode.None};

	//public InputCode[] yourInputCommand = 		{InputCode.None};

	[Header("Timed Input")]
	public TimedInput toggleGravity; 
	public TimedInput selectGravity;

	[HideInInspector] public float jumpTimer = 10;

	[HideInInspector] public Vector3 motionVector;
	[HideInInspector] public Vector3 lookVector;

	void Start()
	{
		player = GetComponent<PlayerManager>();
	}

	void Update()
	{
		if (InputX.Down (jump)) jumpTimer = 0;
		if (jumpTimer < delayThreshold) jumpTimer += Time.deltaTime;

		lookVector.y = InputX.Axis (lookVertical);
		lookVector.x = InputX.Axis (lookHorizontal);

		motionVector.x = InputX.Axis (moveSideways);
		motionVector.z = InputX.Axis (moveForward);
		motionVector.ClampMagnitude(1);
	}

	[Serializable]
	public struct InputProfile
	{
		public InputCode[] jump;
		public InputCode[] toggleGravity;
		public InputCode[] selectGravity;
		public InputCode[] run;
		public InputCode[] toggleInertiaDampener;
		public InputCode[] shoot;
		public InputCode[] punch;
		public InputCode[] interact;
		public InputCode[] switchViewMode;
	}
}