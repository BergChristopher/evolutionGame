using UnityEngine;
using System.Collections;

public class Collectable : MonoBehaviour {
		
	public CollectableType collectableType = CollectableType.REGULAR_PLANT;
	public RewardType rewardType = RewardType.SPEED;

	void OnTriggerStay2D (Collider2D enteringCollider) {
		if (enteringCollider.gameObject.tag == "Player" && enteringCollider.GetType().Equals(typeof(CircleCollider2D))) {
			FishController fish = enteringCollider.GetComponent<FishController>();
			if(fish != null && fish.getIsReadyToEat()) {
				GameStatistics.addCollectable(this.collectableType);
				GameStatistics.addReward(this.rewardType);
				fish.evolve();
				fish.GetComponent<AudioSource>().Play();
				Destroy(this.gameObject);
			}
		}
	}
}
