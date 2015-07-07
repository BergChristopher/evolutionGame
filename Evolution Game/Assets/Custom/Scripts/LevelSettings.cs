using UnityEngine;
using System.Collections;

public class LevelSettings : MonoBehaviour {

	private static LevelSettings _instance;

	public static LevelSettings instance
	{
		get
		{
			if(_instance == null)
				_instance = GameObject.FindObjectOfType<LevelSettings>();
			return _instance;
		}
	}

	public float airToWaterTransitionHeight = 0.0f;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
