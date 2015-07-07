using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FishController : MonoBehaviour {

	private const int ESTIMATED_FRAMES_PER_SECOND = 60;

	public Vector2 swimAcceleration = new Vector2 (4f,2f);
	public Vector2 dragCoefficient = new Vector2 (0.01f, 0.015f);
	public Vector2 maximumVelocity = new Vector2 (20f,10f); // units per second
	public GameObject eggNest; //the parent gameobject of all eggs

	private Animator animator;

	private Vector2 originalSwimAcceleration;
	private Vector2 originalMaximumVelocity;
	private Vector2 velocity = Vector2.zero;
	private bool isFacingRight = true;
	private bool isReadyToEat = false;
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
		} else {
			Debug.LogWarning("No SpriteRenderer attached to FishController on " + this.name);
		}

		originalMaximumVelocity = new Vector2(maximumVelocity.x, maximumVelocity.y);
		originalSwimAcceleration = new Vector2(swimAcceleration.x, swimAcceleration.y);
	}
	
	// Update is called once per frame
	void Update () {
		if (isAlive) {
			updateMovement ();
			updateFacingDirection ();
			updateAnimation ();
		}
	}

	public bool getIsReadyToEat() {
		return isReadyToEat;
	}

	public Vector2 getVelocity() {
		return velocity;
	}

	public void die() {
		GameStatistics.decrementLives();
		if(GameStatistics.getLives() < 0) {
			isAlive = false;
			Destroy (this.gameObject);
		} else {
			GameObject currentEgg = eggs[eggs.Count - 1]; 
			this.transform.position = currentEgg.transform.position;
			eggs.Remove(currentEgg);
			Destroy(currentEgg);
			isFacingRight = true;
			transform.eulerAngles = new Vector3 (this.transform.rotation.eulerAngles.x, 180, this.transform.rotation.eulerAngles.z);
		}

	}

	public void evolve() {
		Debug.Log (originalMaximumVelocity + " " + maximumVelocity);
		if (GameStatistics.getGatheredCollectablesOfType(CollectableType.REGULAR_PLANT) == 6) {
			spriteRenderer.color = new Color(1f,0.3f,0.3f,1f);
			isFast = true;
			recalculateSpeeds();
		}
		if(GameStatistics.getGatheredCollectablesOfType(CollectableType.ENEMY_FISH) == 6) {
			isStrong = true;
			GetComponent<Rigidbody2D>().mass = 800;
			recalculateSpeeds();
		}
		Debug.Log (originalMaximumVelocity + " " + maximumVelocity);
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

}
