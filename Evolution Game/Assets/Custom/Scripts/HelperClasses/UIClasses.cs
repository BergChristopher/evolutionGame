﻿using UnityEngine;
using System.Collections;

public class UIClasses : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void loadLevel(string name) {
		if(EventManager.instance != null) {
			EventManager.instance.triggerEvent(EventType.GAME_RESTARTED);
		}
		Application.LoadLevel(name);
	}
}
