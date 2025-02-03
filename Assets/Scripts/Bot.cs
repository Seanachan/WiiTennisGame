using UnityEngine;

public class Bot : MonoBehaviour
{
    // Add PlayerController reference
    public PlayerController player;
    public Ball ball;
    public float moveSpeed = 5f;
    public float hitDistance = 2f;
    public Animator animator;
    
    private Vector3 botInitialPos = new Vector3(-6.63f, 0.65f, 28.2f);
    private Vector3 targetPos;
    private bool isHitting = false;
    private readonly float courtMinX = -13.75f,courtMaxX = 8.0f, courtMinZ = 6.21f, courtMaxZ = 31.05f;
    private Vector3 serveReceivePos;
    
    void Start()
    {
        transform.position = botInitialPos;
        animator = GetComponent<Animator>();
        targetPos = botInitialPos;
        serveReceivePos = new Vector3(0f, botInitialPos.y, 20f); // Middle court position for receiving serve
    }

    void Update()
    {
        // Only move when ball is served or in play
        if ((player.isServing && !ball.tossed) || (ball.gameObject.activeSelf && player._state == PlayerController.State.hittable))
        {
            if (player.isServing)
            {
                // Move to receive position when player is serving
                MoveToPosition(serveReceivePos);
            }
            else if (ball.gameObject.activeSelf)
            {
                PredictBallLanding();
                MoveToBall();
                HandleHitting();
            }
        }
        else
        {
            // Stay at initial position until ball is served
            transform.position = botInitialPos;
            animator.SetFloat("VeloX", 0);
            animator.SetFloat("VeloY", 0);
        }
    }

    void PredictBallLanding()
    {
        if (ball.gameObject.activeSelf)
        {
            Rigidbody ballRb = ball.GetComponent<Rigidbody>();
            Vector3 ballVelocity = ballRb.linearVelocity;
            Vector3 ballPos = ball.transform.position;
            
            // Simple prediction of where the ball will land
            float timeToLand = (-ballVelocity.y + Mathf.Sqrt(ballVelocity.y * ballVelocity.y + 
                2f * Physics.gravity.magnitude * (ballPos.y - transform.position.y))) / Physics.gravity.magnitude;
            
            Vector3 landingPos = ballPos + new Vector3(
                ballVelocity.x * timeToLand,
                0f,
                ballVelocity.z * timeToLand
            );

            // Clamp the target position within court boundaries
            targetPos = new Vector3(
                Mathf.Clamp(landingPos.x, courtMinX, courtMaxX),
                transform.position.y,
                Mathf.Clamp(landingPos.z, courtMinZ, courtMaxZ)
            );
        }
    }

    void MoveToBall()
    {
        Vector3 moveDirection = (targetPos - transform.position).normalized;
        float distanceToTarget = Vector3.Distance(transform.position, targetPos);

        if (distanceToTarget > 0.1f)
        {
            transform.position += moveDirection * moveSpeed * Time.deltaTime;
            
            // Update animator parameters
            animator.SetFloat("VeloX", moveDirection.x);
            animator.SetFloat("VeloY", moveDirection.z);
        }
        else
        {
            animator.SetFloat("VeloX", 0);
            animator.SetFloat("VeloY", 0);
        }
    }

    void HandleHitting()
    {
        float distanceToBall = Vector3.Distance(transform.position, ball.transform.position);
        
        if (distanceToBall < hitDistance && ball.transform.position.z > courtMinZ)
        {
            if (!isHitting)
            {
                isHitting = true;
                animator.SetBool("Hit", true);
                
                // Determine backhand or forehand based on ball position
                bool isBackhand = (ball.transform.position - transform.position).x >= 1;
                animator.SetBool("backHand", isBackhand);
            }
        }
        else
        {
            isHitting = false;
            animator.SetBool("Hit", false);
        }
    }

    void MoveToPosition(Vector3 position)
    {
        Vector3 moveDirection = (position - transform.position).normalized;
        float distanceToTarget = Vector3.Distance(transform.position, position);

        if (distanceToTarget > 0.1f)
        {
            transform.position += moveDirection * moveSpeed * Time.deltaTime;
            animator.SetFloat("VeloX", moveDirection.x);
            animator.SetFloat("VeloY", moveDirection.z);
        }
        else
        {
            animator.SetFloat("VeloX", 0);
            animator.SetFloat("VeloY", 0);
        }
    }
}
