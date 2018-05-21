using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;



public class CameraControls : MonoBehaviour {
	const float PI = (float)Math.PI;

	private float distance = 7.0f;

	private bool isTurning = false;
	private bool isZooming = false;

	public float sensitivityAngle = PI;
	public float sensitivityZoom = 16.0f;

	private float pitch = 0.0f;
	private float yaw = 0.0f;

	private float pitchStart = 0.0f;
	private float pitchEnd = PI * 0.25f;
	private float yawStart = 0.0f;
	private float yawEnd = PI * 0.25f;

	private float velP = 0.0f;
	private float velY = 0.0f;

	private Vector3 clickOrigin;
	private Vector3 center = new Vector3 (0, 2.5f);

	private float clamp(float x, float min, float max) {
		return Math.Max(Math.Min(max, x), min);
	}

	private Vector3 transformFromAngles() {
		pitch = clamp (pitch, -0.25f * PI, 0.499f * PI);
		float sp = (float) Math.Sin (pitch);
		float cp = (float) Math.Cos (pitch);
		float sy = (float) Math.Sin (yaw);
		float cy = (float) Math.Cos (yaw);

		return new Vector3 (cp * cy, sp, cp * sy);
	}

	// Use this for initialization
	void Start () {
		transform.position = center + distance * transformFromAngles ();
		transform.rotation = Quaternion.LookRotation (new Vector3(0, 2.5f) - transform.position); 
	}
	
	// Update is called once per frame
	void Update () {
		
		if (Input.GetMouseButtonDown (0)) {
			clickOrigin = Input.mousePosition;
			yawStart = yaw;
			pitchStart = pitch;
			isTurning = true;
		} 

		if (Input.GetMouseButtonDown (1)) {
			clickOrigin = Input.mousePosition;
			isZooming = true;
		}

		if (!Input.GetMouseButton (0)) isTurning = false;
		if (!Input.GetMouseButton (1)) isZooming = false;

		Vector3 displacement = Camera.main.ScreenToViewportPoint (Input.mousePosition - clickOrigin);

		if (isTurning) {
			pitchEnd = pitchStart - displacement.y * PI * 0.5f;
			yawEnd = yawStart - displacement.x * PI;
		}

		if (isZooming) {
			distance += -displacement.y * Time.deltaTime * sensitivityZoom;
			distance = clamp (distance, 3.0f, 12.0f);
		}

		pitch = Mathf.SmoothDamp(pitch, pitchEnd, ref velP, 0.3f);
		yaw = Mathf.SmoothDamp(yaw, yawEnd, ref velY, 0.3f);

		transform.position = center + distance * transformFromAngles ();
		transform.rotation = Quaternion.LookRotation (center - transform.position); 

		if (Math.Abs (yawEnd - yaw) < 0.001f) {
			yawStart = yawStart % (2.0f * PI);
			yawEnd = yawEnd % (2.0f * PI);
			yaw = yawEnd;
		}
	}
}
