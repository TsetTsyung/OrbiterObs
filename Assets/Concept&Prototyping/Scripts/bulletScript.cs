using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bulletScript : MonoBehaviour {

    public float damageAmount;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //Debug.Log("We hit something...");
        if (collision.collider.CompareTag("Player"))
        {
            //Debug.Log("We hit a player");
            collision.collider.GetComponent<PlayerHealth>().TakeDamage(damageAmount);
        }
    }
}
