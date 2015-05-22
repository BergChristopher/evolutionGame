using UnityEngine;
using System.Collections;

public class Plant : MonoBehaviour {
		
	public PlantType plantType = PlantType.REGULAR_PLANT;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnTriggerEnter2D (Collider2D enteringCollider) {
		if (enteringCollider.gameObject.tag == "Player") {
			FishController fish = enteringCollider.GetComponent<FishController>();
			if(fish != null && fish.getIsReadyToEat()) {
				GameStatistics.addEatenPlant(this.plantType);
				fish.evolve();
				Destroy(this.gameObject);
			}
		}
	}

}

public enum PlantType { REGULAR_PLANT };
