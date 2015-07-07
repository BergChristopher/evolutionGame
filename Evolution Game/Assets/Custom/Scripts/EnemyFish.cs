using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyFish : MonoBehaviour {

	public FishType fishType = FishType.TEETH_FISH;
	public MovementType movementType = MovementType.HORIZONTAL;
	public MovementType secondaryMovementType =MovementType.NONE;
	public bool isMoving = true;
	public float speed = 3;
	public float rotationSpeed = 50;
	public List<Transform> waypoints = new List<Transform>(); 

	public float awarenessRadius = 15f; 
	
	private bool isFacingRight = true;
	private bool isTurning = false;
	private bool canBeEatenByPlayer = false;

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
		isFacingRight = Mathf.Abs((this.transform.rotation.eulerAngles.y % 360) - 180) <= 45;  
		guardedSpot = new Vector2(this.transform.position.x, this.transform.position.y);

		if(FishType.EATABLE_FISH.Equals(this.fishType)) {
			foreach( Collider2D collider in GetComponents<Collider2D>()) {
				if(collider.isTrigger == true) {
					canBeEatenByPlayer = true;
					break;
				}
			}
			if(!canBeEatenByPlayer) {
				Debug.LogError("Eatable fish " + name + " has no trigger 2D Collider attached." );
			}
		}

		if(GetComponents<CircleCollider2D>().Length > 1) {
			Debug.LogWarning(this.name + " has more than one Circle Collider attached, which could be the mouth.");
		} else if(GetComponent<CircleCollider2D>() == null) {
			Debug.LogWarning(this.name + " has no Circle Collider attached, which could be the mouth.");
		} 

	}
	
	// Update is called once per frame
	void Update () {
		updateMovement(movementType);
	}

	void OnTriggerStay2D(Collider2D enteringCollider) {
		//eat player
		if(fishType.Equals(FishType.TEETH_FISH) || fishType.Equals(FishType.WHITE_SHARK)) {
			if (enteringCollider.gameObject.tag == "Player") {
				FishController fish = enteringCollider.GetComponent<FishController>();
				if(fish != null) {
					GameStatistics.addDeathByFish(this.fishType);
					GetComponent<AudioSource>().Play();
					fish.die();
				}
			}
		}

		//be eaten by player
		if (canBeEatenByPlayer && enteringCollider.gameObject.tag == "Player" && enteringCollider.GetType().Equals(typeof(CircleCollider2D))) {
			FishController fish = enteringCollider.GetComponent<FishController>();
			if(fish != null && fish.getIsReadyToEat()) {
				GameStatistics.addCollectable(CollectableType.ENEMY_FISH);
				fish.evolve();
				Destroy(this.gameObject);
			}
		}

	}

	void OnCollisionEnter2D(Collision2D collision) {
		foreach (ContactPoint2D contact in collision.contacts) {
			//change direction on touching ground
			if(contact.collider.gameObject.tag == "Environment" && (lastCollission + 1f) < Time.time && contact.otherCollider.GetType().Equals(typeof(EdgeCollider2D)) ) {
				turnFish();
				lastCollission = Time.time;
			}
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
				if(player == null) {
					Debug.LogWarning("No player attached to " + name );
				} else {
					updateMoveToTarget (new Vector2 (player.transform.position.x, player.transform.position.y), false);
					updateDirectionToTarget (new Vector2 (player.transform.position.x, player.transform.position.y), false);
				}
			}
			else if (currentMovementType.Equals (MovementType.FLEE_FROM_PLAYER)) {
				if(player == null) {
					Debug.LogWarning("No player attached to " + name );
				} else {
					float newXTargetPosition = transform.position.x + (transform.position.x - player.transform.position.x);
					float newYTargetPosition = transform.position.y + (transform.position.y - player.transform.position.y);
					updateMoveToTarget (new Vector2 (newXTargetPosition, newYTargetPosition), false);
					updateDirectionToTarget (new Vector2 (newXTargetPosition, newYTargetPosition), false);
				}
			}
			else if (currentMovementType.Equals (MovementType.GUARD_STARTING_SPOT)) {
				updateMoveToTarget (guardedSpot, true);
				updateDirectionToTarget (guardedSpot, true);
			}
			limitPosition();
			updateRotation ();
		}
	}

	private void updateWaypointBasedFishMovement() {
		if(!isTurning) {
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
	}

	private void updateWaypointBasedDirection() {
		if(!isTurning) {
			if(!isFacingRight && transform.position.x < waypoints[currentWaypoint].position.x) {
				turnFish();
			} else if(isFacingRight&& transform.position.x > waypoints[currentWaypoint].position.x){
				turnFish ();
			}
		}
	}

	private void updateNormalFishMovement() {
		if(!isTurning) {
			Vector2 fishPosition = new Vector2(transform.position.x, transform.position.y);
			Vector2 target = new Vector2(transform.position.x + 100f, transform.position.y);
			if(!isFacingRight) {
				target.x = transform.position.x -100f;
			}
			fishPosition = Vector2.MoveTowards(fishPosition, target, Mathf.Abs(speed * Time.deltaTime));

			transform.position = new Vector3(fishPosition.x, fishPosition.y, this.transform.position.z);
		}
	}

	private void updateMoveToTarget(Vector2 target, bool ignoreAwarenessRadius) {
		if(!isTurning) {
			if (ignoreAwarenessRadius || Vector2.Distance(target, getMouthPosition()) < awarenessRadius) {
				Vector2 fishMouthPosition = getMouthPosition();

				float frameSpeed = Mathf.Abs(speed * Time.deltaTime);
				Vector2 movement = getMovementFromSourceToTargetWithSpeed(fishMouthPosition, target, frameSpeed);

				transform.position = new Vector3(this.transform.position.x + movement.x, this.transform.position.y + movement.y, this.transform.position.z);
			} else {
				updateMovement(secondaryMovementType);
			}
		}
	}

	private void updateDirectionToTarget(Vector2 target, bool ignoreAwarenessRadius) { 
		if(!isTurning) {
			Vector2 fishMouthPosition = getMouthPosition();
			if (ignoreAwarenessRadius || Vector2.Distance(target, getMouthPosition()) < awarenessRadius) {
				if(!isFacingRight && fishMouthPosition.x + (Mathf.Abs(fishMouthPosition.x - transform.position.x) / 2) < target.x && (lastRotation + 1f) < Time.time) {
					turnFish();
					lastRotation = Time.time;
				} else if(isFacingRight && fishMouthPosition.x - (Mathf.Abs(fishMouthPosition.x - transform.position.x) / 2) > target.x && (lastRotation + 1f) < Time.time){
					turnFish ();
					lastRotation = Time.time;
				}
			} 
		}
	}

	private void updateRotation() {
		if(!isTurning) {
			Vector3 fishRotation = transform.rotation.eulerAngles;
			if(isFacingRight) {
				fishRotation.y = 180;
			} else {
				fishRotation.y = 0;
			}
			transform.rotation.eulerAngles.Set(fishRotation.x, fishRotation.y, fishRotation.z);
		} else {
			transform.Rotate(0,Time.deltaTime * rotationSpeed,0);
			Vector3 fishRotation = transform.rotation.eulerAngles;
			if((isFacingRight && (fishRotation.y % 360) < (Time.deltaTime * rotationSpeed)) || 
			   (!isFacingRight && Mathf.Abs(fishRotation.y % 360 - 180) < (Time.deltaTime * rotationSpeed))) {
				isFacingRight = !isFacingRight;
				isTurning = false;
				updateRotation();
			}
		}
	}

	private Vector2 getMouthPosition() {
		Vector2 result = new Vector2 (this.transform.position.x, this.transform.position.y);
		if(GetComponent<CircleCollider2D>() != null) {
			Vector2 offset = new Vector2(this.transform.localScale.x * GetComponent<CircleCollider2D>().offset.x, this.transform.localScale.y * GetComponent<CircleCollider2D>().offset.y);
			if(isFacingRight) {
				offset.x *= -1;
			}
			result += offset;
		} 
		return result;
	}

	private Vector2 getMovementFromSourceToTargetWithSpeed(Vector2 source, Vector2 target, float speed) {
		Vector2 getVector = target - source;
		float angleRadians = Mathf.Atan2(getVector.y, getVector.x);
		Vector2 movement = new Vector2(Mathf.Cos(angleRadians) * speed, Mathf.Sin(angleRadians) * speed); 

		return movement;
	}

	private void limitPosition () {
		transform.position = new Vector3(transform.position.x, Mathf.Clamp(transform.position.y, float.MinValue, LevelSettings.instance.airToWaterTransitionHeight), transform.position.z);
	}

	private void turnFish() {
		isTurning = true;
	}
}

public enum FishType { TEETH_FISH, NEUTRAL_FISH, WHITE_SHARK, EATABLE_FISH };
public enum MovementType { NONE, WAYPOINT_BASED, HORIZONTAL, FOLLOW_PLAYER, GUARD_STARTING_SPOT, FLEE_FROM_PLAYER  };