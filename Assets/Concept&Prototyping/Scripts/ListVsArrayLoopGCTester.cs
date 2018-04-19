using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ListVsArrayLoopGCTester : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.E))
        {
            // Start the test, 10 loops of each
            for (int a = 0; a < 10; a++)
            {

            }
        }
	}
}
