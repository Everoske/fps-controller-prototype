using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FPSPrototype.Player
{
    public class MovementController : MonoBehaviour
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

        private Bounds bounds;

        private float halfHeight;

        [Header("Gravity")]
        [SerializeField]
        private float baseGravity = -19.6f;
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
        [Tooltip("How fast the character slides down steep slopes")]
        [SerializeField]
        private float slideDownMultiplier = 5f;
        [SerializeField]
        private bool slopeHandlingEnabled = false;

        private void Awake()
        {
            bounds = GetComponent<CapsuleCollider>().bounds;
            halfHeight = bounds.extents.y / 2;

            // Shrink the bounds of the capsule collider by the skin width
            bounds.Expand(-2 * skinWidth);
        }

        /// <summary>
        /// Moves the character based on given input
        /// </summary>
        /// <param name="movementInput">Character input</param>
        public void Move(Vector3 movementInput)
        {
            Vector3 footPosition = new Vector3(transform.position.x, transform.position.y - halfHeight, transform.position.z);
            Vector3 headPosition = new Vector3(transform.position.x, transform.position.y + halfHeight, transform.position.z);

            if (slopeHandlingEnabled)
            {
                movementInput = ProcessSlope(movementInput);
            }

            Vector3 processedMovement = CollideAndSlide(movementInput, footPosition, headPosition, 0, false, movementInput);

            // Handle slide down here?
            // If sliding, apply no gravity

            Vector3 gravityToApply = ProcessGravity();
            processedMovement += CollideAndSlide(gravityToApply, footPosition + processedMovement, headPosition + processedMovement, 0, true, gravityToApply);

#if UNITY_EDITOR
            Debug.DrawRay(transform.position, processedMovement, Color.red, 0.5f);
#endif

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

                Vector3 leftover = givenVelocity - snapToSurface;

                float angle = Vector3.Angle(Vector3.up, hit.normal);

                if (snapToSurface.magnitude <= skinWidth)
                {
                    snapToSurface = Vector3.zero;
                }

                if (angle <= maxSlopeAngle)
                {
                    if (gravityPass)
                    {
                        return snapToSurface;
                    }

                    leftover = ProjectAndScale(leftover, hit.normal);
                }
                else
                {
                    float scale = 1 - Vector3.Dot(
                        new Vector3(hit.normal.x, 0, hit.normal.z).normalized,
                        -new Vector3(initialVelocity.x, 0, initialVelocity.z).normalized
                        );

                    leftover = ProjectAndScale(leftover, hit.normal) * scale;
                }
                

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

            givenVelocity = Vector3.ProjectOnPlane(givenVelocity, givenNormal).normalized;
            givenVelocity *= magnitudeLeft;
            return givenVelocity;
        }

        private Vector3 ProcessGravity()
        {
            Vector3 gravityToApply = Vector3.zero;
            if (!IsGrounded())
            {
                gravityToApply = new Vector3(0f, baseGravity * Time.deltaTime, 0f);
            }
            else
            {
                gravityToApply = new Vector3(0f, gravityGrounded * Time.deltaTime, 0f);
            }
            return gravityToApply;
        }

        private Vector3 ProcessSlope(Vector3 moveInput)
        {
            Vector3 slopeMovement = moveInput;

            if (IsGrounded())
            {
                Vector3 groundNormal = transform.InverseTransformDirection(groundHit.normal);
                float groundSlopeAngle = Vector3.Angle(groundNormal, transform.up);

                if (groundSlopeAngle != 0)
                {
                    Quaternion slopeAngleRotation = Quaternion.FromToRotation(transform.up, groundNormal);
                    slopeMovement = slopeAngleRotation * slopeMovement;

                    if (groundSlopeAngle > maxSlopeAngle)
                    {
                        Vector3 slideDownVector = Vector3.ProjectOnPlane(transform.up, groundHit.normal).normalized * slideDownMultiplier * Time.deltaTime;

                        slopeMovement = slopeMovement - slideDownVector;
                    }
                }
            }

            return slopeMovement;
        }


        /// <summary>
        /// Determines if the character is grounded
        /// </summary>
        /// <returns>True if grounded, false otherwise</returns>
        public bool IsGrounded()
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
