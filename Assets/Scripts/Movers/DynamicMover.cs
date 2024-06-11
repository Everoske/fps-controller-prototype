using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicMover : MonoBehaviour
{
    [SerializeField]
    private WaypointPath path;

    [SerializeField]
    private float speed = 5f;

    private Rigidbody moverRB;

    private int targetWaypointIndex;

    private Transform previousWaypoint;
    private Transform targetWaypoint;

    private void Awake()
    {
        moverRB = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        TargetNextWaypoint();
    }

    private void FixedUpdate()
    {
        float distance = Vector3.Distance(transform.position, targetWaypoint.position);

        if (distance > 0.1f)
        {
            Vector3 directionToTravel = (targetWaypoint.position - transform.position).normalized;

            moverRB.MovePosition(transform.position + (directionToTravel * speed * Time.fixedDeltaTime));
        }
        else
        {
            Vector3 directionToTravel = (targetWaypoint.position - transform.position).normalized;

            moverRB.MovePosition(transform.position + (directionToTravel * distance));
            TargetNextWaypoint();
        }
    }

    private void TargetNextWaypoint()
    {
        previousWaypoint = path.GetWaypoint(targetWaypointIndex);
        targetWaypointIndex = path.GetNextWaypointIndex(targetWaypointIndex);
        targetWaypoint = path.GetWaypoint(targetWaypointIndex);

        float distanceToWaypoint = Vector3.Distance(previousWaypoint.position, targetWaypoint.position);
    }
}
