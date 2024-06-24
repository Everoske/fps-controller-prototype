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
    private float objectGroundedDistance = 1f;

    private Rigidbody moverRB;

    private int targetWaypointIndex;

    private float journeyLength = 0.0f;
    private int waypointsVisited = 0;
    private float timePassed = 0.0f;

    private Transform previousWaypoint;
    private Transform targetWaypoint;

    private List<CharacterController> controllers = new List<CharacterController>();
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
            ApplyCounteractingForce();
        }
    }

    private void MoveTowardTargetWaypoint()
    {
        timePassed += Time.fixedDeltaTime;
        float distanceCovered = timePassed * speed;

        float journeyPercentage = distanceCovered / journeyLength;

        bool platformMovingDown = targetWaypoint.position.y < transform.position.y;

        Vector3 movementVector = Vector3.Lerp(previousWaypoint.position, targetWaypoint.position, journeyPercentage);
        Vector3 riderMovement = movementVector - transform.position;

        if (!platformMovingDown)
        {
            MoveRidersBefore(riderMovement);
            moverRB.MovePosition(movementVector);
            ApplyCounteractingForce();
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
        foreach (CharacterController controller in controllers)
        {
            if (ConnectedControllerGrounded(controller))
            {
                controller.Move(movementVector);
            }         
        }

        Vector3 bodyMovement = new Vector3(movementVector.x, 0f, movementVector.z);

        foreach (Rigidbody body in connectedBodies)
        {
            //body.Move(body.position + bodyMovement, body.rotation);
        }
    }

    private void MoveRiders(Vector3 movementVector)
    {
        foreach (CharacterController controller in controllers)
        {
            controller.Move(movementVector);
        }

        foreach (Rigidbody body in connectedBodies)
        {
            body.Move(body.position + movementVector, body.rotation);
        }
    }

    private bool ConnectedControllerGrounded(CharacterController controller)
    {
        float verticalDistance = (controller.transform.position.y - (controller.height / 2 + controller.skinWidth)) - moverRB.position.y;

        return verticalDistance < objectGroundedDistance;
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
        if (other.TryGetComponent<CharacterController>(out CharacterController controller))
        {
            controllers.Add(controller);
        } 
        else if (other.TryGetComponent<Rigidbody>(out Rigidbody otherRigidbody))
        {
            connectedBodies.Add(otherRigidbody);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<CharacterController>(out CharacterController controller))
        {
            controllers.Remove(controller);
        }
        else if (other.TryGetComponent<Rigidbody>(out Rigidbody otherRigidbody))
        {
            connectedBodies.Remove(otherRigidbody);
        }
    }
}
