using UnityEngine;
using System.Collections;

public class CameraFollowsObject : MonoBehaviour {

	public GameObject target;
	public float cameraVelocity = 0.0F;
	public float cameraSmoothTime = 0.1F;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		float newXPosition = Mathf.SmoothDamp (this.transform.position.x, target.transform.position.x, ref cameraVelocity, cameraSmoothTime);
		this.transform.position = new Vector3 (newXPosition, this.transform.position.y, this.transform.position.z);
	}
}
