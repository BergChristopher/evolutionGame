﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FishController : MonoBehaviour {

	private const int ESTIMATED_FRAMES_PER_SECOND = 60;
	private const int SPEED_REWARDS_TO_UPGRADE = 6;
	private const int STRENGTH_REWARDS_TO_UPGRADE = 6;
	private const int LIBIDO_REWARDS_TO_MATE = 1;
	private const float MATING_DURATION = 2f;


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
	private List<GameObject> eggs = new List<GameObject>();
	private SpriteRenderer spriteRenderer;

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
			foreach (Transform child in eggNest.transform)
			{
				if(child.tag == "Egg") {
					GameStatistics.incrementLives();
					eggs.Add(child.gameObject);
				}
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
	}
	
	// Update is called once per frame
	void Update () {
		if (isAlive) {
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

	public Vector2 getVelocity() {
		return velocity;
	}

	public void die() {
		GameStatistics.decrementLives();
		EventManager.instance.triggerEvent(EventType.PLAYER_DEATH);
		if(GameStatistics.getLives() < 0) {
			isAlive = false;
			Destroy (this.gameObject);
		} else {
			GameObject currentEgg = eggs[eggs.Count - 1]; 
			GameStatistics.clearRewardsAndCollectables();
			isFast = false;
			isStrong = false;
			FishEgg currentEggScript = currentEgg.GetComponent<FishEgg>();
			if(currentEggScript != null) {
				isFast = currentEggScript.spawnsFastFish;
				isStrong = currentEggScript.spawnsStrongFish;
			}
			evolve();
			this.transform.position = currentEgg.transform.position;
			eggs.Remove(currentEgg);
			Destroy(currentEgg);
			isFacingRight = true;
			transform.eulerAngles = new Vector3 (this.transform.rotation.eulerAngles.x, 180, this.transform.rotation.eulerAngles.z);
		}

	}

	public void evolve() {
		if (isFast || GameStatistics.getGatheredRewardsOfType(RewardType.SPEED) >= SPEED_REWARDS_TO_UPGRADE) {
			spriteRenderer.color = new Color(1f,0.3f,0.3f,1f);
			isFast = true;
		} else {
			spriteRenderer.color = originalRenderColor;
			isFast = false;
		}
		if(isStrong || GameStatistics.getGatheredRewardsOfType(RewardType.STRENGTH) >= STRENGTH_REWARDS_TO_UPGRADE) {
			isStrong = true;
			GetComponent<Rigidbody2D>().mass = 800;
		} else {
			GetComponent<Rigidbody2D>().mass = originalMass;
		}
		if(GameStatistics.getGatheredRewardsOfType(RewardType.LIBIDO) >= LIBIDO_REWARDS_TO_MATE) {
			isReadyToMate = true;
		}
		recalculateSpeeds();
	}

	public void mate(EnemyFish matingPartner) {
		if(!isCurrentlyMating){
			isCurrentlyMating = true;
			this.matingPartner = matingPartner;
			matingStartTime = Time.time;
		}
	}

	public void addEgg(GameObject fishEgg) {
		FishEgg fishEggScript = fishEgg.GetComponent<FishEgg>();
		if(fishEggScript != null) {
			fishEggScript.transform.parent = eggNest.transform;
			GameStatistics.incrementLives();
			eggs.Add(fishEgg);
		} else {
			Debug.LogError("Invalid fishEgg added to " + name);
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
		}
	}
}
