using System;
using System.Resources;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using WiimoteApi;

public class PlayerController : Controller
{
    public AudioSource audioSource;
    public AudioClip hittingSound;
    public AudioClip serveThrowSound;
    public AudioClip serveHitSound;
    #region Variables
    [Tooltip("Set to 1 for Player 1, 2 for Player 2")]
    private readonly float speed = 30.0f, deceleration = 4.5f;
    // [NonSerialized] public bool backHand;
    private Rigidbody rb;
    private float serveHitThreshold = 1f, serveHitTimer = 0f;

    public Ball ball;
    [NonSerialized] private int servePressCount = 0;
    
    #endregion

    #region wii variables
    private Wiimote remote;
    private bool aPressed=false;

    // Add these constants at the top of the class
    private const float SWING_ACCELERATION_THRESHOLD = 2.0f;
    private const float SWING_COOLDOWN = 0.5f;

    // Add these variables with the other private fields
    private Vector3 previousAcceleration;
    private float lastSwingTime;
    private bool isSwinging;
    private int wiimoteIndex = 0; 
    private static bool[] wiimoteInitialized = new bool[2];
    #endregion

    private const float MIN_SERVE_PRESS_DELAY = 0.3f; // Minimum time between presses
    private float firstPressTime = 0f;
    [SerializeField]
    private GameController gameController;

    private float hitDuration = 0.36f; // Add this: Duration of hitting state
    private float currentHitTime = 0f; // Add this: Track current hit time
    void Start()
    {
        if(UIController.isSingle&&playerNum==2 ) gameObject.SetActive(false);
        netZ = (playerNum==2)? netZ+5f:netZ;
        if(playerNum==2){
            minZ=netZ;
        }else{
            maxZ=netZ;
        }
        // 玩家號碼從 1 開始，需要減 1 對應索引
        wiimoteIndex = playerNum - 1;

        // 從 WiimoteController 中獲取 Wiimote
        WiimoteController.Instance.InitializeWiimotes();
        remote = WiimoteController.Instance.GetWiimote(wiimoteIndex);
        if (remote == null)
        {
            Debug.LogError($"Wiimote {wiimoteIndex} 未初始化！");
        }

        // 初始化其他變數
        transform.position = defaultPosition;
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        isHitting = false;
        isServing = false;
        previousAcceleration = Vector3.zero;
        lastSwingTime = 0f;
        isSwinging = false;

        // 設置初始狀態
        if (gameController.servePlayer == playerNum)
        {
            _state = State.serverable;
        }
        else
        {
            _state = State.hittable;
        }
        Debug.Log(defaultPosition);
    }

    void Update()
    {
        if(remote != null)
        {
            int ret;
            do{
                ret=remote.ReadWiimoteData(); // Need to read data each frame
            }while(ret>0);
            
            DetectSwing();
        }
        if(playerNum==1){
            animator.SetBool("backHand", (ball.transform.position - gameObject.transform.position).x >= -1f? false : true);
        }else{
            animator.SetBool("backHand", (ball.transform.position - gameObject.transform.position).x >= 1? true : false);
        }
        
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
            // setServerText(true);
            HandleServe();
        }
        
        // Add this new block to handle interval state
        if (ball.gameController.game == GameController.GameState.Interval)
        {
            // Reset player position
            gameObject.transform.position = defaultPosition;
            // Reset server text
            // setServerText(true);
        }
    }

    #region Serve
    private void HandleServe()
    {
        #region handle space
        bool hitKeyPressed = (playerNum == 1) ? Input.GetKeyDown(KeyCode.Space) : Input.GetKeyDown(KeyCode.Return);
        
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
                audioSource.PlayOneShot(serveThrowSound);
            }
            else if (servePressCount == 1 && currentTime - firstPressTime >= MIN_SERVE_PRESS_DELAY)
            {
                // Second press - hit the ball if within threshold
                if (serveHitTimer <= serveHitThreshold)
                {
                    animator.SetBool("SecondPress", true);
                    ball.ServeHit();
                    audioSource.PlayOneShot(serveHitSound);
                }
                servePressCount = 0; // Reset counter
            }
        }
        #endregion

        #region handle wii remote
        else if(remote!=null && remote.Button.a)//avoid ghost key
        {
            float currentTime = Time.time;
            if(!aPressed){
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
                    audioSource.PlayOneShot(serveThrowSound);
                }
            }
            aPressed=true;
        }
        else{
            aPressed=false;
            if (isSwinging&&servePressCount == 1 && Time.time - firstPressTime >= MIN_SERVE_PRESS_DELAY)
            {
                // Second press - hit the ball if within threshold
                if (serveHitTimer <= serveHitThreshold)
                {
                    animator.SetBool("SecondPress", true);
                    ball.ServeHit(); // Add this call to trigger the serve
                    audioSource.PlayOneShot(serveHitSound);
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
        // setServerText(true);
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
        float moveHorizontal = 0f;
        float moveVertical = 0f;

        if (remote != null)
        {
            // D-pad controls remain unchanged for Wiimote
            if (remote.Button.d_left) moveHorizontal = -1f;
            else if (remote.Button.d_right) moveHorizontal = 1f;
            if (remote.Button.d_up) moveVertical = 1f;
            else if (remote.Button.d_down) moveVertical = -1f;
        }
        else
        {
            // Keyboard controls based on player number
            if (playerNum == 1)
            {
                // WASD for Player 1
                if (Input.GetKey(KeyCode.A)) moveHorizontal = -1f;
                if (Input.GetKey(KeyCode.D)) moveHorizontal = 1f;
                if (Input.GetKey(KeyCode.W)) moveVertical = 1f;
                if (Input.GetKey(KeyCode.S)) moveVertical = -1f;
            }
            else
            {
                // Arrow keys for Player 2 (invert vertical for correct direction)
                if (Input.GetKey(KeyCode.LeftArrow)) moveHorizontal = -1f;
                if (Input.GetKey(KeyCode.RightArrow)) moveHorizontal = +1f;
                if (Input.GetKey(KeyCode.UpArrow)) moveVertical = 1f; // Inverted because player 2 faces opposite direction
                if (Input.GetKey(KeyCode.DownArrow)) moveVertical = -1f; // Inverted because player 2 faces opposite direction
            }
        }

        // Calculate movement vector (consider player rotation)
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        Vector3 movement = (forward * moveVertical + right * moveHorizontal) * (speed * Time.deltaTime);

        // Move player if not hitting
        if (!isHitting)
        {
            Vector3 newPosition = rb.position + movement;
            float x = newPosition.x;
            float z = newPosition.z;
            x = Mathf.Clamp(x,minX,maxX);
            z = Mathf.Clamp(z,minZ,maxZ);
            newPosition = new Vector3(x,newPosition.y,z);
            rb.MovePosition(newPosition);
        }

        // Update velocity based on movement - Fix for both players
        if(playerNum == 2)
        {
            UpdateVelocity(-movement);  // Invert movement for player 2
        }
        else
        {
            UpdateVelocity(movement);   // Normal movement for player 1
        }
        
        DecelerateIfNoInput(movement);
    }

    private void UpdateVelocity(Vector3 movement)
    {
        // Update horizontalVelocity based on horizontal movement
        if (movement.x != 0)
        {
            horizontalVelocity = movement.x > 0 ? 2f : -2f;
        }
        
        // Update verticalVeloFcity based on vertical movement
        if (movement.z != 0)
        {
            verticalVelocity = movement.z > 0 ? 2f : -2f;
        }

        // Clamp velocities
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
        if(_state == State.serverable) return;
        
        bool hitKeyPressed = (playerNum == 1) ? Input.GetKeyDown(KeyCode.Space) : Input.GetKeyDown(KeyCode.Return);
        
        if (hitKeyPressed && !isHitting) // Only trigger new hit if not already hitting
        {
            isHitting = true;
            currentHitTime = hitDuration;
            animator.SetBool("Hit", true);
            audioSource.PlayOneShot(hittingSound);
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
            float x=gameObject.transform.position.x,z=gameObject.transform.position.z,y=gameObject.transform.position.y;
            x = Mathf.Clamp(x,minX,maxX);
            z = Mathf.Clamp(z,minZ,maxZ);
            gameObject.transform.position = new Vector3(x,y,z);
        }
        else if(other.gameObject.CompareTag("net")){
            float x=gameObject.transform.position.x,z=gameObject.transform.position.z,y=gameObject.transform.position.y;
            x = Mathf.Clamp(x,minX,maxX);
            z = Mathf.Clamp(z,minZ,netZ);
            gameObject.transform.position = new Vector3(x,y,z);
        }
    }
    #endregion

    #region Wii
    private void FinishedWithWiimotes() {
        foreach(Wiimote r in WiimoteManager.Wiimotes) {
            WiimoteManager.Cleanup(r);
        }
    }

    private void DetectSwing()
    {
        float currentTime = Time.time;
        if (currentTime - lastSwingTime < SWING_COOLDOWN) return;
        
        Vector3 acceleration = new Vector3(
            remote.Accel.GetCalibratedAccelData()[0],
            remote.Accel.GetCalibratedAccelData()[1],
            remote.Accel.GetCalibratedAccelData()[2]
        );

        float accelerationDelta = (acceleration - previousAcceleration).magnitude;

        if (accelerationDelta > SWING_ACCELERATION_THRESHOLD && !isSwinging && !isHitting)
        {
            isSwinging = true;
            lastSwingTime = currentTime;
            isHitting = true;
            currentHitTime = hitDuration;
            animator.SetBool("Hit", true);
            audioSource.PlayOneShot(hittingSound);
        }
        else if (isSwinging && currentTime - lastSwingTime > SWING_COOLDOWN)
        {
            isSwinging = false;
            isHitting = false;
            animator.SetBool("Hit", false);
            previousAcceleration = acceleration;
        }
    }

    // void OnApplicationQuit()
    // {
    //     if (remote != null)
    //     {
    //         FinishedWithWiimotes();
    //     }    
    // }    
    void OnDestroy()    
    {        
        if (remote != null)
        {
            wiimoteInitialized[wiimoteIndex] = false;
        }
    }
    #endregion
}
