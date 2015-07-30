using UnityEngine;
using System.Collections;

public class LevelSettings : MonoBehaviour {

	public const int ESTIMATED_FRAMES_PER_SECOND = 60;
	public const int SPEED_REWARDS_TO_UPGRADE = 2;
	public const int STRENGTH_REWARDS_TO_UPGRADE = 1;
	public const int LIBIDO_REWARDS_TO_MATE = 1;
	public const float MATING_DURATION = 11f;

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
	public Transform respawnPoint1 = null;
	public Transform respawnPoint2 = null;

	// Use this for initialization
	void Start () {
		if(respawnPoint1 == null || respawnPoint2 == null) {
			Debug.LogWarning(name + " is missing reference to respawn points.");
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
