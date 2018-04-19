using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerPhysics : MonoBehaviour {

    public float minThreshold = 1f;
    public float maxControlledRotThreshold = 90f;
    public float maxControlRotThresholdGrace = 5f;
    public float drag = 5f;
    public float dragBrakingMultiplier = 4f;

    private Rigidbody2D rb2D;
    private Rigidbody2D incomingRB;
    private float nm;
    private float rotInputTotal;
    private Vector2 thrustTotalInput;

    private bool outOfControl = false;

    // Use this for initialization
    void Start () {
        rb2D = GetComponent<Rigidbody2D>();
	}
	
	// Update is called once per frame
	void Update () {
    }

    private void FixedUpdate()
    {
        // This SEEMS to solve the issue of "MoveRotation overriding AddToruq instead of adding it
        if (nm != 0f)
        { 
            rb2D.AddTorque(nm);
            nm = 0f;
        }
        else
        {
            if (rotInputTotal != 0f)
            {
                // Create rotation manually, just use Rigidbody for forward movement and collision detection

                rb2D.AddTorque(rotInputTotal * Time.fixedDeltaTime);
                rotInputTotal = 0f;
            }
            if (thrustTotalInput != Vector2.zero)
            {
                rb2D.AddForce(thrustTotalInput * Time.fixedDeltaTime);
                thrustTotalInput = Vector2.zero;
            }
        }

        float AbsoluteRotSpeed = Mathf.Abs(rb2D.angularVelocity);
        
        // Clamp the rotational speed IF under control
        if (!outOfControl)
        {
            if (AbsoluteRotSpeed > maxControlledRotThreshold + maxControlRotThresholdGrace)
            {
                //Debug.LogWarning("We're out of control");
                outOfControl = true;
                // Do we add the emergency brake?
            }
            else if(AbsoluteRotSpeed > maxControlledRotThreshold)
            {
                Mathf.Clamp(rb2D.angularVelocity, -maxControlledRotThreshold, maxControlledRotThreshold);
            }
            
        }
        else
        {
            // We are still out of control, so check that that we haven't gone below the 'control' threshold yet
            if (AbsoluteRotSpeed < maxControlledRotThreshold)
                outOfControl = false;
        }
    }

    #region Creating Torque From Impact
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
        //Debug.Log("The incoming force was calculated at " + impactForce + ", and the position of the impact was " + impactPos);

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
        //Debug.LogWarning("The pivot of " + pivot + " was calculated by " + rb2D.worldCenterOfMass + " - " + impactPos);

        //2.
        float angle = Mathf.Atan2(pivot.y, pivot.x) - Mathf.Atan2(impactForce.y, impactForce.x);

        //Debug.LogWarning("The angle between the 2 vectors is " + angle);
        
        //3.
        float torque = pivot.magnitude * impactForce.magnitude * Mathf.Sin(angle) * Mathf.Rad2Deg;

        //Debug.Log("The torque force is " + torque + ", if timed with fixeddeltatime it would be " + (torque * Time.fixedDeltaTime));

        return torque / rb2D.inertia;
    }
    #endregion

    //////////////////////////////
    //
    // ---- NOT USED -----
    //
    //////////////////////////////
    public void ApplyPlayerInput(float _rotInput, Vector2 _thrustTotal)
    {
        if (_rotInput != 0f)
        {
            rb2D.angularDrag = drag;
        }
        else
        {
            rb2D.angularDrag = drag * dragBrakingMultiplier;
        }

        if (Mathf.Abs(rb2D.angularVelocity) < maxControlledRotThreshold)
        {
            rotInputTotal = _rotInput;

        }

        thrustTotalInput = _thrustTotal;
    }
}
