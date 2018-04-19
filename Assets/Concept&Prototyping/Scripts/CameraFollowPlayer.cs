using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowPlayer : MonoBehaviour {

    public Transform targetTransform;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (targetTransform)
        {
            Vector3 newPos = targetTransform.position;

            newPos.z = -10f;

            transform.position = newPos;
        }
	}
}
