using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateByImpact : MonoBehaviour {

    private Rigidbody2D rb2D;
    private float torque;
    private ContactPoint2D[] contact;

    // Use this for initialization
    void Start () {
        rb2D = GetComponent<Rigidbody2D>();

        contact = new ContactPoint2D[1];
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    private void OnCollisionEnter2D(Collision2D collision)
    {
        collision.GetContacts(contact);

        torque = ForceToTorque(contact[0].relativeVelocity, contact[0].point, ForceMode2D.Impulse);
    }

    public float ForceToTorque(Vector2 force, Vector2 position, ForceMode2D forceMode = ForceMode2D.Force)
    {
        // Vector from the force position to the CoM
        Vector2 p = rb2D.worldCenterOfMass - position;
        
        // Get the angle between the force and the vector from position to CoM
        float angle = Mathf.Atan2(p.y, p.x) - Mathf.Atan2(force.y, force.x);
        
        // This is basically like Vector3.Cross, but in 2D, hence giving just a scalar value instead of a Vector3
        float t = p.magnitude * force.magnitude * Mathf.Sin(angle) * Mathf.Rad2Deg;

        // Continuous force
        //if (forceMode == ForceMode2D.Force) t *= Time.fixedDeltaTime;

        // Apply inertia
        return t / rb2D.inertia;
    }

    private void FixedUpdate()
    {
        if (torque != 0f)
            //Debug.LogError("On the rotated object " + name + " the torque is " + torque);
        rb2D.AddTorque(torque);

        torque = 0f;
    }

}
