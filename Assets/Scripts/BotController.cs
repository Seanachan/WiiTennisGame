using System;
using System.Resources;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using WiimoteApi;

public class BotController : Controller
{
    #region Variables
    [Tooltip("Set to 1 for Player 1, 2 for Player 2")]

    private readonly float speed = 40.0f, deceleration = 4.5f;

    // [NonSerialized] public bool backHand;

    private Rigidbody rb;
    private float serveHitThreshold = 1f, serveHitTimer = 0f;
    public Ball ball;
    [SerializeField]
    int swingDistance = 5;
    [NonSerialized] private int servePressCount = 0;
    Vector3 ballPosition;

    #endregion

    

    private const float MIN_SERVE_PRESS_DELAY = 0.3f; // Minimum time between presses
    private float firstPressTime = 0f;
    [SerializeField]
    private GameController gameController;

    [SerializeField] private float hitDuration = 0.36f; // Add this: Duration of hitting state
    private float currentHitTime = 0f; // Add this: Track current hit time
    bool gameOver = false;
    // Add these fields at the top with other variables

    void Start()
    {
        // Force position update at start
        transform.position = defaultPosition;

        animator = GetComponent<Animator>();

        // Initialize variables and get components
        gameObject.transform.position = defaultPosition;
        horizontalVelocity = 0;
        verticalVelocity = 0;
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        isHitting = false;
        isServing = false;

        if (gameController.servePlayer == playerNum)
        {
            _state = State.serverable;
        }
        else
        {
            _state = State.hittable;
        }
    }

    void Update()
    {
        if (playerNum == 1)
        {
            animator.SetBool("backHand", (ball.transform.position - gameObject.transform.position).x >= 1 ? false : true);
        }
        else
        {
            animator.SetBool("backHand", (ball.transform.position - gameObject.transform.position).x >= 1 ? true : false);
        }
        if (!gameOver && gameController.CheckGameOver())
        {
            gameOver = true;
        }
        
        if (gameOver) return;
        
        if (_state == State.hittable)
        {
            // Handle player movement and hitting actions
            animator.SetBool("isServing", isServing);
            animator.SetBool("SecondPress", false);
            setServerText(false);
            HandleMovement();
            HandleHit();
            UpdateAnimatorParameters();
        }
        else if (_state == State.serverable)
        {
            HandleServe();
        }

        // Add this new block to handle interval state
        if (ball.gameController.game == GameController.GameState.Interval)
        {
            // Reset player position
            gameObject.transform.position = defaultPosition;
            // Reset server text
            setServerText(true);
        }
    }

    #region Serve
    private void HandleServe()
    {
        #region handle space

        bool hitKeyPressed = true;
        
        if (hitKeyPressed)
        {
            float currentTime = Time.time;
            if (servePressCount == 0)
            {
                servePressCount++;
                firstPressTime = currentTime;
                // First press - toss the ball
                setServerText(false);
                isServing = true;
                animator.SetBool("isServing", isServing);
                ball.gameObject.SetActive(true);
                ball.ballToss();
                serveHitTimer = 0f;
            }
            else if (servePressCount == 1 && currentTime - firstPressTime >= MIN_SERVE_PRESS_DELAY)
            {
                // Second press - hit the ball if within threshold
                if (serveHitTimer <= serveHitThreshold)
                {
                    animator.SetBool("SecondPress", true);
                    ball.ServeHit();
                }
                servePressCount = 0; // Reset counter
            }
        }
        #endregion

        if (isServing)
        {
            serveHitTimer += Time.deltaTime;
            if (serveHitTimer > serveHitThreshold)
            {
                // Reset if player took too long to hit
                ResetServe();
            }
        }
    }

    private void ResetServe()
    {
        isServing = false;
        setServerText(true);
        animator.SetBool("isServing", isServing);
        animator.SetBool("SecondPress", false);
        serveHitTimer = 0f;
        servePressCount = 0;
        ball.ballReset();
    }

    #endregion
    #region Movement
    private void HandleMovement()
    {
        //更新目標位置，但只考慮 x 軸
        Rigidbody ballRb = ball.GetComponent<Rigidbody>();

        ballPosition.x = ballRb.position.x;
        ballPosition.z = transform.position.z;

        // 使用 Vector3.MoveTowards 計算下一幀的位置
        Vector3 movement = Vector3.MoveTowards(rb.position, ballPosition , speed * Time.deltaTime);

        // 限制新位置在長方形範圍內
        movement.x = Mathf.Clamp(movement.x, minX, maxX); // 限制 x 軸範圍
                                                               
        // Move player if not hitting
        if (!isHitting)
        {
            transform.position = movement;
        }

        // Update velocity based on movement
        if (playerNum == 2)
        {
            UpdateVelocity(-movement);  // Invert movement for player 2
        }
        else
        {
            UpdateVelocity(movement);   // Normal movement for player 1
        }
        // Decelerate if no input
        DecelerateIfNoInput(movement);
    }
    private void UpdateVelocity(Vector3 movement)
    {
        // Update horizontalVelocity based on horizontal movement
        if (movement.x != 0)
        {
            horizontalVelocity = movement.x > 0 ? 2f : -2f;
        }
        // Update verticalVelocity based on vertical movement
        if (movement.z != 0)
        {
            verticalVelocity = movement.z > 0 ? 2f : -2f;
        }

        // Clamp velocities to a maximum value
        horizontalVelocity = Mathf.Clamp(horizontalVelocity, -2f, 2f);
        verticalVelocity = Mathf.Clamp(verticalVelocity, -2f, 2f);
    }

    private void DecelerateIfNoInput(Vector3 movement)
    {
        // Decelerate horizontalVelocity if no horizontal input
        if (movement.x == 0)
        {
            if (Mathf.Abs(horizontalVelocity) < 0.005f)
                horizontalVelocity = 0;
            else
                horizontalVelocity += (horizontalVelocity > 0 ? -deceleration : deceleration) * Time.deltaTime;
        }

        // Decelerate verticalVelocity if no vertical input
        if (movement.z == 0)
        {
            if (Mathf.Abs(verticalVelocity) < 0.005f)
                verticalVelocity = 0;
            else
                verticalVelocity += (verticalVelocity > 0 ? -deceleration : deceleration) * Time.deltaTime;
        }
    }

    #endregion
    private void HandleHit()
    {
        if (_state == State.serverable) return;

        bool hitKeyPressed = false;
        Rigidbody ballRb = ball.GetComponent<Rigidbody>();
        Vector3 ballDir = ballRb.position - transform.position; // get the ball direction from the bot's position
        if (ballDir.magnitude < swingDistance)
        {
            hitKeyPressed = true;
        }
        else
        {
            hitKeyPressed = false;
        }

        if (hitKeyPressed && !isHitting) // Only trigger new hit if not already hitting
        {
            isHitting = true;
            currentHitTime = hitDuration;
            animator.SetBool("Hit", true);
        }

        // Reset hit state after animation
        if (currentHitTime > 0)
        {
            currentHitTime -= Time.deltaTime;
            if (currentHitTime <= 0)
            {
                isHitting = false;
                animator.SetBool("Hit", false);
            }
        }
    }

    private void UpdateAnimatorParameters()
    {
        // Update animator parameters with current velocities
        animator.SetFloat("VeloX", horizontalVelocity);
        animator.SetFloat("VeloY", verticalVelocity);
    }


    #region Collision
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("wall"))
        {
            float x = gameObject.transform.position.x, z = gameObject.transform.position.z, y = gameObject.transform.position.y;
            x = Mathf.Clamp(x, minX, maxX);
            z = Mathf.Clamp(z, minZ, maxZ);
            gameObject.transform.position = new Vector3(x, y, z);
        }
        else if (other.gameObject.CompareTag("net"))
        {
            float x = gameObject.transform.position.x, z = gameObject.transform.position.z, y = gameObject.transform.position.y;
            x = Mathf.Clamp(x, minX, maxX);
            z = Mathf.Clamp(z, minZ, netZ);
            gameObject.transform.position = new Vector3(x, y, z);
        }
    }
    #endregion

}
