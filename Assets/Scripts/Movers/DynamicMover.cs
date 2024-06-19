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

    private Rigidbody moverRB;

    private int targetWaypointIndex;

    private float startTime = 0.0f;
    private float journeyLength = 0.0f;
    private int waypointsVisited = 0;
    private float timePassed = 0.0f;


    private Vector3 previousPosition;
    private Transform previousWaypoint;
    private Transform targetWaypoint;

    private List<CharacterController> controllers = new List<CharacterController>();

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
            MoveRiders(riderMovement);
            moverRB.MovePosition(movementVector);
        } else
        {
            moverRB.MovePosition(movementVector);
            MoveRiders(riderMovement);
        }
    }

    private void MoveRiders(Vector3 movementVector)
    {
        foreach (CharacterController controller in controllers)
        {
            controller.Move(movementVector);
        }
    }

    private void MoveRiders()
    {
        foreach (CharacterController controller in controllers)
        {
            controller.Move(moverRB.velocity * Time.fixedDeltaTime);
        }
    }

    private void AdjustRiderPosition()
    {
        foreach (CharacterController controller in controllers)
        {
            float adjustedControllerY = controller.transform.position.y - (controller.height / 2 + controller.skinWidth);
            float heightDifference = adjustedControllerY - transform.position.y;

            if (heightDifference < 0.5)
            {
                controller.Move(transform.up * -heightDifference);
            }

            
        }
    }

    private void MoveRidersAfter()
    {
        foreach (CharacterController controller in controllers)
        {

            float relativeControllerY = controller.transform.position.y - (controller.height / 2 + controller.skinWidth);
            float moveDistanceY = transform.position.y - relativeControllerY;

            Vector3 moveVector = new Vector3(moverRB.velocity.x * Time.fixedDeltaTime, moveDistanceY, moverRB.velocity.z * Time.fixedDeltaTime);

            controller.Move(moveVector);
        }
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
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<CharacterController>(out CharacterController controller))
        {
            controllers.Remove(controller);
        }
    }
}
