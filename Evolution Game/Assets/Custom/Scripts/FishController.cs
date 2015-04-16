using UnityEngine;
using System.Collections;

public class FishController : MonoBehaviour {

	public Vector2 swimAcceleration = new Vector2 (1f,1f);
	public Vector2 dragCoefficient = new Vector2 (0.1f, 0.1f);
	public Vector2 maximumVelocity = new Vector2 (0.5f,0.5f);

	private Vector2 calculatedVelocity = Vector2.zero;
	private Vector2 previousSpeed = Vector2.zero;

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
		calculatedVelocity.x = previousSpeed.x + (Input.GetAxis ("Horizontal") * swimAcceleration.x * Time.deltaTime); //v=v0+a*(t-t0)
		calculatedVelocity.y = previousSpeed.y + (Input.GetAxis ("Vertical") * swimAcceleration.y * Time.deltaTime); //v=v0+a*(t-t0)

		float horizontalDrag = (dragCoefficient.x * (calculatedVelocity.x * calculatedVelocity.x)) 
			+ Mathf.Abs (Time.deltaTime * dragCoefficient.x);
		float verticalDrag = (dragCoefficient.y * (calculatedVelocity.y * calculatedVelocity.y)) 
			+ Mathf.Abs (Time.deltaTime * dragCoefficient.y);

		if (calculatedVelocity.x > horizontalDrag) {
			calculatedVelocity.x -= horizontalDrag; //physical drag applied
		} else if (calculatedVelocity.x < -horizontalDrag) {
			calculatedVelocity.x += horizontalDrag; //physical drag applied
		} else {
			calculatedVelocity.x = 0; //prevent too small velocities
		}

		if (calculatedVelocity.y > verticalDrag) {
			calculatedVelocity.y -= verticalDrag; //physical drag applied
		} else if (calculatedVelocity.y < -verticalDrag) {
			calculatedVelocity.y += verticalDrag; //physical drag applied
		} else {
			calculatedVelocity.y = 0; //prevent too small velocities
		}

		calculatedVelocity.x = Mathf.Clamp (calculatedVelocity.x, (maximumVelocity.x * -1), maximumVelocity.x); //clamp between max and min velocity
		calculatedVelocity.y = Mathf.Clamp (calculatedVelocity.y, (maximumVelocity.y * -1), maximumVelocity.y); //clamp between max and min velocity

		previousSpeed.x = calculatedVelocity.x; 
		previousSpeed.y = calculatedVelocity.y;

		Debug.Log ("Velocity.x = " + calculatedVelocity.x + "Velocity.y = " + calculatedVelocity.y);

		this.transform.position = new Vector3(this.transform.position.x + calculatedVelocity.x, 
		                                      this.transform.position.y + calculatedVelocity.y, 
		                                      this.transform.position.z);
	}
}
