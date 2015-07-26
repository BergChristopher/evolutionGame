using UnityEngine;
using System.Collections;

public class FishtailController : MonoBehaviour {


	Animator animator;
	GameObject player;

	// Use this for initialization
	void Start () {
		animator = this.GetComponent<Animator> ();
		GameObject[] possiblePlayers = GameObject.FindGameObjectsWithTag("Player");
		player = null;
		foreach (GameObject possiblePlayer in possiblePlayers) {
			if(possiblePlayer.GetComponent<FishController>() != null) {
				player = possiblePlayer;
				break;
			}
		}
		if(player == null) {
			Debug.LogError("No player found.");
		}
	}
	
	// Update is called once per frame
	void Update () {
		animator.SetBool("isFast", player.GetComponent<FishController> ().getIsFast());
	}
}
