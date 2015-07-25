using UnityEngine;
using System.Collections;

public class Collectable : MonoBehaviour, IEventReceiver {
		
	public CollectableType collectableType = CollectableType.REGULAR_PLANT;
	public RewardType rewardType = RewardType.SPEED;

	void Start() {
		EventManager.instance.addReceiver(EventType.PLAYER_DEATH, this);
		if(GetComponent<SpriteRenderer>() == null && GetComponentInChildren<SpriteRenderer>() == null) {
			Debug.LogWarning(name + " has no Sprite Renderer attached.");
		}
	}

	void OnTriggerStay2D (Collider2D enteringCollider) {
		if (enteringCollider.gameObject.tag == "Player" && enteringCollider.GetType().Equals(typeof(CircleCollider2D))) {
			FishController fish = enteringCollider.GetComponent<FishController>();
			if(fish != null && fish.getIsReadyToEat()) {
				GameStatistics.addCollectable(this.collectableType);
				GameStatistics.addReward(this.rewardType);
				fish.evolve();
				fish.GetComponent<AudioSource>().Play();

				if(rewardType == RewardType.NONE) {
					Destroy(this.gameObject);
				} else {
					setActiveState(false);
				}
			}
		}
	}

	public void handleEvent(EventType eventType) {
		if(eventType == EventType.PLAYER_DEATH) {
			setActiveState(true);
		}
	}

	private void setActiveState(bool active) {
		if(GetComponent<SpriteRenderer>() != null) {
			GetComponent<SpriteRenderer>().enabled = active;
		}
		foreach(SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>()) {
			renderer.enabled = active;
		}
		foreach(Collider2D collider in GetComponents<Collider2D>()) {
			collider.enabled = active;
		}
		foreach(Collider2D collider in GetComponentsInChildren<Collider2D>()) {
			collider.enabled = active;
		}
	}

}
