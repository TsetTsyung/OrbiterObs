using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipsLimits : MonoBehaviour {

    public float speedLimit = 5f;
    Rigidbody2D rb2D;

	// Use this for initialization
	void Start () {
        rb2D = GetComponent<Rigidbody2D>();
	}
	
	// Update is called once per frame
	void Update () {
            //Debug.Log("We're going too fast!");
	}
}
