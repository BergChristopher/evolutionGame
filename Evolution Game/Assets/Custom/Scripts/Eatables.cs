using UnityEngine;
using System.Collections;

public class Eatables : MonoBehaviour {
		
	public EatableType eatableType = EatableType.REGULAR_PLANT;

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
				GameStatistics.addEatable(this.eatableType);
				fish.evolve();
				Destroy(this.gameObject);
			}
		}
	}

}

public enum EatableType { REGULAR_PLANT, ENEMY_FISH_EGG };
