using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyFish : MonoBehaviour {

	public FishType fishType = FishType.TEETH_FISH;
	public bool isMoving = true;
	public float speed = 3;
	public List<Transform> waypoints = new List<Transform>(); 

	private bool isFacingRight = true;

	private int currentWaypoint = 0;
	private float lastCollission = 0; 

	// Use this for initialization
	void Start () {
		if(waypoints.Count < 2 && fishType.Equals(FishType.TEETH_FISH)) {
			Debug.LogWarning("Your fish " + name + " has less than 2 waypoints assigned ");
		}
		if(speed < 0) {
			Debug.LogWarning("Your fish " + name + " has a speed less than 0, it will be converted to " + Mathf.Abs(speed) + ".");
		}
	}
	
	// Update is called once per frame
	void Update () {
		if(isMoving) {
			if(fishType.Equals(FishType.TEETH_FISH)) {
				updateWaypointBasedFishMovement();
				updateWaypointBasedDirection();
			} else {
				updateNormalFishMovement();
			}

			updateRotation();
		}
		
	}

	void OnTriggerEnter2D (Collider2D enteringCollider) {
		//eat player
		if(fishType.Equals(FishType.TEETH_FISH) || fishType.Equals(FishType.WHITE_SHARK)) {
			if (enteringCollider.gameObject.tag == "Player") {
				FishController fish = enteringCollider.GetComponent<FishController>();
				if(fish != null) {
					GameStatistics.addDeathByFish(this.fishType);
					fish.die();
				}
			}
		}

		//change direction on touching ground
		if(fishType.Equals(FishType.NEUTRAL_FISH) || fishType.Equals(FishType.WHITE_SHARK)) {
			if(enteringCollider.gameObject.tag == "Environment" && (lastCollission + 1f) < Time.time ) {
				isFacingRight = !isFacingRight;
				lastCollission = Time.time;
			}
		}
	}

	private void updateWaypointBasedFishMovement() {
		if (waypoints.Count >= 2) {
			Vector2 fishPosition = new Vector2(transform.position.x, transform.position.y);
			Vector2 target = new Vector2(waypoints[currentWaypoint].position.x, waypoints[currentWaypoint].position.y);
			fishPosition = Vector2.MoveTowards(fishPosition, target, Mathf.Abs(speed * Time.deltaTime));

			if(fishPosition.x == target.x && fishPosition.y == target.y) {
				currentWaypoint = (currentWaypoint + 1) % waypoints.Count;
			}

			transform.position = new Vector3(fishPosition.x, fishPosition.y, this.transform.position.z);
		}
	}

	private void updateWaypointBasedDirection() {
		if(transform.rotation.x < waypoints[currentWaypoint].position.x) {
			isFacingRight = true;
		} else if(transform.rotation.x > waypoints[currentWaypoint].position.x){
			isFacingRight = false;
		}
	}

	private void updateNormalFishMovement() {
		Vector2 fishPosition = new Vector2(transform.position.x, transform.position.y);
		Vector2 target = new Vector2(transform.position.x + 100f, transform.position.y);
		if(!isFacingRight) {
			target.x = transform.position.x -100f;
		}
		fishPosition = Vector2.MoveTowards(fishPosition, target, Mathf.Abs(speed * Time.deltaTime));

		transform.position = new Vector3(fishPosition.x, fishPosition.y, this.transform.position.z);
	}

	private void updateRotation() {
		Quaternion fishRotation = transform.rotation;
		if(isFacingRight) {
			fishRotation.y = 180;
		} else if(!isFacingRight){
			fishRotation.y = 0;
		}
		transform.rotation = fishRotation; 
	}
}

public enum FishType { TEETH_FISH, NEUTRAL_FISH, WHITE_SHARK };