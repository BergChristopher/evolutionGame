using UnityEngine;
using System.Collections;

public class FishController : MonoBehaviour {

	private const int ESTIMATED_FRAMES_PER_SECOND = 60;

	public Vector2 swimAcceleration = new Vector2 (4f,2f);
	public Vector2 dragCoefficient = new Vector2 (0.01f, 0.015f);
	public Vector2 maximumVelocity = new Vector2 (20f,10f); // units per second

	public float maximumYPosition = 12.7f;

	private Animator animator;

	private Vector2 velocity = Vector2.zero;
	private bool isFacingRight = true;
	private bool isReadyToEat = false;
	private bool isAlive = true;

	// Use this for initialization
	void Start () {
		animator = this.GetComponent<Animator>();
		if (animator == null) {
			Debug.LogWarning("No animator attached to FishController on " + this.name);
		}
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

	public void die() {
		isAlive = false;
		Destroy (this.gameObject);
	}

	private void updateFacingDirection() {
		if ((Input.GetAxis ("Horizontal") < 0 && isFacingRight) || (Input.GetAxis ("Horizontal") > 0 && !isFacingRight)) {
			isFacingRight = !isFacingRight;
			if (isFacingRight) {
				this.transform.rotation = new Quaternion (this.transform.rotation.x, 180, this.transform.rotation.z, this.transform.rotation.w);
			} else {
				this.transform.rotation = new Quaternion (this.transform.rotation.x, 0, this.transform.rotation.z, this.transform.rotation.w);
			}
		}
	}

	private void updateMovement() {
		Vector2 previousVelocity = Vector2.zero;
		previousVelocity.x = velocity.x; 
		previousVelocity.y = velocity.y;

		velocity.x = previousVelocity.x + (Input.GetAxis ("Horizontal") * swimAcceleration.x * Time.deltaTime * ESTIMATED_FRAMES_PER_SECOND); //v=v0+a*(t-t0)
		velocity.y = previousVelocity.y + (Input.GetAxis ("Vertical") * swimAcceleration.y * Time.deltaTime * ESTIMATED_FRAMES_PER_SECOND); //v=v0+a*(t-t0)
		
		float horizontalDrag = (dragCoefficient.x * (velocity.x * velocity.x)) + (dragCoefficient.x);
		float verticalDrag = (dragCoefficient.y * (velocity.y * velocity.y)) + (dragCoefficient.y);

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
		                                      Mathf.Clamp((this.transform.position.y + (velocity.y * Time.deltaTime)),float.MinValue,maximumYPosition), 
		                                      this.transform.position.z);
	}

	private void updateAnimation() {
		if (Input.GetKeyDown (KeyCode.Space)) {
			animator.SetInteger("Action",1); //Action 1 indicates a transition to the eat animation
			isReadyToEat = true;
		}
		if (Input.GetKeyUp (KeyCode.Space)) {
			animator.SetInteger("Action",0); //Action 0 indicates a transition to the normal animation
			isReadyToEat = false;
		}
	}

	public void evolve() {
		if (GameStatistics.getAmountRegularPlants() == 6) {
			swimAcceleration = new Vector2 (10f,5f);
			maximumVelocity = new Vector2 (40f,20f);
		}
	}
}
