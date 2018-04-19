using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerImpactSpinV2 : MonoBehaviour {

    public float minThreshold = 4f;

    private HomeGrownPhysicsControllerInput inputControllerScript;
    private Rigidbody2D rb2D;

    private Rigidbody2D incomingRB;
    private float nm;
    private float rotInput;

    private bool countFrames = false;
    private int frameCount;

    // Use this for initialization
    void Start () {
        inputControllerScript = GetComponent<HomeGrownPhysicsControllerInput>();
        rb2D = GetComponent<Rigidbody2D>();

        inputControllerScript.minRotationThreshold = inputControllerScript.rotationSpeed * Time.fixedDeltaTime ;
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    private void FixedUpdate()
    {
        if (nm != 0f)
        {
            inputControllerScript.spinRotation += nm;

            nm = 0f;
        }

        if (inputControllerScript == null)
            return;

        // Remove all torque spin if it's below the threshold
        if (Mathf.Abs(inputControllerScript.spinRotation) <= inputControllerScript.minRotationThreshold)
            inputControllerScript.spinRotation = 0f;

        if (inputControllerScript.spinRotation != 0f)
        {
            Debug.Log("Spinrotation is " + inputControllerScript.spinRotation + ", and the braking force is " + -(Mathf.Sign(inputControllerScript.totalRotationSpeed) * inputControllerScript.rotationalBreak * Time.fixedDeltaTime));

            float brakingFactor = -Mathf.Sign(inputControllerScript.spinRotation) * Mathf.Max(1f, Mathf.Abs(inputControllerScript.spinRotation / 2f));
            Debug.LogError("brakingFactor is " + brakingFactor);

            inputControllerScript.spinRotation += -(Mathf.Sign(inputControllerScript.totalRotationSpeed) * inputControllerScript.rotationalBreak * Time.fixedDeltaTime * brakingFactor);
            inputControllerScript.totalRotationSpeed = inputControllerScript.spinRotation;
        }
        else
        {
            inputControllerScript.totalRotationSpeed = inputControllerScript.rotationInput * inputControllerScript.rotationSpeed * Time.fixedDeltaTime;
        }

        rb2D.MoveRotation(rb2D.rotation + inputControllerScript.totalRotationSpeed);
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        ///////////////////////////////
        //
        // other collider is always THIS collider,
        // whereas collision.collider is the object we hit
        //
        ///////////////////////////////

        ContactPoint2D[] contacts = new ContactPoint2D[1];

        incomingRB = null;

        // Populate the array of contact point for this collision
        collision.GetContacts(contacts);

        incomingRB = collision.collider.attachedRigidbody;

        if (incomingRB == null)
            return;

        Vector2 force = contacts[0].relativeVelocity * incomingRB.mass;

        nm = ApplyTorqueFromImpact(contacts[0].point, force);

        //countFrames = true;

        if (collision.collider.CompareTag("Bullet"))
            Destroy(collision.collider.gameObject);

    }

    private float ApplyTorqueFromImpact(Vector2 impactPos, Vector2 impactForce)
    {
        Debug.Log("The incoming force was calculated at " + impactForce + ", and the position of the impact was " + impactPos);

        /////////////////////////////////////
        //
        //  PROCESS:
        //
        //  1. Work out the vector from impact point to CoM (Centre of Mass)
        //  2. Work out the theta angle between the line running point-CoM and 
        //  line of impact with other object
        //  3. Use Pythag to find the torque component of incoming impactForce (sin[theta] * hypot[magnitude of impactForce])
        //  4. Multiply the distance to impact point (magnitude point-CoM) with torque component
        //  5. Applytorque to rigidbody
        //  
        //  TODO: Take into account the stationary objects, as their velocity will be 0
        //  
        //
        /////////////////////////////////////

        //1.
        Vector2 pivot = rb2D.worldCenterOfMass - impactPos;
        Debug.LogWarning("The pivot of " + pivot + " was calculated by " + rb2D.worldCenterOfMass + " - " + impactPos);

        //2.
        float angle = Mathf.Atan2(pivot.y, pivot.x) - Mathf.Atan2(impactForce.y, impactForce.x);

        Debug.LogWarning("The angle between the 2 vectors is " + angle);

        //3.
        float torque = pivot.magnitude * impactForce.magnitude * Mathf.Sin(angle) * Mathf.Rad2Deg;

        Debug.Log("The torque force is " + torque + ", if timed with fixeddeltatime it would be " + (torque * Time.fixedDeltaTime));

        return torque / rb2D.inertia;
    }
}
