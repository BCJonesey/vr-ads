using UnityEngine;
using System.Collections;

public class Spin : MonoBehaviour {
	public float rotationSpeed;

	void Update() {
		transform.RotateAroundLocal(Vector3.up, Time.deltaTime * rotationSpeed);
	}
}
