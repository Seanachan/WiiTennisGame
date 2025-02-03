using System;
using TMPro;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Ball : MonoBehaviour
{
    public AudioClip bounceSound;
    public AudioClip hittingSound;
    public AudioSource audioSource;
    #region ball variables
    // player and racket references
    public GameObject racket, serveTarget1,serveTarget2;
    private GameObject serveTarget;
    public Controller player1,player2;

    // Ball state variables
    private Vector3 initialPos;
    private Rigidbody ballRb;
    private Vector3 hitDirection, finalForce;
    [SerializeField] private float hitStrength = 23.0f; // Reduced hit strength for better control
    private float spinAmount = 5.0f, tossDistance = 0.0f, tossForceMultiplier = 0.50f;
    private float tossHeight = 4.0f; 
    [NonSerialized] public bool tossed = false;
    private float offset = 13.0f; 
    [SerializeField] Mode mode;
    #endregion

    // Add at the top with other fields
    public ReplaySystem replaySystem1,replaySystem2;
    private ReplaySystem replaySystem;  // Single replay system reference

    public enum Mode
    {
        Single,
        Double
    }
    #region player State
    public GameController gameController;
    [SerializeField] private GameController.GameState game;
    private GameController.PlayerTurn turn;
    private Controller serverPlayer;
    private bool isWithinPlayerRange = false;
    private PlayerController currentPlayer = null;
    private float hitStartTime = 0f;    // When player enters trigger zone
    private float maxHitWindow = 0.3f;  // Maximum time window for hitting
    #endregion


    void Start()
    {

        if (UIController.isSingle || SceneManager.GetActiveScene().name == "SingleMainScene")
        {
            mode = Mode.Single;
        }
        else if (UIController.isSingle || SceneManager.GetActiveScene().name == "MainScene")
        {
            mode = Mode.Double;
        }
        else
        {
            Debug.Log("SceneLoad Problem!!");
        }



        if (mode.ToString() == "Single")
        {
            player1 = (PlayerController)player1;
            player2 = (BotController)player2;
        }
        else if (mode.ToString() == "Double")
        {
            player1 = (PlayerController)player1;
            player2 = (PlayerController)player2;
        }

        // Initialize variables and get components
        gameObject.SetActive(false);
        if (gameController.servePlayer == 1)
        {
            serverPlayer = player1;
            turn = GameController.PlayerTurn.P1;
            initialPos = serverPlayer.transform.position + Vector3.up * 4.0f + Vector3.forward * 1.5f + Vector3.right * 0.8f;
        }
        else
        {
            serverPlayer = player2;
            turn = GameController.PlayerTurn.P2;
            initialPos = serverPlayer.transform.position + Vector3.up * 4.0f + Vector3.forward * 1.5f + Vector3.right * -0.8f;
        }


        if (serverPlayer == player1) serveTarget = serveTarget1;
        else serveTarget = serveTarget2;

        gameObject.transform.position = initialPos;
        ballRb = gameObject.GetComponent<Rigidbody>();
        ballRb.isKinematic = true; // Disable physics initially
        game = GameController.GameState.Going;
    }

    void Update()
    {
        if(gameObject.transform.position.z > serverPlayer.netZ&&turn==GameController.PlayerTurn.P1){
            turn=GameController.PlayerTurn.P2;  
            gameController.bounceTime=0;
        }else if(gameObject.transform.position.z <= serverPlayer.netZ&&turn==GameController.PlayerTurn.P2) {
            turn=GameController.PlayerTurn.P1;  
            gameController.bounceTime=0;
        }

        if(game==GameController.GameState.Interval){
            resetGame();
        }
        if (serverPlayer._state == PlayerController.State.serverable)
        {
            if (serverPlayer.isServing)
            {
                gameObject.SetActive(true);
                ballRb.isKinematic = false; // Make sure physics is enabled when serving
            }
            else if (serverPlayer._state != PlayerController.State.hittable && serverPlayer.isHitting)
            {
                ballReset(); // Use the reset method instead
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            hitStartTime = Time.time;  // Record when player enters hitting zone
            Controller hitPlayer = other.GetComponent<PlayerController>();
    // if(hitPlayer==null) {Debug.Log(hitPlayer!=null);hitPlayer = other.GetComponent<BotController>();Debug.Log(hitPlayer!=null);}

            if (hitPlayer != null)
            {
                isWithinPlayerRange = true;
                currentPlayer = (PlayerController) hitPlayer;
            }
        }
        else if(other.CompareTag("out")){
            if(gameController.bounceTime==1){
                if(turn == GameController.PlayerTurn.P1){
                    gameController.AddOpponentScore();
                    replaySystem=replaySystem2;
                }else{
                    gameController.AddPlayerScore();
                    replaySystem=replaySystem1;
                }
            }else{
                if(turn == GameController.PlayerTurn.P1){
                    gameController.AddPlayerScore();
                    replaySystem=replaySystem1;
                }else{
                    gameController.AddOpponentScore();
                    replaySystem=replaySystem2;
                }
            }
            audioSource.PlayOneShot(bounceSound);
            gameController.bounceTime=0;
            game = GameController.GameState.Interval;

            if (replaySystem != null)
            {
                replaySystem1.StopRecording();
                replaySystem2.StopRecording();
                replaySystem.StartPlayback(player1, player2);
            }
            resetGame();
        
        }
        else if(other.CompareTag("in")){
            gameController.bounceTime++;
            audioSource.PlayOneShot(bounceSound);
            if(gameController.bounceTime==2){
                bool isPlayer2Point = turn == GameController.PlayerTurn.P2;
                
                if(turn==GameController.PlayerTurn.P1){
                    gameController.AddOpponentScore();
                    replaySystem=replaySystem2;
                }else{
                    gameController.AddPlayerScore();
                    replaySystem=replaySystem1;
                }
                gameController.bounceTime=0;
                game = GameController.GameState.Interval;
                resetGame();
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if(other.CompareTag("Player") && isWithinPlayerRange && currentPlayer != null && currentPlayer.isHitting)
        {
            // Calculate timing (0 = early hit, 1 = late hit)
            float hitTiming = Mathf.Clamp01((Time.time - hitStartTime) / maxHitWindow);
            
            // Early hits get wider angles (up to 60 degrees for earliest hits)
            float sideAngle = Mathf.Lerp(10f, -15f, hitTiming);
            
            // Determine direction based on player's horizontal velocity
            float directionMultiplier = currentPlayer.horizontalVelocity >= 0 ? 1f : -1f;
            if (currentPlayer == player2)
            {
                directionMultiplier *= -1f; // Invert for player 2's side
            }
            if(currentPlayer.transform.position.x < ballRb.transform.position.x){
                directionMultiplier *= -1f;
            }
            
            // Apply angle to forward direction
            Vector3 hitDirection = Quaternion.Euler(0, sideAngle * directionMultiplier, 0) * currentPlayer.transform.forward;
            float distanceToNet = Mathf.Abs(transform.position.z - currentPlayer.netZ);
            float upwardAngle = Mathf.Lerp(15f, 20f, distanceToNet / 20f);
            hitDirection = Quaternion.Euler((currentPlayer==player1)? -upwardAngle : upwardAngle, 0, 0) * hitDirection;
            
            // Early hits are slightly weaker to balance gameplay
            float powerMultiplier = Mathf.Lerp(0.5f, 1.0f, distanceToNet / 20f);
            float adjustedStrength = hitStrength * powerMultiplier;
            
            // Apply forces
            ballRb.linearVelocity = Vector3.zero;
            ballRb.AddForce(hitDirection * adjustedStrength, ForceMode.Impulse);
            ballRb.AddTorque(Vector3.right * spinAmount);

            // Reset states
            currentPlayer.isHitting = false;
            isWithinPlayerRange = false;
            currentPlayer = null;

            audioSource.PlayOneShot(hittingSound);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            isWithinPlayerRange = false;
            currentPlayer = null;
        }
    }

    private void OnCollisionEnter(Collision collision){
        if(collision.gameObject.CompareTag("Player")){
            Rigidbody rb = GetComponent<Rigidbody>();

            Vector3 newVelo = rb.linearVelocity;
            newVelo = new Vector3(Mathf.Abs(newVelo.x),Mathf.Abs(newVelo.y),Mathf.Abs(newVelo.z));
            rb.linearVelocity = newVelo;
        }
    }

    private void UpdateInitialPosition()
    {
        if (gameController.servePlayer == 1)
        {
            // For player 1, position the ball slightly to their right and in front
            initialPos = player1.transform.position + 
                        new Vector3(0.8f, 1.5f, 1.0f);
        }
        else
        {
            // For player 2, position the ball slightly to their left and in front
            initialPos = player2.transform.position + 
                        new Vector3(-0.8f, 1.5f, -1.0f);
        }
    }

    void resetGame(){
        if(gameController.servePlayer==1){
            serverPlayer=player1;
            serveTarget=serveTarget1;
            player1._state=PlayerController.State.serverable;
            player2._state=PlayerController.State.hittable;

            turn = GameController.PlayerTurn.P1;
        }else{
            serverPlayer=player2;
            serveTarget=serveTarget2;
            player2._state=PlayerController.State.serverable;
            player1._state=PlayerController.State.hittable;

            turn = GameController.PlayerTurn.P2;
        }

        // Update ball position before reset
        UpdateInitialPosition();
        ballReset();

        // Reset player positions and states
        player1.transform.position=player1.defaultPosition;
        player2.transform.position=player2.defaultPosition;

        serverPlayer._state = PlayerController.State.serverable;
        // serverPlayer.setServerText(true);
        game=GameController.GameState.Going;

        player1.verticalVelocity=0;player1.horizontalVelocity=0;
        player2.verticalVelocity=0;player2.horizontalVelocity=0;

        player1.animator.SetFloat("VeloX",player1.horizontalVelocity);
        player1.animator.SetFloat("VeloY",player1.verticalVelocity);
        player2.animator.SetFloat("VeloX",player2.horizontalVelocity);
        player2.animator.SetFloat("VeloY",player2.verticalVelocity);

        player1.isHitting=false;
        player1.animator.SetBool("Hit",player1.isHitting);
        player2.isHitting=false;
        player2.animator.SetBool("Hit",player2.isHitting);
    }

    public void ServeHit()
    {
        if (!tossed) return;
        
        Vector3 fixedBallPos = initialPos + Vector3.up * tossHeight * 2.5f;
        hitDirection = (serveTarget.transform.position - fixedBallPos).normalized;
        finalForce = hitDirection * hitStrength + Vector3.up * offset;
        ballRb.linearVelocity = finalForce;
        ballRb.AddForce(Vector3.down * spinAmount);
        
        // After successful serve, ball becomes hittable
        serverPlayer._state = PlayerController.State.hittable;
        serverPlayer.isServing = false;
        tossed = false;

        // Start recording after serve
        if (replaySystem != null)
        {
            replaySystem.StartRecording();
        }
    }

    public void ballToss()
    {
        if (tossed) return;
        
        // Update position before toss
        UpdateInitialPosition();
        
        gameObject.SetActive(true);
        tossed = true;
        ballRb.isKinematic = false;
        
        // Set position and reset velocity
        transform.position = initialPos;
        ballRb.linearVelocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;

        // Calculate toss with more controlled values
        Vector3 tossDirection = new Vector3(0, 1, 0.2f).normalized;
        float tossForce = 8f;
        
        ballRb.AddForce(tossDirection * tossForce, ForceMode.Impulse);
    }

    public void ballReset()
    {
        UpdateInitialPosition();
        transform.position = initialPos;
        gameObject.SetActive(false);
        
        if (ballRb != null)
        {
            ballRb.isKinematic = true;
            ballRb.linearVelocity = Vector3.zero;
            ballRb.angularVelocity = Vector3.zero;
        }
        
        tossed = false;
    }
}
