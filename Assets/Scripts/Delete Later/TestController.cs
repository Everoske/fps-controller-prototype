using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.HID;

public class TestController : MonoBehaviour
{
    [SerializeField]
    private int maxBounces = 5;
    [SerializeField]
    private float skinWidth = 0.015f;
    [SerializeField]
    private LayerMask collisionMask;
    [SerializeField]
    private float maxSlopeAngle = 55f;

    private float halfHeight;
    private Bounds bounds;

    [SerializeField]
    private float gravity = -9.6f;
    [SerializeField]
    private float gravityGrounded = -1.0f;
    [SerializeField]
    private float groundCheckTolerance = 0.015f;

    [SerializeField]
    private float slideDownMultiplier = 5f;

    private float groundCheckRadius = 0f;
    private RaycastHit groundHit;
    public bool isPlayerGrounded = false;

    private void Awake()
    {
        bounds = GetComponent<Collider>().bounds;
        halfHeight = bounds.extents.y / 2;
        bounds.Expand(-2 * skinWidth);
    }

    public void Move(Vector3 movementInput)
    {
        Vector3 gravityInAir = new Vector3(0f, gravity * Time.deltaTime, 0f);
        Vector3 gravityOnGround = new Vector3(0f, gravityGrounded * Time.deltaTime, 0f);

        Vector3 footPosition = new Vector3(transform.position.x, transform.position.y - halfHeight, transform.position.z);
        Vector3 headPosition = new Vector3(transform.position.x, transform.position.y + halfHeight, transform.position.z);

        movementInput = ProcessSlope(movementInput);

        Vector3 processedMovement = CollideAndSlide(movementInput, footPosition, headPosition, 0, false, movementInput);
        if (!IsGrounded())
        {
            processedMovement += CollideAndSlide(gravityInAir, footPosition + processedMovement, headPosition + processedMovement, 0, true, gravityInAir);
        }
        else
        {
            processedMovement += CollideAndSlide(gravityOnGround, footPosition + processedMovement, headPosition + processedMovement, 0, true, gravityOnGround);
        }

        transform.position += processedMovement;
    }

    public Vector3 CollideAndSlide(Vector3 givenVelocity, Vector3 givenFootPosition, Vector3 givenHeadPosition, int depth, bool gravityPass, Vector3 initialVelocity)
    {
        // Used to break recursion
        if (depth >= maxBounces)
        {
            return Vector3.zero;
        }

        float distance = givenVelocity.magnitude + skinWidth;

        // Swept capsule is used to check for collisions
        if (Physics.CapsuleCast(givenFootPosition, givenHeadPosition, bounds.extents.x, givenVelocity.normalized, out RaycastHit hit, distance, collisionMask))
        {
            // This causes issues when going up and to the side on slopes
            // The hit.distance is usually small resulting in a negative snapToSurface
            // The player can still move but much slower
            // Represents the distance the player has to travel before touching the surface
            Vector3 snapToSurface = givenVelocity.normalized * (hit.distance - skinWidth);

            // Left over velocity after the surface
            Vector3 leftover = givenVelocity - snapToSurface;

            // Angle of the collider's surface
            float angle = Vector3.Angle(Vector3.up, hit.normal);

            // Ensures snap to surface distance is greater than skin width
            if (snapToSurface.magnitude <= skinWidth)
            {
                snapToSurface = Vector3.zero;
            }

            // Normal ground / slope
            if (angle <= maxSlopeAngle)
            {
                // Gravity is being processed
                // Do not slide on shallow slopes
                if (gravityPass)
                {
                    return snapToSurface;
                }
            }

            leftover = ProjectAndScale(leftover, hit.normal);

            // Return current level of collide and slide and add the previous iteration to it
            return snapToSurface + CollideAndSlide(leftover, givenFootPosition + snapToSurface, givenHeadPosition + snapToSurface, depth + 1, gravityPass, initialVelocity);
        }

        return givenVelocity;
    }

    /*
     * Handles collision by detecting using a swept circle to detect colliders
     * Swept circle goes in the direction of the intended velocity. If a collision is detected, this algorithm
     * projects the remaining velocity on the plane. It is called recursively to check for other collisions until 
     * either there are no more collisions or the recursion reaches its max number of iterations
     * This algorithm returns the calculated velocity based on the sum of the velocity vectors
     */

    public Vector3 CollideAndSlide(Vector3 givenVelocity, Vector3 givenPosition, int depth, bool gravityPass, Vector3 initialVelocity)
    {
        // Used to break recursion
        if (depth >= maxBounces)
        {
            return Vector3.zero;
        }

        float distance = givenVelocity.magnitude + skinWidth;

        // Swept sphere is used to check for collisions
        if (Physics.SphereCast(givenPosition, bounds.extents.x, givenVelocity.normalized, out RaycastHit hit, distance, collisionMask))
        {
            // Represents the distance the player has to travel before touching the surface
            Vector3 snapToSurface = givenVelocity.normalized * (hit.distance - skinWidth);
            // Left over velocity after the surface
            Vector3 leftover = givenVelocity - snapToSurface;

            // Angle of the collider's surface
            float angle = Vector3.Angle(Vector3.up, hit.normal);

            // Ensures snap to surface distance is greater than skin width
            if (snapToSurface.magnitude <= skinWidth)
            {
                snapToSurface = Vector3.zero;
            }

            // Normal ground / slope
            if (angle <= maxSlopeAngle)
            {
                // Gravity is being processed
                // Do not slide on shallow slopes
                if (gravityPass)
                {
                    return snapToSurface;
                }
                leftover = ProjectAndScale(leftover, hit.normal);
            }
            // Wall or steep slope
            else
            {
                // Slows player down if they are moving directly into wall or slope that is too steep
                // Scale velocity based on the angle between the surface normal and the initial intended velocity
                float scale = 1 - Vector3.Dot(
                    new Vector3(hit.normal.x, 0, hit.normal.z).normalized,
                    -new Vector3(initialVelocity.x, 0, initialVelocity.z).normalized
                );

                leftover = ProjectAndScale(leftover, hit.normal) * scale;
            }

            // Return current level of collide and slide and add the previous iteration to it
            return snapToSurface + CollideAndSlide(leftover, givenPosition + snapToSurface, depth + 1, gravityPass, initialVelocity);
        }

        return givenVelocity;
    }

    /*
     * Returns a new velocity vector based on a plane with the same
     * magnitude as the given velocity
     */
    private Vector3 ProjectAndScale(Vector3 givenVelocity, Vector3 givenNormal)
    {
        float magnitudeLeft = givenVelocity.magnitude;

        // Find the unit vector along the collision plane  
        givenVelocity = Vector3.ProjectOnPlane(givenVelocity, givenNormal).normalized;
        // Multiply this unit vector by the leftover velocity
        givenVelocity *= magnitudeLeft;
        return givenVelocity;
    }

    private bool IsGrounded()
    {
        float sphereCastRadius = bounds.extents.x;
        Vector3 playerCenterPoint = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        Physics.SphereCast(playerCenterPoint, sphereCastRadius, Vector3.down, out groundHit);
        float playerCenterToGroundDistance = groundHit.distance + sphereCastRadius;

        isPlayerGrounded = (playerCenterToGroundDistance >= bounds.extents.y + skinWidth - groundCheckTolerance) &&
            (playerCenterToGroundDistance <= bounds.extents.y + skinWidth + groundCheckTolerance);

        return isPlayerGrounded;
    }

    private Vector3 ProcessSlope(Vector3 moveInput)
    {
        Vector3 slopeMovement = moveInput;
        

        if (isPlayerGrounded)
        {
            Vector3 groundNormal = transform.InverseTransformDirection(groundHit.normal);
            float groundSlopeAngle = Vector3.Angle(groundNormal, transform.up);

            if (groundSlopeAngle != 0)
            {
                Quaternion slopeAngleRotation = Quaternion.FromToRotation(transform.up, groundNormal);
                slopeMovement = slopeAngleRotation * slopeMovement;

                // If slope angle is larger than the max slope angle, add the negative slope vector to player movement
                if (groundSlopeAngle > maxSlopeAngle)
                {
                    // Gets a vector parallel to the slope
                    // It is normalized, multiplied by Time.deltaTime, and a slide down multiplier to increase the speed of the downward slide
                    Vector3 slideDownVector = Vector3.ProjectOnPlane(transform.up, groundHit.normal).normalized * slideDownMultiplier * Time.deltaTime;

                    // Subtract the slide down vector from the slope movement to get the final movement for the player 
                    slopeMovement = slopeMovement - slideDownVector;
                }
            }
        }

#if UNITY_EDITOR
        Debug.DrawRay(transform.position, transform.TransformDirection(slopeMovement), Color.red, 0.5f);
#endif

        return slopeMovement;
    }
}
