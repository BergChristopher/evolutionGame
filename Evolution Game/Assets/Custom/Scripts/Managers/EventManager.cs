using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EventManager : MonoBehaviour {

	private static EventManager _instance;

	private Dictionary<EventType, List<IEventReceiver>> eventReceivers;

	public static EventManager instance {
		get {
			if(_instance == null) {
				_instance = GameObject.FindObjectOfType<EventManager>();
				if(_instance == null) {
					Debug.LogError("No EventManager could be found!");
				}
			}
			
			return _instance;
		}
	}
	
	void Awake() {
		if (_instance == null) {
			_instance = this; //Assigning the first awakening EventManager as the singleton	
			if(eventReceivers == null) {
				eventReceivers = new Dictionary<EventType, List<IEventReceiver>>();
			}
			DontDestroyOnLoad(this);
		} else if (this != _instance){
			Debug.LogWarning("Found another EventManager" + this);
			Destroy (this.gameObject); //Destroy every other EventManager
		}
	}

	void Start() {

	}

	public void addReceiver(EventType eventType, IEventReceiver receiver) {
		if(!eventReceivers.ContainsKey(eventType)) {
			eventReceivers.Add(eventType, new List<IEventReceiver>());
		}
		if(!eventReceivers[eventType].Contains(receiver)) {
			eventReceivers[eventType].Add(receiver);
		}
	}

	public void removeReceiver(IEventReceiver receiver) {
		foreach(List<IEventReceiver> receivers in eventReceivers.Values) {
			receivers.Remove(receiver);
		}
	}

	public void triggerEvent(EventType eventType) {
		if(eventReceivers.ContainsKey(eventType)) {
			foreach(IEventReceiver receiver in eventReceivers[eventType]) {
				receiver.handleEvent(eventType);
			}
		}

		if(eventType == EventType.GAME_RESTARTED) {
			GameStatistics.clearAll();
			eventReceivers.Clear();
		}
	}
}

public interface IEventReceiver {
	void handleEvent(EventType triggeredEvent);
}
