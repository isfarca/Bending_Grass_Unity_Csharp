using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCube : MonoBehaviour {

	public BendinGrass.PermanentBendingVolume volume;
	public bool enableOnPress;
	public string enableButton = "Fire1";
	BendinGrass.GrassBender bender;

	// Use this for initialization
	void Start () {
		bender = GetComponentInChildren<BendinGrass.GrassBender>();
	}
	
	// Update is called once per frame
	void Update () {
		Move();
		Commands();
	}

	void Move()
	{
		Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
		if (input.sqrMagnitude < .02f)
			return;

		Vector3 dir = transform.forward * input.y + transform.right * input.x;
		dir.y = 0f;
		dir.Normalize();

		transform.position += dir * Time.deltaTime * 20f;
	}

	void Commands()
	{
		if (Input.GetButtonDown("Jump"))
			if (volume)
				volume.InitializeVolumeMap();

		if (enableOnPress)
		{
			if (bender)
			{
				bender.enabled = Input.GetButton(enableButton);
			}
		}
		else
		{
			bender.enabled = true;
		}
	}
}
