using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ControllerInput : MonoBehaviour {

    public string playerPrefix;

    public float rotationalForce;

    public float thrust;
    public float maxSpeed;
    public float fireRate;
    public float muzzleVelocity;

    public ParticleSystem particles;
    public Transform gunBarrel;
    public GameObject bulletPrefab;

    private Rigidbody2D rb2D;
    private PlayerPhysics impactScript;
    private ParticleSystem.EmissionModule em;
    private bool firing;
    private float nextBulletTimer;
    private float rotationInput;
    private float thrustInput;
    private Vector2 thrustTotal;

    private bool Engage = false;

    // Use this for initialization
    void Start () {
        rb2D = GetComponent<Rigidbody2D>();
        particles = GetComponentInChildren<ParticleSystem>();
        impactScript = GetComponent<PlayerPhysics>();
    }
	
	// Update is called once per frame
	void Update () {
        nextBulletTimer -= Time.deltaTime;
        rotationInput = -Input.GetAxis(playerPrefix + "Horizontal"); // This is reversed because Unity uses a left hand rule for rotation
        thrustInput = Input.GetAxis(playerPrefix + "Vertical");

        if (thrustInput < 0f)
            thrustInput = 0f;
        
        if (particles != null)
        {
            em = particles.emission;

            float trailRate = (thrustInput / 1f) * 100f;

            em.rateOverTime = trailRate;
        }

        thrustTotal = (transform.up * thrustInput * thrust);

        //transform.Rotate(Vector3.forward * Time.deltaTime * -rotationInput * rotationSpeed);
        //Debug.Log("Applying " + (transform.up * thrustInput * thrust * Time.deltaTime) + " thrust");
        //rb2D.AddForce(transform.up * thrustInput * thrust * Time.deltaTime);

        rb2D.velocity = Vector2.ClampMagnitude(rb2D.velocity, maxSpeed);

        if (Input.GetButtonDown(playerPrefix + "Fire1"))
        {
            firing = true;
        }

        if (Input.GetButtonUp(playerPrefix + "Fire1"))
        {
            firing = false;
        }

        if (firing)
        {
            if (nextBulletTimer <= 0f)
            {
                rb2D.AddForceAtPosition(-transform.up * muzzleVelocity * 0.25f, gunBarrel.position);
                nextBulletTimer = fireRate;
                GameObject bullet = Instantiate(bulletPrefab, gunBarrel.position, Quaternion.identity);
                bullet.GetComponent<Rigidbody2D>().velocity = transform.up * muzzleVelocity;
            }
        }

        if (Input.GetKey(KeyCode.U))
        {
            Engage = true;
        }
        else
        {
            Engage = false;
        }

        if (impactScript != null)
        {
            impactScript.ApplyPlayerInput(rotationInput * rotationalForce, thrustTotal);
        }
    }

    private void FixedUpdate()
    {

    }
}

