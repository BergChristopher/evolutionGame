using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyFish : MonoBehaviour, IEventReceiver {

	public FishCategoryType fishCategoryType = FishCategoryType.PLAYER_LIKE;
	public FishType fishType = FishType.AGGRESSIVE_INTERACTING;
	public MovementType movementType = MovementType.HORIZONTAL;
	public MovementType secondaryMovementType = MovementType.NONE;
	public RewardType rewardType = RewardType.NONE; 
	public bool isMoving = true;
	public float speed = 3;
	public float rotationSpeed = 50;
	public float awarenessRadius = 15f; 

	private GameObject player = null;
	private Animator animator;
	private Emitter heartEmitter;
	private bool isFacingRight = true;
	private bool isTurning = false;
	private bool canBeEatenByPlayer = false;
	private float currentMaxSpeed;
	private Vector3 lastPosition; //position in last frame
	private bool isGameActive = true;
	private bool onTriggerStayWasAlreadyExecutedThisFrame = false;

	//only for FishType that is ready for mating
	private bool isMating = false;
	private bool isReadyToLayEgg = false;

	//waypoint based movement variables
	public List<Transform> waypoints = new List<Transform>(); 
	private int currentWaypoint = 0;

	//horizontal movement variables 
	private float lastCollission = 0; 

	//follow player based movement 
	private float lastRotation = 0;

	//guard starting spot variables
	public float speedMultiplierIfPlayerIsCloserToGuardedSpot = 1f;
	private Vector2 guardedSpot = Vector2.zero;

	//random movement variables
	public Vector2 timeToChangeRandomMovement = new Vector2(5f, 2f); //in s
	private Vector2 lastDirectionChange = Vector2.zero;
	private Vector2 currentDirection = Vector2.zero;

	//swarm movement
	public Vector2 separationRadiusAndImpact = new Vector2(3f,10f); //avoid collision
	public Vector2 alignmentRadiusAndImpact = new Vector2(6f,5f); //align direction
	public Vector2 cohesionRadiusAndImpact = new Vector2(10f,2f); //attract other fish of same type to join swarm
	private static Dictionary<FishCategoryType, List<EnemyFish>> fishCategoryTypeToListOfEnemyFish = new Dictionary<FishCategoryType, List<EnemyFish>>(); 

	void Start () {
		animator = this.GetComponent<Animator>();
		currentMaxSpeed = speed;
		lastPosition = transform.position;
		isFacingRight = Mathf.Abs((this.transform.rotation.eulerAngles.y % 360) - 180) <= 45;  
		guardedSpot = new Vector2(this.transform.position.x, this.transform.position.y);

		if(waypoints.Count < 2 && movementType.Equals(MovementType.WAYPOINT_BASED)) {
			Debug.LogWarning("Your fish " + name + " has less than 2 waypoints assigned.");
		}
		if(currentMaxSpeed < 0) {
			Debug.LogWarning("Your fish " + name + " has a speed less than 0, it will be converted to " + Mathf.Abs(currentMaxSpeed) + ".");
		}

		GameObject[] possiblePlayers = GameObject.FindGameObjectsWithTag("Player");
		player = null;
		foreach (GameObject possiblePlayer in possiblePlayers) {
			if(possiblePlayer.GetComponent<FishController>() != null) {
				player = possiblePlayer;
				break;
			}
		}
		if(player == null) {
			Debug.LogError("Your fish " + name + " cannot find the player.");
		}

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

		//check colliders, one circle needed if fish should be able to eat something
		CircleCollider2D[] circleCollider2D = GetComponents<CircleCollider2D> ();
		if(circleCollider2D.Length > 1) {
			Debug.LogWarning(this.name + " has more than one Circle Collider attached, which could be the mouth.");
		} else if(circleCollider2D == null) {
			Debug.LogWarning(this.name + " has no Circle Collider attached, which could be the mouth.");
		} else if(circleCollider2D.Length == 1 && !circleCollider2D[0].isTrigger){
			Debug.LogWarning(this.name + " has one Circle Collider attached, which could be the mouth, but it is not set as trigger.");
		}

		//fish should have a Rigidbody 2D to interact with other physics
		if(GetComponent<Rigidbody2D>() == null) {
			Debug.LogWarning(this.name + " has no Rigidbody2D attached and can therefore not interact with other objects correctly.");
		}

		//fish should have an edge collider to detect collisions with walls
		if(GetComponent<EdgeCollider2D>() == null || GetComponent<EdgeCollider2D>().isTrigger) {
			Debug.LogWarning(this.name + " has no non trigger EdgeCollider2D attached and might therefore not respond to environmental collissions correctly.");
		}

		int hearts = 0;
		foreach(Emitter emitter in GetComponentsInChildren<Emitter>()) {
			if(emitter.emitterType == EmitterType.HEARTS) {
				hearts++;
				heartEmitter = emitter;
				heartEmitter.gameObject.SetActive(false);
			}
		}
		if(hearts != 1 && fishType == FishType.READY_TO_MATE_FISH) {
			Debug.LogError(hearts + " instead of one heartEmitter attached to EnemyFish's Children on " + name);
		}

		addMeToDictionaryOfFish();
		EventManager.instance.addReceiver(EventType.GAME_OVER, this);
		EventManager.instance.addReceiver(EventType.GAME_WON, this);
	}
	
	void Update () {
		if(isGameActive) {
			onTriggerStayWasAlreadyExecutedThisFrame = false;
			updateMating();
			if(!isMating) {
				updateMovement(movementType);
			}
		}
	}

	void OnDestroy() {
		removeMeFromDictionary();
	}

	void OnTriggerStay2D(Collider2D enteringCollider) {
		if(!onTriggerStayWasAlreadyExecutedThisFrame) {
			//eat player
			if(fishType.Equals(FishType.AGGRESSIVE_NON_INTERACTING) || fishType.Equals(FishType.AGGRESSIVE_INTERACTING)) {
				if (enteringCollider.gameObject.tag == "Player") {
					FishController fish = enteringCollider.GetComponent<FishController>();
					if(fish != null) {
						GameStatistics.addDeathByFish(this.fishType);
						GetComponent<AudioSource>().Play();
						fish.die();
						onTriggerStayWasAlreadyExecutedThisFrame = true;
					}
				}
			}
			//eat fisheggs if they fall into your mouth
			if(fishType.Equals(FishType.AGGRESSIVE_NON_INTERACTING) || fishType.Equals(FishType.AGGRESSIVE_INTERACTING)) {
				if (enteringCollider.gameObject.GetComponent<FishEgg>() != null) {
					GetComponent<AudioSource>().Play();
					player.GetComponent<FishController>().removeEgg(enteringCollider.gameObject.GetComponent<FishEgg>());
				}
			}
			//be eaten by player
			if (canBeEatenByPlayer && enteringCollider.gameObject.tag == "Player" && enteringCollider.GetType().Equals(typeof(CircleCollider2D))) {
				FishController fish = enteringCollider.GetComponent<FishController>();
				if(fish != null && fish.getIsReadyToEat()) {
					GameStatistics.addCollectable(CollectableType.ENEMY_FISH);
					GameStatistics.addReward(rewardType);
					fish.evolve();
					fish.GetComponent<AudioSource>().Play();
					GameObject nom = (GameObject) Instantiate(Resources.Load("nom"), transform.position, new Quaternion(0f,0f,0f,0f));
					Destroy (nom, 0.2F);
					handleMyDeath();
					onTriggerStayWasAlreadyExecutedThisFrame = true;
				}
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

	public void handleEvent(EventType eventType) {
		if(eventType == EventType.GAME_OVER || eventType == EventType.GAME_WON) {
			isGameActive = false;
		}
		if(eventType == EventType.PLAYER_DEATH) {
			stopMating(false);
			fishCategoryTypeToListOfEnemyFish.Clear();
		}
	}
	
	public void layEggs() {
		if(isMating) {
			isReadyToLayEgg = true;
		}
	}

	private void updateMovement(MovementType currentMovementType)
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
				if (animator != null) {
					animator.SetBool("Attacking",true);
				}
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
				if (animator != null) {
					animator.SetBool("Attacking",false);
				}
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

			if(movement.magnitude == 0 && secondaryMovementType != MovementType.SWARM) {
				updateMovement(secondaryMovementType);
			} else if(isFacingRight && movement.x >= 0 || !isFacingRight && movement.x <= 0) {
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
		if(movementType.Equals(MovementType.FOLLOW_PLAYER) && secondaryMovementType.Equals(MovementType.GUARD_STARTING_SPOT) && player != null) {
			Vector2 playerPos = new Vector2(player.transform.position.x, player.transform.position.y);
			Vector2 mouthPos = getMouthPosition();
			bool isPlayerCloserToSpot = Mathf.Abs(Vector2.Distance(playerPos, guardedSpot)) < Mathf.Abs(Vector2.Distance(mouthPos, guardedSpot));
			currentMaxSpeed = speed;
			if(isPlayerCloserToSpot) {
				currentMaxSpeed *= speedMultiplierIfPlayerIsCloserToGuardedSpot;
			}
		}
	}

	private void addMeToDictionaryOfFish() {
		//assert this object is not yet in the list
		if(fishCategoryTypeToListOfEnemyFish.ContainsKey(fishCategoryType)) {
			foreach (EnemyFish enemyFish in fishCategoryTypeToListOfEnemyFish[fishCategoryType]) {
				if(enemyFish.gameObject.Equals(this.gameObject)) {
					return;
				}
			}
		}

		if(!fishCategoryTypeToListOfEnemyFish.ContainsKey(fishCategoryType)) {
			fishCategoryTypeToListOfEnemyFish.Add(fishCategoryType, new List<EnemyFish>());
        }
		fishCategoryTypeToListOfEnemyFish[fishCategoryType].Add(this);
	}

	private void removeMeFromDictionary() {
		//assert this object is not yet in the list
		if(fishCategoryTypeToListOfEnemyFish.ContainsKey(fishCategoryType)) {
			fishCategoryTypeToListOfEnemyFish[fishCategoryType].Remove(this);
		}
	}

	private List<EnemyFish> getSameFishTypeInRadius(float radius) {
		List<EnemyFish> result = new List<EnemyFish>();

		if(fishCategoryTypeToListOfEnemyFish.ContainsKey(fishCategoryType)) {
			foreach (EnemyFish enemyFish in fishCategoryTypeToListOfEnemyFish[fishCategoryType]) {
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

	private void handleMyDeath()
	{
		if(RewardType.NONE == rewardType) {
			Destroy (this.gameObject);
		} else {
			respawnMe();
		}
	}

	private void respawnMe() {
		Vector2 respawn1 = new Vector2 (LevelSettings.instance.respawnPoint1.position.x, LevelSettings.instance.respawnPoint1.position.y);
		Vector2 respawn2 = new Vector2 (LevelSettings.instance.respawnPoint2.position.x, LevelSettings.instance.respawnPoint1.position.y);
		Vector2 playerPosition = new Vector2(player.transform.position.x, player.transform.position.y);
		if(Vector2.Distance (playerPosition, respawn1) < Vector2.Distance(playerPosition, respawn2)) {
			transform.position = new Vector3(respawn2.x, respawn2.y, transform.position.z);
		} else {
			transform.position = new Vector3(respawn1.x, respawn1.y, transform.position.z);
		}
	}

	private void updateMating() {
		if(player != null) {
			if(fishType.Equals(FishType.READY_TO_MATE_FISH)) {
				Vector2 thisPosition = getMouthPosition();
				Vector2 playerPosition = new Vector2(player.transform.position.x, player.transform.position.y);
				FishController fishController = player.GetComponent<FishController>();
				if(fishController != null && fishController.getIsReadyToMate() && 
				   Vector2.Distance(thisPosition, playerPosition) < awarenessRadius) {
					heartEmitter.gameObject.SetActive(true);
					fishController.attract(true);
					if(!isMating && Input.GetKeyDown(KeyCode.Return)) {
						fishController.mate(this);
						isMating = true;
					}
				} else {
					heartEmitter.gameObject.SetActive(false);
					fishController.attract(false);
				}
			}
			if(isMating && !isReadyToLayEgg) {
				transform.Rotate(Vector3.down);
			}
			if(isMating && isReadyToLayEgg) {
				stopMating(true);
			}
		}
	}

	private void stopMating(bool success) {
		isMating = false;
		isReadyToLayEgg = false;
		heartEmitter.gameObject.SetActive(false);
		FishController fishController = player.GetComponent<FishController>();
		if(fishController != null) {
			fishController.attract(false);
		}
		if(transform.eulerAngles.y % 360 > 90 && transform.eulerAngles.y % 360 < 270) {
			transform.eulerAngles = new Vector3(transform.eulerAngles.x, 180 ,transform.eulerAngles.z);
		} else {
			transform.eulerAngles = new Vector3(transform.eulerAngles.x, 0 ,transform.eulerAngles.z);
		}

		if(success) {
			GameObject fishEgg = (GameObject) Instantiate(Resources.Load("fishEgg"), transform.position, transform.rotation);
			AudioSource.PlayClipAtPoint((AudioClip)Resources.Load("egg"), transform.position, 1.0F);
			FishEgg fishEggScript = fishEgg.GetComponent<FishEgg>();
			if(fishEggScript != null) {
				if(fishController.getIsStrong()) {
					fishEggScript.spawnsStrongFish = true;
				}
				if(fishController.getIsFast()) {
					fishEggScript.spawnsFastFish = true;
				}
				fishController.addEgg (fishEggScript);
			} else {
				Debug.LogError("Prefab fishEgg has no FishEgg script attached");
			}
		}
	}
}
