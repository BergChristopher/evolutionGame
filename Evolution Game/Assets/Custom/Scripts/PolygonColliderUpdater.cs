using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PolygonColliderUpdater : MonoBehaviour {

	private Dictionary<string, PolygonCollider2D> spriteToCollider = new Dictionary<string, PolygonCollider2D>();
	private SpriteRenderer spriteRenderer;
	private string currentSpriteName = null;
	private PolygonCollider2D currentPolygonCollider = null;

	// Use this for initialization
	void Start () {
		currentPolygonCollider = GetComponent<PolygonCollider2D>();
		if(currentPolygonCollider == null) {
			currentPolygonCollider = gameObject.AddComponent<PolygonCollider2D>();
		}

		spriteRenderer = GetComponent<SpriteRenderer>();
		if(spriteRenderer == null) {
			Debug.LogError("No SpriteRenderer attached to PolygonColliderUpdater Component on " + name);
		} else if(spriteRenderer.sprite != null){
			currentSpriteName = spriteRenderer.sprite.name;
			spriteToCollider.Add(currentSpriteName, currentPolygonCollider);
		} else {
			Debug.LogError("No sprite attached to SpriteRenderer on " + name);
		}
	}
	
	// Update is called once per frame
	void Update () {
		updateCollider(); 
	}

	private void updateCollider() {
		if(spriteRenderer != null) {
			string newSpriteName = spriteRenderer.sprite.name;
			if(newSpriteName != null) {
				if(newSpriteName != currentSpriteName) {
					changeCollider(newSpriteName);
				}
			}
		}
	}

	private void changeCollider (string spriteName) {
		bool isTrigger = currentPolygonCollider.isTrigger;
		PolygonCollider2D newPolygonCollider = null;

		if(spriteToCollider.ContainsKey(spriteName)) {
			newPolygonCollider = spriteToCollider[spriteName];
		} else {
			newPolygonCollider = gameObject.AddComponent<PolygonCollider2D>();
			spriteToCollider.Add(spriteName, newPolygonCollider);
		}
		currentPolygonCollider.enabled = false;
		newPolygonCollider.enabled = true;
		newPolygonCollider.isTrigger = isTrigger;
		currentPolygonCollider = newPolygonCollider;
		currentSpriteName = spriteName;
	}

}
