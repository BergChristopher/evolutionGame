using UnityEngine;
using System.Collections;

public class FishController : MonoBehaviour {

	private CharacterController controller;
	private Vector3 speed = Vector3.zero;

	// Use this for initialization
	void Start () {
		controller = GetComponent<CharacterController> ();
	}
	
	// Update is called once per frame
	void Update () {
		speed.x = Input.GetAxis ("Horizontal");

		controller.Move(speed);
	}
}
