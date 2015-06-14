using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyFish : MonoBehaviour {

	public FishType fishType = FishType.TEETH_FISH;
	public MovementType movementType = MovementType.HORIZONTAL;
	public MovementType secondaryMovementType =MovementType.NONE;
	public bool isMoving = true;
	public float speed = 3;
	public List<Transform> waypoints = new List<Transform>(); 

	public float awarenessRadius = 15f; 
	
	private bool isFacingRight = true;

	//waypoint based movement variables
	private int currentWaypoint = 0;

	//horizontal movement variables 
	private float lastCollission = 0; 

	//follow player based movement 
	private GameObject player = null;
	private float lastRotation = 0;

	//guard starting spot variables
	private Vector2 guardedSpot = Vector2.zero;


	// Use this for initialization
	void Start () {
		if(waypoints.Count < 2 && movementType.Equals(MovementType.WAYPOINT_BASED)) {
			Debug.LogWarning("Your fish " + name + " has less than 2 waypoints assigned.");
		}
		if(speed < 0) {
			Debug.LogWarning("Your fish " + name + " has a speed less than 0, it will be converted to " + Mathf.Abs(speed) + ".");
		}
		player = GameObject.FindGameObjectWithTag("Player");
		if(player == null || player.GetComponent<FishController>() == null) {
			player = null;
			if(movementType.Equals(MovementType.FOLLOW_PLAYER)) {
				Debug.LogWarning("Your fish " + name + " cannot find the player.");
			}
		}
		isFacingRight = this.transform.rotation.y == 180;  
		guardedSpot = new Vector2(this.transform.position.x, this.transform.position.y);
	}
	
	// Update is called once per frame
	void Update () {
		updateMovement(movementType);
		
	}

	void OnTriggerEnter2D(Collider2D enteringCollider) {
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
		if(enteringCollider.gameObject.tag == "Environment" && (lastCollission + 1f) < Time.time ) {
			isFacingRight = !isFacingRight;
			lastCollission = Time.time;
		}
	}

	void updateMovement(MovementType currentMovementType)
	{
		if (isMoving) {
			if (currentMovementType.Equals (MovementType.WAYPOINT_BASED)) {
				updateWaypointBasedFishMovement ();
				updateWaypointBasedDirection ();
			}
			else if (currentMovementType.Equals (MovementType.HORIZONTAL)) {
				updateNormalFishMovement ();
			}
			else if (currentMovementType.Equals (MovementType.FOLLOW_PLAYER)) {
				updateMoveToTarget (new Vector2 (player.transform.position.x, player.transform.position.y), false);
				updateDirectionToTarget (new Vector2 (player.transform.position.x, player.transform.position.y), false);
			}
			else if (currentMovementType.Equals (MovementType.GUARD_STARTING_SPOT)) {
				updateMoveToTarget (guardedSpot, true);
				updateDirectionToTarget (guardedSpot, true);
			}
			updateRotation ();
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
		if(!isFacingRight && transform.position.x < waypoints[currentWaypoint].position.x) {
			isFacingRight = true;
		} else if(isFacingRight && transform.position.x > waypoints[currentWaypoint].position.x){
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

	private void updateMoveToTarget(Vector2 target, bool ignoreAwarenessRadius) {
		if (ignoreAwarenessRadius || Vector2.Distance(target, getMouthPosition()) < awarenessRadius) {
			Vector2 fishMouthPosition = getMouthPosition();
			Vector2 newFishMouthPosition = Vector2.MoveTowards(fishMouthPosition, target, Mathf.Abs(speed * Time.deltaTime));

			Vector2 movement = newFishMouthPosition - fishMouthPosition;

			transform.position = new Vector3(this.transform.position.x + movement.x, this.transform.position.y + movement.y, this.transform.position.z);
		} else {
			updateMovement(secondaryMovementType);
		}
	}

	private void updateDirectionToTarget(Vector2 target, bool ignoreAwarenessRadius) { 
		Vector2 fishMouthPosition = getMouthPosition();
		if (ignoreAwarenessRadius || Vector2.Distance(target, getMouthPosition()) < awarenessRadius) {
			if(!isFacingRight && fishMouthPosition.x + (Mathf.Abs(fishMouthPosition.x - transform.position.x) / 2) < target.x && (lastRotation + 1f) < Time.time) {
				isFacingRight = true;
				lastRotation = Time.time;
			} else if(isFacingRight && fishMouthPosition.x - (Mathf.Abs(fishMouthPosition.x - transform.position.x) / 2) > target.x && (lastRotation + 1f) < Time.time){
				isFacingRight = false;
				lastRotation = Time.time;
			}
		} 
	}

	private void updateRotation() {
		Quaternion fishRotation = transform.rotation;
		if(isFacingRight) {
			fishRotation.y = 180;
		} else {
			fishRotation.y = 0;
		}
		transform.rotation = fishRotation; 
	}

	private Vector2 getMouthPosition() {
		Vector2 result = new Vector2 (this.transform.position.x, this.transform.position.y);
		if(GetComponents<CircleCollider2D>().Length > 1) {
			Debug.LogWarning(this.name + " has more than one Circle Collider attached, which could be the mouth.");
		} else if(GetComponent<CircleCollider2D>() != null) {
			Vector2 offset = new Vector2(this.transform.localScale.x * GetComponent<CircleCollider2D>().offset.x, this.transform.localScale.y * GetComponent<CircleCollider2D>().offset.y);
			if(isFacingRight) {
				offset.x *= -1;
			}
			result += offset;
		} else {
			Debug.LogWarning(this.name + " has no Circle Collider attached, which could be the mouth.");
		}
		return result;
	}
}

public enum FishType { TEETH_FISH, NEUTRAL_FISH, WHITE_SHARK };
public enum MovementType { NONE, WAYPOINT_BASED, HORIZONTAL, FOLLOW_PLAYER, GUARD_STARTING_SPOT  };