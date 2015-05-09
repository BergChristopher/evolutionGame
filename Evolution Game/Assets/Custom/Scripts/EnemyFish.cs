using UnityEngine;
using System.Collections;

public class EnemyFish : MonoBehaviour {

	public FishType fishType = FishType.TEETH_FISH;
	public bool isMoving = true;
	public float speed = -3;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		if (isMoving) {
			this.transform.Translate(Vector3.right * speed * Time.deltaTime);
		}
	}

	void OnTriggerEnter2D (Collider2D enteringCollider) {
		if (enteringCollider.gameObject.tag == "Player") {
			FishController fish = enteringCollider.GetComponent<FishController>();
			if(fish != null) {
				GameStatistics.addDeathByFish(this.fishType);
				fish.die();
			}
		}
	}
}

public enum FishType { TEETH_FISH };