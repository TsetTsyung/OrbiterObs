using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour {

    public bool invulnerable;

    [SerializeField]
    private float startingHealth;
    [SerializeField]
    private float maxHealth;

    private float currentHealth;
    private bool alive = true;

    // Use this for initialization
    void Start () {
        currentHealth = startingHealth;
	}
	
	// Update is called once per frame
	void Update () {
        CheckForDeath();
	}

    public void TakeDamage(float damageTaken)
    {
        if (invulnerable)
            return;
        //Debug.Log("Player taking damage");
        currentHealth -= damageTaken;
    }

    private void CheckForDeath()
    {
        if (alive && currentHealth <= 0f)
        {
            //Debug.Log("We died");
            alive = false;
            GetComponent<Rigidbody2D>().simulated = false;
            GetComponentInChildren<SpriteRenderer>().enabled = false;
        }
    }
}
