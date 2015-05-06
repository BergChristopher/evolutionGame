using UnityEngine;
using System.Collections;

public class CameraFollowsObject : MonoBehaviour {

	public GameObject target;
	public Vector2 cameraSmoothTime = new Vector2(0.1f,0.1f); //smooth time for x and y direction
	public Vector2 cameraDeadZone = new Vector2 (3f, 3f); //zone in which the camera wont follow the target yet in x and y direction
	public Vector2 cameraFollowAccuracy = new Vector2 (0.5f, 0.5f); //the accuracy of the camera following the target in x and y direction
	public bool followHorizontalMovement = true;
	public bool followVerticalMovement = true;

	private bool followHorizontal = false;
	private bool followVertical = false;

	private Vector2 cameraVelocity = new Vector2(0.0f,0.0f);

	// Use this for initialization
	void Start () {
		if (target == null) {
			Debug.LogError("No target set on " + this.name + " Component: CameraFollowsObject.cs");
		}
	}
	
	// Update is called once per frame
	void Update () {
		Vector3 newPosition = new Vector3(this.transform.position.x,this.transform.position.y,this.transform.position.z);
		if (followHorizontalMovement && (Mathf.Abs(target.transform.position.x - this.transform.position.x) > cameraDeadZone.x)) {
			followHorizontal = true;
		}
		if (followVerticalMovement && (Mathf.Abs(target.transform.position.y - this.transform.position.y) > cameraDeadZone.y)) {
			followVertical = true;
		}

		if (followHorizontal) {
			newPosition.x = Mathf.SmoothDamp (newPosition.x, target.transform.position.x, ref cameraVelocity.x, cameraSmoothTime.x);
			if(Mathf.Abs(newPosition.x - target.transform.position.x) <= cameraFollowAccuracy.x) {
				followHorizontal = false;
			}
		}
		if (followVertical) {
			newPosition.y = Mathf.SmoothDamp (newPosition.y, target.transform.position.y, ref cameraVelocity.y, cameraSmoothTime.y);
			if(Mathf.Abs(newPosition.y - target.transform.position.y) <= cameraFollowAccuracy.y) {
				followVertical = false;
			}
		}
		this.transform.position = newPosition;
	}
}
