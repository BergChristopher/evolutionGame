using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FishController : MonoBehaviour, IEventReceiver {

	private const int ESTIMATED_FRAMES_PER_SECOND = 60;
	private const int SPEED_REWARDS_TO_UPGRADE = 2;
	private const int STRENGTH_REWARDS_TO_UPGRADE = 1;
	private const int LIBIDO_REWARDS_TO_MATE = 1;
	private const float MATING_DURATION = 6f;

	public Vector2 swimAcceleration = new Vector2 (4f,2f);
	public Vector2 dragCoefficient = new Vector2 (0.01f, 0.015f);
	public Vector2 maximumVelocity = new Vector2 (20f,10f); // units per second
	public GameObject eggNest; //the parent gameobject of all eggs

	private Animator animator;
	private FishtailController fishtailController;
	private Emitter heartEmitter;
	private Emitter bubbleEmitter;

	private Vector2 originalSwimAcceleration;
	private Vector2 originalMaximumVelocity;
	private float originalMass;
	private Vector2 velocity = Vector2.zero;
	private bool isFacingRight = true;
	private bool isReadyToEat = false;
	private bool isReadyToMate = false;
	private bool isCurrentlyMating = false;
	private float matingStartTime; 
	private EnemyFish matingPartner = null;
	private bool isAlive = true;
	private List<FishEgg> eggs = new List<FishEgg>();
	private bool isGameActive = true;

	//evolving states
	private bool isStrong = false;
	private bool isFast = false;

	// Use this for initialization
	void Start () {
		animator = GetComponent<Animator>();
		if (animator == null) {
			Debug.LogWarning("No animator attached to FishController on " + name);
		}
		if(eggNest == null) {
			Debug.LogWarning("No eggNest attached to FishController on " + name);
		} else {
			foreach (FishEgg egg in eggNest.GetComponentsInChildren<FishEgg>())
			{
					GameStatistics.incrementLives();
					eggs.Add(egg);
			}
		}

		if(GetComponent<AudioSource>() == null) {
			Debug.LogWarning("No AudioSource for eat sound attached to FishController on " + name);
		}

		if(GetComponent<Rigidbody2D>() != null) {
			originalMass = GetComponent<Rigidbody2D>().mass;
		} else {
			Debug.LogWarning("No Rigidbody2D attached to FishController on " + name);
		}

		int hearts = 0;
		int bubbles = 0;
		foreach(Emitter emitter in GetComponentsInChildren<Emitter>()) {
			if(emitter.emitterType == EmitterType.HEARTS) {
				hearts++;
				heartEmitter = emitter;
				heartEmitter.gameObject.SetActive(false);
			}
			if(emitter.emitterType == EmitterType.BUBBLES) {
				bubbles++;
				bubbleEmitter = emitter;
			}
		}
		if(hearts != 1) {
			Debug.LogError(hearts + " instead of one heartEmitter attached to FishControllers Children on " + name);
		}
		if(bubbles != 1) {
			Debug.LogError(bubbles + " instead of one bubbleEmitter attached to FishControllers Children on " + name);
		}

		if(GetComponentsInChildren<FishtailController>().Length == 1) {
			fishtailController = GetComponentInChildren<FishtailController>();
		} else {
			Debug.Log("Found less or more than one FishtailController on " + name);
		}

		originalMaximumVelocity = new Vector2(maximumVelocity.x, maximumVelocity.y);
		originalSwimAcceleration = new Vector2(swimAcceleration.x, swimAcceleration.y);

		EventManager.instance.addReceiver(EventType.GAME_OVER, this);
		EventManager.instance.addReceiver(EventType.GAME_WON, this);
	}
	
	// Update is called once per frame
	void Update () {
		if (isAlive && isGameActive) {
			if(!isCurrentlyMating) {
				updateMovement ();
				updateFacingDirection ();
				updateAnimation ();
			} else {
				updateMatingBehaviour();
			}
		}
	}

	public bool getIsReadyToEat() {
		return isReadyToEat;
	}

	public bool getIsReadyToMate() {
		return isReadyToMate;
	}

	public bool getIsStrong() {
		return isStrong;
	}

	public bool getIsFast() {
		return isFast;
	}

	public Vector2 getVelocity() {
		return velocity;
	}

	public void die() {
		GameStatistics.decrementLives();
		EventManager.instance.triggerEvent(EventType.PLAYER_DEATH);
		if(GameStatistics.getLives() < 0) {
			isAlive = false;
			Debug.Log ("trigger game over");
			EventManager.instance.triggerEvent(EventType.GAME_OVER);
			Destroy (this.gameObject);
		} else {
			FishEgg currentEgg = eggs[eggs.Count - 1]; 
			GameStatistics.clearRewardsAndCollectables();
			updateFastState(currentEgg.spawnsFastFish);
			updateStrongState(currentEgg.spawnsStrongFish);
			isReadyToMate = false;
			heartEmitter.gameObject.SetActive(false);
			this.transform.position = currentEgg.transform.position;
			eggs.Remove(currentEgg);
			Destroy(currentEgg.gameObject);
			isFacingRight = true;
			transform.eulerAngles = new Vector3 (this.transform.rotation.eulerAngles.x, 180, this.transform.rotation.eulerAngles.z);
		}
	}

	public void evolve() {
		if (!isFast && GameStatistics.getGatheredRewardsOfType(RewardType.SPEED) >= SPEED_REWARDS_TO_UPGRADE) {
			GameStatistics.decrementGatheredRewardsOfType(RewardType.SPEED, SPEED_REWARDS_TO_UPGRADE);
			updateFastState(true);
			AudioSource.PlayClipAtPoint((AudioClip)Resources.Load("evolve"), transform.position, 1.0F);
		} 

		if(!isStrong && GameStatistics.getGatheredRewardsOfType(RewardType.STRENGTH) >= STRENGTH_REWARDS_TO_UPGRADE) {
			GameStatistics.decrementGatheredRewardsOfType(RewardType.STRENGTH, STRENGTH_REWARDS_TO_UPGRADE);
			updateStrongState(true);
			AudioSource.PlayClipAtPoint((AudioClip)Resources.Load("evolve"), transform.position, 1.0F);
		}
			
		if(GameStatistics.getGatheredRewardsOfType(RewardType.LIBIDO) >= LIBIDO_REWARDS_TO_MATE) {
			isReadyToMate = true;
		}

		recalculateSpeeds();
	}

	public void attract(bool attract) {
		heartEmitter.gameObject.SetActive(attract);
	}

	public void mate(EnemyFish matingPartner) {
		if(!isCurrentlyMating){
			isCurrentlyMating = true;
			this.matingPartner = matingPartner;
			matingStartTime = Time.time;
		}
	}

	public void addEgg(FishEgg fishEgg) {
		if(fishEgg != null) {
			fishEgg.transform.parent = eggNest.transform;
			GameStatistics.incrementLives();
			eggs.Add(fishEgg);
		} else {
			Debug.LogError("Null fishEgg added to " + name);
		}
	}

	public void removeEgg(FishEgg fishEgg) {
		if(fishEgg != null) {
			GameStatistics.decrementLives();
			eggs.Remove(fishEgg);
			Destroy(fishEgg.gameObject);
		} else {
			Debug.LogError("Null fishEgg in removeEgg at " + name);
		}
	}

	public void handleEvent(EventType eventType) {
		if(eventType == EventType.GAME_OVER || eventType == EventType.GAME_WON) {
			isGameActive = false;
		}
	}

	private void updateFacingDirection() {
		if ((Input.GetAxis ("Horizontal") < 0 && isFacingRight) || (Input.GetAxis ("Horizontal") > 0 && !isFacingRight)) {
			isFacingRight = !isFacingRight;
			if (isFacingRight) {
				transform.eulerAngles = new Vector3 (this.transform.rotation.eulerAngles.x, 180, this.transform.rotation.eulerAngles.z);
			} else {
				transform.eulerAngles = new Vector3 (this.transform.rotation.eulerAngles.x, 0, this.transform.rotation.eulerAngles.z);
			}
		}
	}

	private void updateMovement() {
		Vector2 previousVelocity = Vector2.zero;
		previousVelocity.x = velocity.x; 
		previousVelocity.y = velocity.y;

		velocity.x = previousVelocity.x + (Input.GetAxis ("Horizontal") * swimAcceleration.x * Time.deltaTime * ESTIMATED_FRAMES_PER_SECOND); //v=v0+a*(t-t0)
		velocity.y = previousVelocity.y + (Input.GetAxis ("Vertical") * swimAcceleration.y * Time.deltaTime * ESTIMATED_FRAMES_PER_SECOND); //v=v0+a*(t-t0)
		
		float horizontalDrag = (1 - Mathf.Abs(Input.GetAxis ("Horizontal"))) * ((dragCoefficient.x * (velocity.x * velocity.x)) + (dragCoefficient.x));
		float verticalDrag = (1 - Mathf.Abs(Input.GetAxis ("Vertical"))) * ((dragCoefficient.y * (velocity.y * velocity.y)) + (dragCoefficient.y));

		if (velocity.x > horizontalDrag) {
			velocity.x -= horizontalDrag; //physical drag applied
		} else if (velocity.x < -horizontalDrag) {
			velocity.x += horizontalDrag; //physical drag applied
		} else {
			velocity.x = 0; //prevent too small velocities
		}

		if (velocity.y > verticalDrag) {
			velocity.y -= verticalDrag; //physical drag applied
		} else if (velocity.y < -verticalDrag) {
			velocity.y += verticalDrag; //physical drag applied
		} else {
			velocity.y = 0; //prevent too small velocities
		}
		
		velocity.x = Mathf.Clamp (velocity.x, (maximumVelocity.x * -1), maximumVelocity.x); //clamp between max and min velocity
		velocity.y = Mathf.Clamp (velocity.y, (maximumVelocity.y * -1), maximumVelocity.y); //clamp between max and min velocity

		this.transform.position = new Vector3(this.transform.position.x + (velocity.x * Time.deltaTime), 
		                                      Mathf.Clamp((this.transform.position.y + (velocity.y * Time.deltaTime)), float.MinValue, LevelSettings.instance.airToWaterTransitionHeight), 
		                                      this.transform.position.z);
	}

	private void updateAnimation() {
		if (Input.GetKeyDown (KeyCode.Space) && !isReadyToEat) {
			animator.SetInteger("Action",1); //Action 1 indicates a transition to the eat animation
			isReadyToEat = true;
			recalculateSpeeds();
		}
		if (Input.GetKeyUp (KeyCode.Space) && isReadyToEat) {
			animator.SetInteger("Action",0); //Action 0 indicates a transition to the normal animation
			isReadyToEat = false;
			recalculateSpeeds();
		}
		/*if (isReadyToMate) {
			animator.SetInteger("Horny", 1); //Action 1 indicates a transition to the horny state
		} else {
			animator.SetInteger("Horny", 0); //Action 0 indicates a transition to the not horny state
		}*/
	}

	private void recalculateSpeeds() {
		swimAcceleration = originalSwimAcceleration;
		maximumVelocity = originalMaximumVelocity;
		if(isFast) {
			swimAcceleration *= 2f;
			maximumVelocity *= 2f;
		}
		if(isReadyToEat) {
			swimAcceleration *= 0.5f;
			maximumVelocity *= 0.5f;
		}
	}

	private void updateMatingBehaviour () {
		transform.Rotate(new Vector3(0, 1, 0));
		if(Time.time > matingStartTime + MATING_DURATION) {
			isCurrentlyMating = false;
			transform.eulerAngles = new Vector3(transform.eulerAngles.x, 180 ,transform.eulerAngles.z);
			matingPartner.layEggs();
			matingPartner = null;
			GameStatistics.decrementGatheredRewardsOfType(RewardType.LIBIDO, LIBIDO_REWARDS_TO_MATE);
			isReadyToMate = false;
			heartEmitter.gameObject.SetActive(false);
			evolve();
		}
	}

	private void updateStrongState(bool strong) {
		isStrong = strong;
		if (strong) {
			GetComponent<Rigidbody2D>().mass = 800;
			bubbleEmitter.transform.localPosition = new Vector3(-3f, 0.1f, 0f);
			heartEmitter.transform.localPosition = new Vector3(-3f, 0.1f, 0f);
		} else {
			GetComponent<Rigidbody2D>().mass = originalMass;
			bubbleEmitter.transform.localPosition = new Vector3(-2.4f, 0.1f, 0f);
			heartEmitter.transform.localPosition = new Vector3(-2.4f, 0.1f, 0f);
		}
		animator.SetBool ("isStrong", isStrong);
	}

	private void updateFastState(bool fast) {
		isFast = fast;
		if(fishtailController != null) {
			fishtailController.setIsFast(isFast);
			fishtailController.updateCollider();
		}
	}
}
