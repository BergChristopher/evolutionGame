using UnityEngine;
using System.Collections;

public class FisheyeController : MonoBehaviour {
	
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
		animator.SetBool("isHorny", player.GetComponent<FishController> ().getIsReadyToMate());
		/*if (player.GetComponent<FishController> ().getIsStrong ()) {
			this.transform.position = new Vector3(player.transform.position.x -2.4f, player.transform.position.x + 0.19f, player.transform.position.x + 0f);
		} else {
			this.transform.position = new Vector3(player.transform.position.x + -1.13f, player.transform.position.x + 0.51f, player.transform.position.x + 0f);
		}*/
	}
}
