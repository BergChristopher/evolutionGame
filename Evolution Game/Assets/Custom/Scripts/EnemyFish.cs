using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyFish : MonoBehaviour {

	public FishType fishType = FishType.TEETH_FISH;
	public bool isMoving = true;
	public float speed = 3;
	public List<Transform> waypoints = new List<Transform>(); 

	private int currentWaypoint = 0;

	// Use this for initialization
	void Start () {
		if(waypoints.Count < 2) {
			Debug.LogWarning("Your fish " + name + " has less than 2 waypoints assigned ");
		}
		if(speed < 0) {
			Debug.LogWarning("Your fish " + name + " has a speed less than 0, it will be converted to " + Mathf.Abs(speed) + ".");
		}
	}
	
	// Update is called once per frame
	void Update () {
		if (isMoving && waypoints.Count >= 2) {
			Vector2 fishPosition = new Vector2(transform.position.x, transform.position.y);
			Vector2 target = new Vector2(waypoints[currentWaypoint].position.x, waypoints[currentWaypoint].position.y);
			fishPosition = Vector2.MoveTowards(fishPosition, target, Mathf.Abs(speed * Time.deltaTime));
			this.transform.position = new Vector3(fishPosition.x, fishPosition.y, this.transform.position.z);
			//Vector3.MoveTowards(this.transform.position, , speed * Time.deltaTime);
			if(fishPosition.x == target.x && fishPosition.y == target.y) {
				currentWaypoint = (currentWaypoint + 1) % waypoints.Count;
			}
			Quaternion fishRotation = transform.rotation;
			if(fishPosition.x < waypoints[currentWaypoint].position.x) {
				fishRotation.y = 180;
			} else if(fishPosition.x > waypoints[currentWaypoint].position.x){
				fishRotation.y = 0;
			}
			transform.rotation = fishRotation; 
		}
	}

	void OnTriggerEnter2D (Collider2D enteringCollider) {
		if (enteringCollider.gameObject.tag == "Player") {
			FishController fish = enteringCollider.GetComponent<FishController>();
			if(fish != null) {
				GameStatistics.addDeathByFish(this.fishType);
				fish.die();
			}
		}
	}
}

public enum FishType { TEETH_FISH };