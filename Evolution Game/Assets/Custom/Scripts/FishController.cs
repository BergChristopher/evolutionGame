using UnityEngine;
using System.Collections;

public class FishController : MonoBehaviour {

	private const int ESTIMATED_FRAMES_PER_SECOND = 60;

	public Vector2 swimAcceleration = new Vector2 (4f,2f);
	public Vector2 dragCoefficient = new Vector2 (0.01f, 0.015f);
	public Vector2 maximumVelocity = new Vector2 (20f,10f); // units per second

	private Animator animator;

	private Vector2 velocity = Vector2.zero;
	private bool isFacingRight = true;
	private bool isReadyToEat = false;

	// Use this for initialization
	void Start () {
		animator = this.GetComponent<Animator>();
		if (animator == null) {
			Debug.LogWarning("No animator attached to FishController on " + this.name);
		}
	}
	
	// Update is called once per frame
	void Update () {


		updateMovement ();
		updateFacingDirection ();
		updateAnimation ();


	}

	public bool getIsReadyToEat() {
		return isReadyToEat;
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

		Debug.Log ("horDrag = " + horizontalDrag + " horVel = " + velocity.x + " Time.delta " + Time.deltaTime);

		if (velocity.x > horizontalDrag) {
			velocity.x -= horizontalDrag; //physical drag applied
		} else if (velocity.x < -horizontalDrag) {
			velocity.x += horizontalDrag; //physical drag applied
		} else {
			velocity.x = 0; //prevent too small velocities
		}

		Debug.Log ("results in " + ((velocity.x - horizontalDrag) * Time.deltaTime * 60));

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
		                                      this.transform.position.y + (velocity.y * Time.deltaTime), 
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
}
