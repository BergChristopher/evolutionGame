using UnityEngine;
using System.Collections;

public class CameraFollowsObject : MonoBehaviour {

	public GameObject target;
	public Vector2 cameraSmoothTime = new Vector2(0.1f,0.1f);
	public bool followHorizontalMovement = true;
	public bool followVerticalMovement = true;

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
		if (followHorizontalMovement) {
			newPosition.x = Mathf.SmoothDamp (newPosition.x, target.transform.position.x, ref cameraVelocity.x, cameraSmoothTime.x);
		}
		if (followVerticalMovement) {
			newPosition.y = Mathf.SmoothDamp (newPosition.y, target.transform.position.y, ref cameraVelocity.y, cameraSmoothTime.y);
		}
		this.transform.position = newPosition;
	}
}
