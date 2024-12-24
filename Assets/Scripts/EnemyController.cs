using UnityEngine;
using System.Collections.Generic;

public class EnemyAI : MonoBehaviour
{
    private Rigidbody rb;
    private Transform playerTransform;

    [Header("Flight")]
    [SerializeField] private float maxSpeed = 30f;
    [SerializeField] private float minSpeed = 20f;
    [SerializeField] private float turnSmoothSpeed = 2f;

    [Header("Combat Positioning")]
    [SerializeField] private float idealFollowDistance = 5f;
    [SerializeField] private float minFollowDistance = 3f;
    [SerializeField] private float maxFollowDistance = 30f;
    [SerializeField] private float verticalOffset = 2f;
    [SerializeField] private float horizontalOffset = 3f;

    [Header("Reaction Settings")]
    [SerializeField] private float positionUpdateRate = 0.5f;
    [SerializeField] private float targetingSmoothTime = 1.5f;
    [SerializeField] private float reactionDelay = 0.5f;
    [SerializeField] private int queueCapacity = 60;

    [Header("Engagement")]
    [SerializeField] private float attackRange = 40f;
    [SerializeField] private float attackAngle = 15f;
    [SerializeField] private float disengageDistance = 120f;

    private Vector3 targetPosition;
    private Vector3 smoothedDirection;
    private float currentSpeed;
    private float positionUpdateTimer;
    private Vector3 smoothedTargetPosition;
    private Vector3 targetPositionVelocity;

    private Queue<PositionData> positionQueue;
    private float timePerFrame;

    private struct PositionData
    {
        public Vector3 position;
        public Vector3 forward;
        public float timestamp;

        public PositionData(Vector3 pos, Vector3 fwd, float time)
        {
            position = pos;
            forward = fwd;
            timestamp = time;
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Find all objects with Player tag
        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject playerObject in playerObjects)
        {
            // Skip if it's this object
            if (playerObject == gameObject)
                continue;

            // Found valid target, assign and break
            playerTransform = playerObject.transform;
            break;
        }

        // Check if we found a target
        if (playerTransform == null)
        {
            Debug.LogError("Enemy AI: Could not find valid player target!");
        }


        currentSpeed = maxSpeed;
        smoothedDirection = transform.forward;
        positionUpdateTimer = positionUpdateRate;

        positionQueue = new Queue<PositionData>(queueCapacity);
        timePerFrame = positionUpdateRate / queueCapacity;

        positionQueue.Enqueue(new PositionData(
            playerTransform.position,
            playerTransform.forward,
            Time.time
        ));
    }

    void FixedUpdate()
    {
        if (playerTransform == null) return;

        UpdatePositionQueue();

        positionUpdateTimer -= Time.fixedDeltaTime;

        if (positionUpdateTimer <= 0)
        {
            positionUpdateTimer = positionUpdateRate;
            ProcessDelayedPosition();
        }

        smoothedTargetPosition = Vector3.SmoothDamp(
            smoothedTargetPosition,
            targetPosition,
            ref targetPositionVelocity,
            targetingSmoothTime
        );

        Vector3 toSmoothTarget = smoothedTargetPosition - transform.position;
        UpdateMovement(toSmoothTarget.magnitude);
    }

    void UpdatePositionQueue()
    {
        positionQueue.Enqueue(new PositionData(
            playerTransform.position,
            playerTransform.forward,
            Time.time
        ));

        while (positionQueue.Count > queueCapacity)
        {
            positionQueue.Dequeue();
        }
    }

    void ProcessDelayedPosition()
    {
        float targetTime = Time.time - reactionDelay;
        PositionData delayedData = new PositionData();
        bool foundPosition = false;

        foreach (var data in positionQueue)
        {
            if (data.timestamp >= targetTime)
            {
                delayedData = data;
                foundPosition = true;
                break;
            }
        }

        if (!foundPosition)
        {
            delayedData = positionQueue.Peek();
        }

        Vector3 toPlayer = delayedData.position - transform.position;
        UpdateTargetPosition(toPlayer, delayedData.forward, toPlayer.magnitude);
    }

    void UpdateTargetPosition(Vector3 toPlayer, Vector3 playerForward, float distance)
    {
        Vector3 targetPlayerPos = transform.position + toPlayer;
        Vector3 behindPlayer = targetPlayerPos - playerForward * idealFollowDistance;

        float timeOffset = Mathf.Sin(Time.time * 0.5f);
        Vector3 offset = Vector3.right * horizontalOffset * timeOffset +
                        Vector3.up * verticalOffset * Mathf.Cos(Time.time * 0.3f);

        if (distance < minFollowDistance)
        {
            behindPlayer = targetPlayerPos - toPlayer.normalized * (minFollowDistance * 1.5f);
        }
        else if (distance > maxFollowDistance)
        {
            behindPlayer = targetPlayerPos;
        }

        targetPosition = behindPlayer + offset;
    }

    void UpdateMovement(float distanceToPlayer)
    {
        // Target direction for movement
        Vector3 desiredDirection = (smoothedTargetPosition - transform.position).normalized;

        // Try to point nose at the actual player position for better aiming
        Vector3 toPlayer = playerTransform.position - transform.position;
        Vector3 aimDirection = Vector3.Lerp(desiredDirection, toPlayer.normalized, 0.5f);

        // Smoothly interpolate direction
        smoothedDirection = Vector3.Slerp(smoothedDirection, aimDirection, turnSmoothSpeed * Time.fixedDeltaTime);

        // Adjust speed based on angle to target
        float angleToTarget = Vector3.Angle(transform.forward, desiredDirection);
        float speedMultiplier = Mathf.Lerp(0.8f, 1f, angleToTarget / 180f);
        currentSpeed = Mathf.Lerp(minSpeed, maxSpeed, speedMultiplier);

        // Apply movement
        rb.velocity = transform.forward * currentSpeed;

        // Smooth rotation to face target
        Quaternion targetRotation = Quaternion.LookRotation(smoothedDirection, Vector3.up);
        rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, turnSmoothSpeed * Time.fixedDeltaTime);
    }

    void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(targetPosition, 2f);
            Gizmos.DrawLine(transform.position, targetPosition);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(smoothedTargetPosition, 1.5f);
        }
    }
}