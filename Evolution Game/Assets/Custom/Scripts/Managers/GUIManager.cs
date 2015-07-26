using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class GUIManager : MonoBehaviour, IEventReceiver {

	private static GUIManager _instance;

	public Text lives;
	public Text collectables;
	public Text deaths;
	public Text gameOver;
	public Button restartButton;

	public static GUIManager instance {
		get {
			if(_instance == null) {
				_instance = GameObject.Find("StaticObjects").GetComponent<GUIManager>();
				if(_instance == null) {
					Debug.LogError("No GUIManager could be found!");
				}
			}
			
			return _instance;
		}
	}
	
	void Awake() {
		if (_instance == null) {
			_instance = this; //Assigning the first awakening GUIManager as the singleton	
			DontDestroyOnLoad(this);
		} else if (this != _instance){
			Debug.LogWarning("Found another GUIManager" + this);
			Destroy (this.gameObject); //Destroy every other GUIManager
		}
	}

	void Start() {
		if(lives == null) {
			Debug.LogError("No Text for lives attached to " + this.name);
		}
		if(deaths == null) {
			Debug.LogError("No Text for deaths attached to " + this.name);
		}
		if(collectables == null) {
			Debug.LogError("No Text for collectables attached to " + this.name);
		}
		if(restartButton == null) {
			Debug.LogWarning("No Button restartButton attached to " + this.name);
		} else {
			setButtonActiveState(restartButton, false);
		}
		EventManager.instance.addReceiver(EventType.GAME_OVER, this);
		EventManager.instance.addReceiver(EventType.GAME_WON, this);

	}

	void OnLevelWasLoaded(int level) {
		EventManager.instance.addReceiver(EventType.GAME_OVER, this);
		EventManager.instance.addReceiver(EventType.GAME_WON, this);
	}

	public void updateLivesText() {
		if(lives != null) {
			lives.text = "You have ";
			if(GameStatistics.getLives() == 1) {
				lives.text += "one live";
			} else {
				lives.text += GameStatistics.getLives() + " lives";
			}
			lives.text += " remaining.";
		}
	}

	public void updateDeathsText() {
		if(deaths != null) {
			Dictionary<FishType, int> deathsByFishType = GameStatistics.getDeathByFishType(); 

			deaths.text = "You have been killed ";
			if(GameStatistics.getDeaths() == 1) {
				deaths.text += "one time.";
			} else {
				deaths.text += GameStatistics.getDeaths() + " times.";
			}

			deaths.text += "\nOf those kills, ";
			if(GameStatistics.getDeathsByFish() == 1) {
				deaths.text += "one was";
			} else {
				deaths.text += GameStatistics.getDeathsByFish() + " were";
			}
			deaths.text += " caused by other fish.\n\n";

			foreach(KeyValuePair<FishType, int> pair in deathsByFishType) {
				deaths.text += pair.Key.ToString() + ": " + pair.Value.ToString() + "\n";
			}
		}
	}

	public void updateCollectablesText() {
		if(collectables != null) {
			Dictionary<CollectableType, int> gatheredCollectables = GameStatistics.getGatheredCollectables(); 
			collectables.text = "";
			foreach (KeyValuePair<CollectableType, int> pair in gatheredCollectables) {
				collectables.text += pair.Key.getDescription() + ": " + pair.Value.ToString() + "\n";
			}
			Dictionary<RewardType, int> gatheredRewards = GameStatistics.getGatheredRewards(); 
			foreach (KeyValuePair<RewardType, int> pair in gatheredRewards) {
				collectables.text += pair.Key.getDescription() + ": " + pair.Value.ToString() + "\n";
			}
		}
	}

	public void handleEvent(EventType eventType) {
		if(eventType == EventType.GAME_OVER) {
			gameOver.text = getGameOverText();
			setButtonActiveState(restartButton, true);
		} else if(eventType == EventType.GAME_WON) {
			gameOver.text = getGameWonText();
			setButtonActiveState(restartButton, true);
		}
	}

	private void setButtonActiveState(Button button, bool active) {
		button.gameObject.SetActive(active);
		if(button.GetComponentInChildren<CanvasRenderer>() != null) {
			button.GetComponentInChildren<CanvasRenderer>().SetAlpha(active ? 1 : 0);
		}
		if(button.GetComponentInChildren<Text>()) {
			button.GetComponentInChildren<Text>().color = active ? Color.black : Color.clear;
		}
	}

	private string getGameOverText() {
		return "You lost, try again!";
	}

	private string getGameWonText() {
		return "You won, great job!";
	}

}
