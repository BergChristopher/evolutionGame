using UnityEngine;
using System.Collections;

public class Collectable : MonoBehaviour {
		
	public CollectableType collectableType = CollectableType.REGULAR_PLANT;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnTriggerEnter2D (Collider2D enteringCollider) {
		if (enteringCollider.gameObject.tag == "Player" && enteringCollider.GetType().Equals(typeof(CircleCollider2D))) {
			FishController fish = enteringCollider.GetComponent<FishController>();
			if(fish != null && fish.getIsReadyToEat()) {
				GameStatistics.addCollectable(this.collectableType);
				fish.evolve();
				Destroy(this.gameObject);
			}
		}
	}

}

public enum CollectableType { REGULAR_PLANT, ENEMY_FISH_EGG, ENEMY_FISH };
