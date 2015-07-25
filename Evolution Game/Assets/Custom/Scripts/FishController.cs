using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FishController : MonoBehaviour, IEventReceiver {

	private const int ESTIMATED_FRAMES_PER_SECOND = 60;
	private const int SPEED_REWARDS_TO_UPGRADE = 6;
	private const int STRENGTH_REWARDS_TO_UPGRADE = 6;
	private const int LIBIDO_REWARDS_TO_MATE = 1;
	private const float MATING_DURATION = 6f;

	public Vector2 swimAcceleration = new Vector2 (4f,2f);
	public Vector2 dragCoefficient = new Vector2 (0.01f, 0.015f);
	public Vector2 maximumVelocity = new Vector2 (20f,10f); // units per second
	public GameObject eggNest; //the parent gameobject of all eggs

	private Animator animator;

	private Vector2 originalSwimAcceleration;
	private Vector2 originalMaximumVelocity;
	private Color originalRenderColor;
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
	private SpriteRenderer spriteRenderer;
	private bool isGameActive = true;

	//evolving states
	private bool isStrong = false;
	private bool isFast = false;

	// Use this for initialization
	void Start () {
		animator = this.GetComponent<Animator>();
		if (animator == null) {
			Debug.LogWarning("No animator attached to FishController on " + this.name);
		}
		if(eggNest == null) {
			Debug.LogWarning("No eggNest attached to FishController on " + this.name);
		} else {
			foreach (FishEgg egg in eggNest.GetComponentsInChildren<FishEgg>())
			{
					GameStatistics.incrementLives();
					eggs.Add(egg);
			}
		}
		if(GetComponent<SpriteRenderer>() != null) {
			spriteRenderer = GetComponent<SpriteRenderer>();
			originalRenderColor = spriteRenderer.color;
		} else {
			Debug.LogWarning("No SpriteRenderer attached to FishController on " + this.name);
		}

		if(GetComponent<AudioSource>() == null) {
			Debug.LogWarning("No AudioSource for eat sound attached to FishController on " + this.name);
		}

		if(GetComponent<Rigidbody2D>() != null) {
			originalMass = GetComponent<Rigidbody2D>().mass;
		} else {
			Debug.LogWarning("No Rigidbody2D attached to FishController on " + this.name);
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
			isFast = currentEgg.spawnsFastFish;
			isStrong = currentEgg.spawnsStrongFish;
			isReadyToMate = false;
			evolve();
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
			isFast = true;
		} 

		if(!isStrong && GameStatistics.getGatheredRewardsOfType(RewardType.STRENGTH) >= STRENGTH_REWARDS_TO_UPGRADE) {
			GameStatistics.decrementGatheredRewardsOfType(RewardType.STRENGTH, STRENGTH_REWARDS_TO_UPGRADE);
			isStrong = true;
		}
			
		if(GameStatistics.getGatheredRewardsOfType(RewardType.LIBIDO) >= LIBIDO_REWARDS_TO_MATE) {
			isReadyToMate = true;
		}

		updateEvolutionState();
		recalculateSpeeds();
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

	public void handleEvent(EventType eventType) {
		if(eventType == EventType.GAME_OVER || eventType == EventType.GAME_WON) {
			isGameActive = false;
		}
	}

	private void updateEvolutionState() {
		if(isFast) {
			spriteRenderer.color = new Color(1f,0.3f,0.3f,1f);
		} else {
			spriteRenderer.color = originalRenderColor;
		}

		if(isStrong) {
			GetComponent<Rigidbody2D>().mass = 800;
		} else {
			GetComponent<Rigidbody2D>().mass = originalMass;
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
			evolve();
		}
	}
}
