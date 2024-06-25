using FPSPrototype.Player;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicMover : MonoBehaviour
{
    [SerializeField]
    private WaypointPath path;

    [SerializeField]
    private bool shouldLoop;

    [SerializeField]
    private float speed = 5f;

    [Tooltip("Threshold for which an object on the moving platform is considered grounded")]
    [SerializeField]
    private float groundedTolerance = 0.05f;

    private Rigidbody moverRB;

    private int targetWaypointIndex;

    private float journeyLength = 0.0f;
    private int waypointsVisited = 0;
    private float timePassed = 0.0f;

    private Transform previousWaypoint;
    private Transform targetWaypoint;

    private bool platformMovingDown = false;

    private List<PlayerMovement> controllers = new List<PlayerMovement>();
    private List<Rigidbody> connectedBodies = new List<Rigidbody>();

    private void Awake()
    {
        moverRB = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        TargetNextWaypoint();
        DetermineJourneyLength();
    }

    private void FixedUpdate()
    {
        if (transform.position != targetWaypoint.position)
        {
            MoveTowardTargetWaypoint();
        }
        else
        {
            TargetNextWaypoint();
            if (!platformMovingDown)
            {
                ApplyCounteractingForce();
            }
        }
    }

    private void MoveTowardTargetWaypoint()
    {
        timePassed += Time.fixedDeltaTime;
        float distanceCovered = timePassed * speed;

        float journeyPercentage = distanceCovered / journeyLength;

        platformMovingDown = targetWaypoint.position.y < transform.position.y;

        Vector3 movementVector = Vector3.Lerp(previousWaypoint.position, targetWaypoint.position, journeyPercentage);
        Vector3 riderMovement = movementVector - transform.position;

        if (!platformMovingDown)
        {
            MoveRidersBefore(riderMovement);
            moverRB.MovePosition(movementVector);
            if (Vector3.Distance(targetWaypoint.position, riderMovement) < movementVector.magnitude)
            {
                ApplyCounteractingForce();
            }
        } else
        {
            moverRB.MovePosition(movementVector);
            MoveRiders(riderMovement);
        }
    }

    private void ApplyCounteractingForce()
    {
        foreach (Rigidbody body in connectedBodies)
        {
            if (body.velocity.y > 0)
            {
                body.AddForce(new Vector3(0f, -body.velocity.y, 0f), ForceMode.VelocityChange);
            }
        }
    }

    private void MoveRidersBefore(Vector3 movementVector)
    {
        foreach (PlayerMovement character in controllers)
        {
            if (ConnectedControllerGrounded(character))
            {
                character.ApplyExternalMovement(movementVector);
            }         
        }
    }

    private void MoveRiders(Vector3 movementVector)
    {
        foreach (PlayerMovement character in controllers)
        {
            character.ApplyExternalMovement(movementVector);
        }

        foreach (Rigidbody body in connectedBodies)
        {
            body.Move(body.position + movementVector, body.rotation);
        }
    }

    private bool ConnectedControllerGrounded(PlayerMovement character)
    {
        float verticalDistance = character.transform.position.y - moverRB.position.y;
        float groundedDistance = character.GetDistanceToGround() + groundedTolerance;

        return verticalDistance < groundedDistance;
    }

    private void DetermineJourneyLength()
    {
        journeyLength = Vector3.Distance(previousWaypoint.position, targetWaypoint.position);
    }

    private void TargetNextWaypoint()
    {
        if (waypointsVisited >= path.GetTotalWaypoints() - 1 && !shouldLoop) return;

        previousWaypoint = path.GetWaypoint(targetWaypointIndex);
        targetWaypointIndex = path.GetNextWaypointIndex(targetWaypointIndex);
        targetWaypoint = path.GetWaypoint(targetWaypointIndex);
        timePassed = 0.0f;

        waypointsVisited++;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<PlayerMovement>(out PlayerMovement character))
        {
            controllers.Add(character);
        } 
        else if (other.TryGetComponent<Rigidbody>(out Rigidbody otherRigidbody))
        {
            connectedBodies.Add(otherRigidbody);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<PlayerMovement>(out PlayerMovement character))
        {
            controllers.Remove(character);
        }
        else if (other.TryGetComponent<Rigidbody>(out Rigidbody otherRigidbody))
        {
            connectedBodies.Remove(otherRigidbody);
        }
    }
}
