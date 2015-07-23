using UnityEngine;
using System.Collections;

public class ResetTransformOnPlayerDeath : MonoBehaviour, IEventReceiver {

	private EventType eventType = EventType.PLAYER_DEATH;

	private Vector3 startPosition;
	private Vector3 startRotation;

	// Use this for initialization
	void Start () {
		startPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);
		startRotation = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z);
		EventManager.instance.addReceiver(eventType, this);
	}

	void OnDestroy() {
		EventManager.instance.removeReceiver(this);
	}

	public void handleEvent(EventType eventType) {
		if(eventType.Equals(EventType.PLAYER_DEATH)) {
			transform.position = startPosition;
			transform.eulerAngles = startRotation;
		}
	}
}
