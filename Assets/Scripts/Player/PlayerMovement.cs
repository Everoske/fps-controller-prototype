using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR;

/*
 * Name: Player Movement
 * Purpose: This class handles moving the player 
 */
namespace FPSPrototype.Player
{
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Collision Handling")]
        [Tooltip("This determines the max number of collision points checked")]
        [SerializeField]
        private int maxBounces = 5;
        [Tooltip("This represents a buffer between the player's collider and external collisions")]
        [SerializeField]
        private float skinWidth = 0.015f;
        [SerializeField]
        private LayerMask collisionMask;

        // Represents bounds of the player's collider
        private Bounds bounds;

        [Header("Movement")]
        [SerializeField]
        private float walkSpeed = 8.0f;
        [SerializeField]
        private float sprintSpeed = 12.0f;

        // Needed for positioning the player
        private float halfHeight;

        [Header("Gravity and Ground")]
        [SerializeField]
        private float baseGravity = -9.8f;
        [SerializeField]
        private float terminalVelocity = -98f;
        [SerializeField]
        private float gravityIncrement = -4.4f;
        [SerializeField]
        private float gravityGrounded = -1.0f;
        [SerializeField]
        private float groundCheckTolerance = 0.015f;

        private RaycastHit groundHit;

        [Header("Slope Handling")]
        [SerializeField]
        private float maxSlopeAngle = 55f;
        [Tooltip("How fast the player slides down steep slopes")]
        [SerializeField]
        private float slideDownMultiplier = 5f;

        private void Awake()
        {
            bounds = GetComponent<CapsuleCollider>().bounds;
            halfHeight = bounds.extents.y / 2;

            // Shrink the bounds of the capsule collider by the skin width
            bounds.Expand(-2 * skinWidth);
        }

        public void ProcessMove(Vector2 movementInput)
        {
            // Create movement vector based on player input and the current angle
            Vector3 currentMovement = new Vector3();
            currentMovement += transform.forward * movementInput.y;
            currentMovement += transform.right * movementInput.x;

#if UNITY_EDITOR
            Debug.DrawRay(transform.position, currentMovement, Color.red, 0.5f);
#endif

            // Clamp the magnitude of the movement vector to 1f
            currentMovement = Vector3.ClampMagnitude(currentMovement, 1f);
            currentMovement = currentMovement * walkSpeed * Time.deltaTime;

            // Move the character based on all parameters
            Move(currentMovement);
        }

        /// <summary>
        /// Moves the character based on given input
        /// </summary>
        /// <param name="movementInput">Character input</param>
        public void Move(Vector3 movementInput)
        {
            Vector3 footPosition = new Vector3(transform.position.x, transform.position.y - halfHeight, transform.position.z);
            Vector3 headPosition = new Vector3(transform.position.x, transform.position.y + halfHeight, transform.position.z);

            Vector3 processedMovement = CollideAndSlide(movementInput, footPosition, headPosition, 0, false, movementInput);
            if (!IsGrounded())
            {
                // Vector3 gravityInAir = ProcessGravity();
                Vector3 gravityInAir = new Vector3(0f, baseGravity * Time.deltaTime, 0f);
                processedMovement += CollideAndSlide(gravityInAir, footPosition + processedMovement, headPosition + processedMovement,
                    0, true, gravityInAir);
            } 
            else
            {
                Vector3 gravityOnGround = new Vector3(0f, gravityGrounded * Time.deltaTime, 0f);
                processedMovement += CollideAndSlide(gravityOnGround, footPosition + processedMovement, headPosition + processedMovement,
                    0, true, gravityOnGround);
            }

            transform.position += processedMovement;
        }


        /// <summary>
        /// Handles movement while account for collision using swept spheres
        /// If collision occurs within a given movement, the remaining movement is
        /// projected along the collision plane. This cycle repeats recursively until
        /// there are no more collisions or the recursion reaches the max number of iterations
        /// The final return value is the results of adding all projected vectors
        /// </summary>
        /// <param name="givenVelocity">Intended velocity</param>
        /// <param name="footPosition">Bottom position of character used for capsule cast</param>
        /// <param name="headPosition">Top position of character used for capsule cast</param>
        /// <param name="depth">Current level of recursion</param>
        /// <param name="gravityPass">Used for gravity-based calculations</param>
        /// <param name="initialVelocity">Original given velocity</param>
        /// <returns>Vector3 representing the adjusted velocity based on collisions</returns>
        private Vector3 CollideAndSlide(Vector3 givenVelocity, Vector3 footPosition, Vector3 headPosition,
            int depth, bool gravityPass, Vector3 initialVelocity)
        {
            // Used to break recursion
            if (depth >= maxBounces)
            {
                return Vector3.zero;
            }

            float distance = givenVelocity.magnitude + skinWidth;

            // Swept capsule is used to check for collisions
            if (Physics.CapsuleCast(footPosition, headPosition, bounds.extents.x, givenVelocity.normalized,
                out RaycastHit hit, distance, collisionMask))
            {
                // Distance the player has to travel before touching the surface
                Vector3 snapToSurface = givenVelocity.normalized * (hit.distance - skinWidth);

                // Leftover velocity after snapping to surface
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
                    // Gravity is being processed, do not slide on shallow slopes
                    if (gravityPass)
                    {
                        return snapToSurface;
                    }
                }

                // Project leftover velocity across the collision's surface
                leftover = ProjectAndScale(leftover, hit.normal);

                // Return current level of collide and slide and add previous iteration to it
                return snapToSurface + CollideAndSlide(leftover, footPosition + snapToSurface, headPosition + snapToSurface,
                    depth + 1, gravityPass, initialVelocity); 
            }

            return givenVelocity;
        }

        /// <summary>
        /// Projects the given velocity across the surface of the given normal and
        /// returns the result
        /// </summary>
        /// <param name="givenVelocity">Velocity to project across plane</param>
        /// <param name="givenNormal">The normal of the surface to project against</param>
        /// <returns>A new velocity vector projected across the plane of the given normal</returns>
        private Vector3 ProjectAndScale(Vector3 givenVelocity, Vector3 givenNormal)
        {
            float magnitudeLeft = givenVelocity.magnitude;

            // Find the unit vector along the collision plane
            givenVelocity = Vector3.ProjectOnPlane(givenVelocity, givenNormal).normalized;
            // Multiply this unit vector by the leftover velocity
            givenVelocity *= magnitudeLeft;
            return givenVelocity;
        }

        /// <summary>
        /// Determines if the character is grounded
        /// </summary>
        /// <returns>True if grounded, false otherwise</returns>
        private bool IsGrounded()
        {
            float sphereCastRadius = bounds.extents.x;
            Vector3 playerCenterPoint = new Vector3(transform.position.x, transform.position.y, transform.position.z);
            Physics.SphereCast(playerCenterPoint, sphereCastRadius, Vector3.down, out groundHit);
            float playerCenterToGroundDistance = groundHit.distance + sphereCastRadius;


            return (playerCenterToGroundDistance >= bounds.extents.y + skinWidth - groundCheckTolerance) &&
                (playerCenterToGroundDistance <= bounds.extents.y + skinWidth + groundCheckTolerance);
        }
    }
}
