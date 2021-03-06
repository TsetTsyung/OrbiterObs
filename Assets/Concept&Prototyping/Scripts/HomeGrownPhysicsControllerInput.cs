﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomeGrownPhysicsControllerInput : MonoBehaviour {

    public string playerPrefix;

    #region Public Ship Specific Variables
    public float rotationSpeed;
    public float maxEnvironRotSpeed;
    public float rotationalBreak; // This is the reverse to slow down extra rotational forces other than use input
    public float minRotationThreshold;
    public float thrust;
    public float maxSpeed;
    public float fireRate;
    public float muzzleVelocity;
    #endregion

    #region Other Gameobject Components
    public ParticleSystem particles;
    public Transform gunBarrel;
    public GameObject bulletPrefab;
    private Rigidbody2D rb2D;
    private PlayerImpactSpinV2 impactScript;
    private ParticleSystem.EmissionModule em;
#endregion

    private bool firing;
    private float nextBulletTimer;
    public float rotationInput;
    public float spinRotation;
    public float totalRotationSpeed;
    public float thrustInput;

    // Use this for initialization
    void Start()
    {
        rb2D = GetComponent<Rigidbody2D>();
        particles = GetComponentInChildren<ParticleSystem>();
        impactScript = GetComponent<PlayerImpactSpinV2>();
    }

    // Update is called once per frame
    void Update()
    {
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

        //Debug.Log("Applying thrust of " + transform.up * thrustInput * thrust * Time.deltaTime);
        rb2D.AddForce(transform.up * thrustInput * thrust * Time.deltaTime);

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

        //////////////////////////////////////
        //
        // This is the important point of this script
        //
        //////////////////////////////////////
        //impactScript.ApplyPlayerInput(rotationInput);
    }

    private void FixedUpdate()
    {
    }
}
