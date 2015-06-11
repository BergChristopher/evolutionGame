using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class GUIManager : MonoBehaviour {

	private static GUIManager _instance;

	public Text lives;
	public Text deaths;

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
	}

	public void updateLivesText() {
		lives.text = "You have ";
		if(GameStatistics.getLives() == 1) {
			lives.text += "one live";
		} else {
			lives.text += GameStatistics.getLives() + " lives";
		}
		lives.text += " remaining.";
	}

	public void updateDeathsText() {
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
