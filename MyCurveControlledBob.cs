using System;
using UnityEngine;


[Serializable]
public class MyCurveControlledBob
{
	public float HorizontalBobRange = 0.33f;
	public float VerticalBobRange = 0.33f;
	public AnimationCurve Bobcurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f),
	                                                    new Keyframe(1f, 0f), new Keyframe(1.5f, -1f),
	                                                    new Keyframe(2f, 0f)); // sin curve for head bob
	public float VerticaltoHorizontalRatio = 1f;
	public float BobBaseStepInterval;
	
	private float m_CyclePositionX = 0; //set/used in DoHeadBob()
	private float m_CyclePositionY = 0; //set/used in DoHeadBob()
	private float m_CurveLength { get {return Bobcurve[Bobcurve.length - 1].time;} } //init in Setup()
	private Vector3 m_WorldBob;

	
	public Vector3 GetHeadBob(float speed)
	{
		//normal version
		//float xPos = m_OriginalCameraPosition.x + (Bobcurve.Evaluate(m_CyclePositionX)*HorizontalBobRange);
		//float yPos = m_OriginalCameraPosition.y + (Bobcurve.Evaluate(m_CyclePositionY)*VerticalBobRange);
		
		//my version returns the difference of the bob itself, not new local coordinates for the camera transform
		m_WorldBob.x = (Bobcurve.Evaluate(m_CyclePositionX)*HorizontalBobRange);
		m_WorldBob.y = (Bobcurve.Evaluate(m_CyclePositionY)*VerticalBobRange);
		
		m_CyclePositionX += (speed * Time.deltaTime)/BobBaseStepInterval;
		m_CyclePositionY += ((speed * Time.deltaTime) / BobBaseStepInterval) * VerticaltoHorizontalRatio;
		
		if (m_CyclePositionX > m_CurveLength)
		{
			m_CyclePositionX = m_CyclePositionX - m_CurveLength;
		}
		if (m_CyclePositionY > m_CurveLength)
		{
			m_CyclePositionY = m_CyclePositionY - m_CurveLength;
		}
		
		return m_WorldBob;
	}
}