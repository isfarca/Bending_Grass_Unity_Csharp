using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCamera : MonoBehaviour {
	[Range(0f, 10f)]
	public float speed;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		Vector3 movement = transform.forward * Input.GetAxis("Vertical") + transform.right * Input.GetAxis("Horizontal");
		movement = Vector3.ClampMagnitude(movement, 1f);

		this.transform.position += movement * speed * Time.deltaTime;
	}
}
