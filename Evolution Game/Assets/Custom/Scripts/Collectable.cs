using UnityEngine;
using System.Collections;

public class Collectable : MonoBehaviour {
		
	public CollectableType collectableType = CollectableType.REGULAR_PLANT;

	void OnTriggerStay2D (Collider2D enteringCollider) {
		if (enteringCollider.gameObject.tag == "Player" && enteringCollider.GetType().Equals(typeof(CircleCollider2D))) {
			FishController fish = enteringCollider.GetComponent<FishController>();
			if(fish != null && fish.getIsReadyToEat()) {
				GameStatistics.addCollectable(this.collectableType);
				fish.evolve();
				fish.GetComponent<AudioSource>().Play();
				Destroy(this.gameObject);
			}
		}
	}
}

public enum CollectableType { 
	[Description("regular plant")]
	REGULAR_PLANT, 
	[Description("enemy fish egg")]
	ENEMY_FISH_EGG, 
	[Description("enemy fish")]
	ENEMY_FISH 
};

public static class CollectableTypeExtension {
	public static string getDescription(this CollectableType collectableType) {
		Description[] da = (Description[])(collectableType.GetType().GetField(collectableType.ToString())).GetCustomAttributes(typeof(Description), false);
		return da.Length > 0 ? da[0].Value : collectableType.ToString();
	}
}
