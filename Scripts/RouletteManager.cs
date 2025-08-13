using UnityEngine;

public class RouletteManager : MonoBehaviour
{
    public Transform wheel;        
    public Transform ball;         
    public float idleWheelSpeed = 45f;
    public float spinWheelSpeed = 260f;
    public float wheelFriction = 25f;

    private float currentWheelSpeed;
    private bool spinning;
    private Vector3 ballStartPos;

    void Awake()
        {
            if (ball != null) ball.SetParent(null, true); 
        }

    void Start()
    {
        ballStartPos = ball.position;
        EnterIdle();
    }

    void Update()
    {
        if (currentWheelSpeed != 0f)
        {
            wheel.Rotate(0, 0, -currentWheelSpeed * Time.deltaTime);
            if (spinning)
            {
                currentWheelSpeed = Mathf.MoveTowards(currentWheelSpeed, 0f, wheelFriction * Time.deltaTime);
            }
        }
    }

    void EnterIdle()
    {
        spinning = false;
        currentWheelSpeed = idleWheelSpeed;
        ball.position = ballStartPos;
    }

    public void PlaySpin()
    {
        spinning = true;
        currentWheelSpeed = spinWheelSpeed;

        var rb = ball.GetComponent<Rigidbody2D>();
        rb.isKinematic = false;
        rb.linearVelocity = Random.insideUnitCircle.normalized * 5f;
    }

    public void OnBallLanded(int number)
    {
        Debug.Log("Ganador: " + number);
        Invoke(nameof(EnterIdle), 2f);
    }
}
