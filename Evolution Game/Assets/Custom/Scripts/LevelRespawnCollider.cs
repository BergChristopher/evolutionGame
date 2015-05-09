using UnityEngine;
using System.Collections;

public class LevelRespawnCollider : MonoBehaviour {

	public Vector3 startPosition = new Vector3(0,0,0);

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnTriggerEnter2D(Collider2D other) {
		other.gameObject.transform.position = startPosition;
	}
}
