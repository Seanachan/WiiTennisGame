using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private float velocityX;
    private float velocityY;
    private readonly float speed = 30.0f;
    private readonly float deceleration = 4.5f;
    private bool isHitting,isServing;
    private Animator animator;
    private Rigidbody rb;
    private readonly Vector3 defualtPosition = new Vector3(1.52f,0.65f,-14.21f);
    private float holdThreshold=2.833f,holdTime=0f;
    public enum State{
        serverable,
        hittable,
        hidden
    }
    public static State _state=State.serverable;

    void Start()
    {
        // Initialize variables and get components]
        gameObject.transform.position=defualtPosition;
        velocityX = 0;
        velocityY = 0;
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        isHitting = false;isServing=true;
    }

    void Update()
    {
        // if(_state == State.hittable){
            // Handle player movement and hitting actions
            HandleMovement();
            HandleHit();
            UpdateAnimatorParameters();   
        // }
        // else if(_state == State.serverable){
        //     HandleServe();
        // }
    }
    private void HandleServe(){
        if (Keyboard.current.spaceKey.wasPressedThisFrame) {
            isServing=true;
            animator.SetBool("isServing",isServing);
        }
        if (Keyboard.current.spaceKey.isPressed) holdTime += Time.deltaTime;
        if (Keyboard.current.spaceKey.wasReleasedThisFrame){
            if (holdTime < holdThreshold) {
                isServing=false;
                animator.SetBool("isServing",isServing);
            }
            holdTime = 0f;
        }
        if (holdTime >= holdThreshold && _state != State.hittable) {
            _state=State.hittable;
            isServing=false;
            animator.SetBool("isServing",isServing);
        }
    }
    private void HandleMovement()
    {
        // Get input for movement
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        // Calculate movement vector
        Vector3 movement = (transform.forward * moveVertical + transform.right * moveHorizontal) * (speed * Time.deltaTime);

        // Move player if not hitting
        if (!isHitting)
        {
            Vector3 newPosition = rb.position + movement;
            rb.MovePosition(newPosition);
        }

        // Update velocity based on movement
        UpdateVelocity(movement);
        // Decelerate if no input
        DecelerateIfNoInput(movement);
    }

    private void UpdateVelocity(Vector3 movement)
    {
        // Update velocityX based on horizontal movement
        if (movement.x != 0)
        {
            if (velocityX * movement.x < 0) velocityX = 0;
            velocityX += (movement.x > 0 ? speed : -speed) * Time.deltaTime;
        }
        // Update velocityY based on vertical movement
        if (movement.z != 0)
        {
            if (velocityY * movement.z < 0) velocityY = 0;
            velocityY += (movement.z > 0 ? speed : -speed) * Time.deltaTime;
        }

        // Clamp velocities to a maximum value
        velocityX = Mathf.Clamp(velocityX, -2f, 2f);
        velocityY = Mathf.Clamp(velocityY, -2f, 2f);
    }

    private void DecelerateIfNoInput(Vector3 movement)
    {
        // Decelerate velocityX if no horizontal input
        if (movement.x == 0)
        {
            if (Mathf.Abs(velocityX) < 0.005f)
                velocityX = 0;
            else
                velocityX += (velocityX > 0 ? -deceleration : deceleration) * Time.deltaTime;
        }

        // Decelerate velocityY if no vertical input
        if (movement.z == 0)
        {
            if (Mathf.Abs(velocityY) < 0.005f)
                velocityY = 0;
            else
                velocityY += (velocityY > 0 ? -deceleration : deceleration) * Time.deltaTime;
        }
    }

    private void HandleHit()
    {
        // Check for hit input
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isHitting = true;
            animator.SetBool("Hit", isHitting);
        }else{
            isHitting = false;
            animator.SetBool("Hit",isHitting);
        }

    }

    private void UpdateAnimatorParameters()
    {
        // Update animator parameters with current velocities
        animator.SetFloat("VeloX", velocityX);
        animator.SetFloat("VeloY", velocityY);
    }
}
