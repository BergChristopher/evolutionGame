using UnityEngine;
using System.Collections;

public class FishtailController : MonoBehaviour {

	Animator animator;

	void Start () {
		animator = this.GetComponent<Animator> ();
	}
	
	public void setIsFast (bool isFast) {
		animator.SetBool("isFast", isFast);
	}

	public void updateCollider() {
	}
}
