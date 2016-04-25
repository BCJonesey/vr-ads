using UnityEngine;
using System.Collections;

public class Poster : MonoBehaviour {

	public Light posterLight;

	public void OnClick() {
		ToggleLight(!posterLight.enabled);
	}

	public void ToggleLight(bool toogle) {
		posterLight.enabled = toogle;
	}
}
