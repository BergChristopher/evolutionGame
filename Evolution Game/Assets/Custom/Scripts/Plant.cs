using UnityEngine;
using System.Collections;

public class Plant : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnTriggerEnter2D (Collider2D enteringCollider) {
		Debug.Log ("trigger enter");
		if (enteringCollider.gameObject.tag == "Player") {
			Debug.Log ("is player");
			Destroy(this.gameObject);
		}
	}

}
