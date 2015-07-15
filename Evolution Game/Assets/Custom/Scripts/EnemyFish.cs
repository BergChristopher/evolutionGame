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
	public float awarenessRadius = 15f; 
	
	private bool isFacingRight = true;
	private bool isTurning = false;
	private bool canBeEatenByPlayer = false;
	private float currentMaxSpeed;
	private Vector3 lastPosition; //position in last frame

	//waypoint based movement variables
	public List<Transform> waypoints = new List<Transform>(); 
	private int currentWaypoint = 0;

	//horizontal movement variables 
	private float lastCollission = 0; 

	//follow player based movement 
	private GameObject player = null;
	private float lastRotation = 0;

	//guard starting spot variables
	public float speedMultiplierIfPlayerIsCloserToGuardedSpot = 1f;
	private Vector2 guardedSpot = Vector2.zero;

	//random movement variables
	public Vector2 timeToChangeRandomMovement = new Vector2(5f, 2f); //in s
	private Vector2 lastDirectionChange = Vector2.zero;
	private Vector2 currentDirection = Vector2.zero;

	//swarm movement
	public Vector2 separationRadiusAndImpact = new Vector2(2f,10f); //avoid collision
	public Vector2 alignmentRadiusAndImpact = new Vector2(5f,5f); //align direction
	public Vector2 cohesionRadiusAndImpact = new Vector2(10f,2f); //attract other fish of same type to join swarm
	private static Dictionary<FishType, List<EnemyFish>> fishTypeToListOfEnemyFish = new Dictionary<FishType, List<EnemyFish>>(); 

	void Start () {
		currentMaxSpeed = speed;
		lastPosition = transform.position;
		if(waypoints.Count < 2 && movementType.Equals(MovementType.WAYPOINT_BASED)) {
			Debug.LogWarning("Your fish " + name + " has less than 2 waypoints assigned.");
		}
		if(currentMaxSpeed < 0) {
			Debug.LogWarning("Your fish " + name + " has a speed less than 0, it will be converted to " + Mathf.Abs(currentMaxSpeed) + ".");
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

		addMeToDictionary();
	}
	
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
				fish.GetComponent<AudioSource>().Play();
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
			lastPosition = transform.position;
			determineNewSpeed();
			if (currentMovementType.Equals (MovementType.WAYPOINT_BASED)) {
				updateWaypointBasedFishMovement ();
			}
			else if (currentMovementType.Equals (MovementType.HORIZONTAL)) {
				updateNormalFishMovement ();
			}
			else if (currentMovementType.Equals (MovementType.FOLLOW_PLAYER)) {
				if(player == null) {
					Debug.LogWarning("No player attached to " + name );
				} else {
					updateMoveToTarget (new Vector2 (player.transform.position.x, player.transform.position.y), false);
				}
			}
			else if (currentMovementType.Equals (MovementType.FLEE_FROM_PLAYER)) {
				if(player == null) {
					Debug.LogWarning("No player attached to " + name );
				} else {
					float newXTargetPosition = transform.position.x + (transform.position.x - player.transform.position.x);
					float newYTargetPosition = transform.position.y + (transform.position.y - player.transform.position.y);
					updateMoveToTarget (new Vector2 (newXTargetPosition, newYTargetPosition), false);
				}
			}
			else if (currentMovementType.Equals (MovementType.GUARD_STARTING_SPOT)) {
				updateMoveToTarget (guardedSpot, true, false);
			}
			else if (currentMovementType.Equals (MovementType.RANDOM)) {
				updateRandomMovement();
			}
			else if (currentMovementType.Equals (MovementType.SWARM)) {
				updateSwarmMovement();
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
				fishPosition = Vector2.MoveTowards(fishPosition, target, Mathf.Abs(currentMaxSpeed * Time.deltaTime));

				if(fishPosition.x == target.x && fishPosition.y == target.y) {
					currentWaypoint = (currentWaypoint + 1) % waypoints.Count;
				}

				transform.position = new Vector3(fishPosition.x, fishPosition.y, this.transform.position.z);
			}

			updateWaypointBasedDirection();
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
			fishPosition = Vector2.MoveTowards(fishPosition, target, Mathf.Abs(currentMaxSpeed * Time.deltaTime));

			transform.position = new Vector3(fishPosition.x, fishPosition.y, this.transform.position.z);
		}
	}

	private void updateMoveToTarget(Vector2 target, bool ignoreAwarenessRadius, bool overShootMovement = true) {
		if(!isTurning) {
			Vector2 mouthPosition = getMouthPosition();
			if (ignoreAwarenessRadius || Vector2.Distance(target, mouthPosition) < awarenessRadius) {
				float frameSpeed = Mathf.Abs(currentMaxSpeed * Time.deltaTime);
				Vector2 movement = Vector2.zero;
				if(!overShootMovement && Vector2.Distance (target, mouthPosition) <= frameSpeed) {
					movement = target - mouthPosition;
				} else {
					movement = getMovementFromSourceToTargetWithSpeed(mouthPosition, target, frameSpeed);
				}

				if(isFacingRight && movement.x >= 0 || !isFacingRight && movement.x <= 0) {
					transform.position = new Vector3(this.transform.position.x + movement.x, this.transform.position.y + movement.y, this.transform.position.z);
				} else {
					updateDirectionToTarget(target, ignoreAwarenessRadius);
				}
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

	private void updateRandomMovement() {
		if(Time.time > lastDirectionChange.x + timeToChangeRandomMovement.x) {
			currentDirection = new Vector2(Random.Range(-currentMaxSpeed,currentMaxSpeed), currentDirection.y);
			lastDirectionChange = new Vector2(Time.time, lastDirectionChange.y);
			float newXTargetPosition = transform.position.x + currentDirection.x;
			float newYTargetPosition = transform.position.y + currentDirection.y;
			updateDirectionToTarget(new Vector2(newXTargetPosition, newYTargetPosition), true);
		}
		if(Time.time > lastDirectionChange.y + timeToChangeRandomMovement.y) {
			currentDirection = new Vector2(currentDirection.x, Random.Range(-currentMaxSpeed,currentMaxSpeed));
			lastDirectionChange = new Vector2(lastDirectionChange.x, Time.time);
		}
		if(!isTurning) {
			Vector2 newPosition = new Vector2(transform.position.x + (currentDirection.x * Time.deltaTime), 
		    	                              transform.position.y + (currentDirection.y * Time.deltaTime));
			transform.position = new Vector3(newPosition.x, newPosition.y, this.transform.position.z);
		}
	}

	private void updateSwarmMovement() {
		if(!isTurning) {
			Vector2 separation = getSwarmSeparation();
			Vector2 alignment = getSwarmAlignment();
			Vector2 cohesion = getSwarmCohesion();

			Vector2 movement = (separation + alignment + cohesion).normalized * speed * Time.deltaTime;
			Vector3 desiredPosition = new Vector3(this.transform.position.x + movement.x, this.transform.position.y + movement.y, this.transform.position.z);

			if(isFacingRight && movement.x >= 0 || !isFacingRight && movement.x <= 0) {
				transform.position = desiredPosition;
			} else {
				updateDirectionToTarget(new Vector2(desiredPosition.x, desiredPosition.y), true);
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
		float angleRadians = getAngleInRadiansFromAToB(source, target);
		Vector2 movement = new Vector2(Mathf.Cos(angleRadians) * speed, Mathf.Sin(angleRadians) * speed); 

		return movement;
	}

	private float getAngleInRadiansFromAToB(Vector2 a, Vector2 b) {
		Vector2 getVector = b - a;
		float angleRadians = Mathf.Atan2(getVector.y, getVector.x);

		return angleRadians;
	}

	private void limitPosition () {
		transform.position = new Vector3(transform.position.x, Mathf.Clamp(transform.position.y, float.MinValue, LevelSettings.instance.airToWaterTransitionHeight), transform.position.z);
	}

	private void turnFish() {
		isTurning = true;
	}

	private void determineNewSpeed () {
		//increase speed if player is closer to guarding spot than the follower
		if(movementType.Equals(MovementType.FOLLOW_PLAYER) && secondaryMovementType.Equals(MovementType.GUARD_STARTING_SPOT)) {
			Vector2 playerPos = new Vector2(player.transform.position.x, player.transform.position.y);
			Vector2 mouthPos = getMouthPosition();
			bool isPlayerCloserToSpot = Mathf.Abs(Vector2.Distance(playerPos, guardedSpot)) < Mathf.Abs(Vector2.Distance(mouthPos, guardedSpot));
			currentMaxSpeed = speed;
			if(isPlayerCloserToSpot) {
				currentMaxSpeed *= speedMultiplierIfPlayerIsCloserToGuardedSpot;
			}
		}
	}

	private void addMeToDictionary() {
		//assert this object is not yet in the list
		if(fishTypeToListOfEnemyFish.ContainsKey(fishType)) {
			foreach (EnemyFish enemyFish in fishTypeToListOfEnemyFish[fishType]) {
				if(enemyFish.gameObject.Equals(this.gameObject)) {
					return;
				}
			}
		}

		if(!fishTypeToListOfEnemyFish.ContainsKey(fishType)) {
			fishTypeToListOfEnemyFish.Add(fishType, new List<EnemyFish>());
        }
		fishTypeToListOfEnemyFish[fishType].Add(this);
	}

	private List<EnemyFish> getSameFishTypeInRadius(float radius) {
		List<EnemyFish> result = new List<EnemyFish>();

		if(fishTypeToListOfEnemyFish.ContainsKey(fishType)) {
			foreach (EnemyFish enemyFish in fishTypeToListOfEnemyFish[fishType]) {
				Vector2 enemyPosition = new Vector2 (enemyFish.transform.position.x, enemyFish.transform.position.y);
				if(!System.Object.ReferenceEquals(enemyFish, this) && Mathf.Abs(Vector2.Distance(getMouthPosition(), enemyPosition)) <= radius) {
					result.Add(enemyFish);
				}
			}
		}

		return result;
	}

	private Vector2 getSwarmSeparation() {
		Vector2 separation = Vector2.zero;
		List<EnemyFish> sameFishInRadius = getSameFishTypeInRadius(separationRadiusAndImpact.x);

		foreach(EnemyFish enemyFish in sameFishInRadius) {
			Vector2 separationForOneFish = getMouthPosition () - new Vector2(enemyFish.transform.position.x, enemyFish.transform.position.y);
			separation += separationForOneFish.normalized;
		}

		separation = separation.normalized * separationRadiusAndImpact.y;
		//Debug.Log ("Separation= " + separation + " for " + name + " with " + sameFishInRadius.Count + " fish in " + separationRadiusAndImpact.x + " distance.");
		return separation;
	}

	private Vector2 getSwarmAlignment() {
		Vector2 alignment = Vector2.zero;
		List<EnemyFish> sameFishInRadius = getSameFishTypeInRadius(alignmentRadiusAndImpact.x);
		
		foreach(EnemyFish enemyFish in sameFishInRadius) {
			alignment += enemyFish.getPreviousMovement().normalized;
		}
		
		alignment = alignment.normalized * alignmentRadiusAndImpact.y;
		//Debug.Log ("Alignment= " + alignment + " for " + name + " with " + sameFishInRadius.Count + " fish in " + alignmentRadiusAndImpact.x + " distance.");
		return alignment;
	}

	private Vector2 getSwarmCohesion() {
		Vector2 cohesion = Vector2.zero;
		List<EnemyFish> sameFishInRadius = getSameFishTypeInRadius(cohesionRadiusAndImpact.x);
		
		foreach(EnemyFish enemyFish in sameFishInRadius) {
			Vector2 cohesionForOneFish = new Vector2(enemyFish.transform.position.x, enemyFish.transform.position.y) - getMouthPosition ();
			cohesion += cohesionForOneFish.normalized;
			//Debug.Log(name + " sees " + enemyFish.name + " with movement = " + cohesionForOneFish);
		}
		
		cohesion = cohesion.normalized * cohesionRadiusAndImpact.y;
		//Debug.Log ("Cohesion= " + cohesion + " for " + name + " with " + sameFishInRadius.Count + " fish in " + cohesionRadiusAndImpact.x + " distance.");
		return cohesion;
	}

	public Vector2 getPreviousMovement() {
		return new Vector2(transform.position.x - lastPosition.x, transform.position.y - lastPosition.y );
	}
}

public enum FishType { TEETH_FISH, NEUTRAL_FISH, WHITE_SHARK, EATABLE_FISH };
public enum MovementType { NONE, WAYPOINT_BASED, HORIZONTAL, FOLLOW_PLAYER, GUARD_STARTING_SPOT, FLEE_FROM_PLAYER, RANDOM, SWARM };