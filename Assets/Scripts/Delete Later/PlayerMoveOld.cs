using FPSPrototype.Input;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

/*
 * Contains the logic for moving the player in the overworld
 * Include walking, sprinting, crouching, jumping, and falling
 * Certain actions should be disabled during build mode
 */

namespace FPSPrototype.Delete
{

    public class PlayerMoveOld : MonoBehaviour
    {
        [Header("Movement Speed Settings")]
        [SerializeField]
        private float walkSpeed = 8;
        [SerializeField]
        private float sprintSpeed = 12;
        [SerializeField]
        private float crouchSpeed = 4;
        [SerializeField]
        private float offGroundSpeed = 1;

        private float currentSpeed = 0f;

        // Determines gravity and velocity while on a slope
        private Vector3 velocity = Vector3.zero;

        [Header("Crouch Settings")]
        [SerializeField]
        private float standingHeight = 2f;
        [SerializeField]
        private float crouchingHeight = 1.5f;
        [SerializeField]
        private float standingOffset = 1f;
        [SerializeField]
        private float crouchingOffset = 0.75f;
        [SerializeField]
        private float crouchTransitionTime = 10f;

        private float targetHeight;
        private float currentHeight;
        private bool shouldCrouch = false;
        private float elapsedTime;
        private float targetTransitionTime;

        [Header("Jump and Gravity")]
        [SerializeField]
        private float jumpHeight = 3f;
        [SerializeField]
        private float gravity = -19.6f;
        [Tooltip("Determines how character slows down in midair")]
        [SerializeField]
        private float airDrag = 5f;
        [Tooltip("How fast the player is allowed to fall")]
        [SerializeField]
        private float terminalVelocity = -300f;
        [Tooltip("Represents where the ground is to be checked")]
        [SerializeField]
        private float groundOffset = -0.3f;
        [Tooltip("Radius of the sphere that checks if the player is grounded")]
        [SerializeField]
        private float groundCheckRadius = 0.475f;
        [SerializeField]
        private LayerMask groundLayer;

        [Header("Slope Settings")]
        [SerializeField]
        private float slopeSlideSpeed = 5f;
        [SerializeField]
        private float edgeSlideSpeed = 5f;

        // The current angle the player should move at relative to the angle of the ground
        private Quaternion currentAngle = Quaternion.identity;
        private bool isSliding = false;

        private bool canSprint = true;
        private bool canCrouch = true;
        private bool canJump = true;

        [SerializeField]
        private InputReader inputReader;

        private CharacterController controller;

        // Testing Purposes
        private Vector3 surfaceNormal = Vector3.zero;
        private Vector3 gizmosMovementVector = Vector3.zero;
        private Vector3 gizmosVelocityVector = Vector3.zero;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            targetHeight = standingHeight;
        }

        private void OnEnable()
        {
            inputReader.PlayerActions.Jump.performed += OnJump;
            inputReader.PlayerActions.Crouch.performed += OnCrouch;
            inputReader.PlayerActions.Sprint.performed += OnSprint;
        }

        private void OnDisable()
        {
            inputReader.PlayerActions.Jump.performed -= OnJump;
            inputReader.PlayerActions.Crouch.performed -= OnCrouch;
            inputReader.PlayerActions.Sprint.performed -= OnSprint;
        }

        private void Update()
        {
            HandleCrouch();
            ProcessMove(inputReader.PlayerActions.Move.ReadValue<Vector2>());
        }

        /*
         * Determines how the player should move based on player input
         */
        private void ProcessMove(Vector2 movementInput)
        {
            float targetSpeed = IsCrouching() ? crouchSpeed : IsSprinting() ? sprintSpeed : walkSpeed;

            if (IsGrounded() && velocity.y < 0)
            {
                velocity.y = -1;
                currentSpeed = targetSpeed;
                HandleSlope();
                TestSphereCast();
            } 
            else
            {
                // Apply Gravity and cap at terminal velocity
                velocity.y += gravity * Time.deltaTime;
                velocity.y = Mathf.Max(velocity.y, terminalVelocity);

                // Check if player is stuck on an edge
                if (Mathf.Approximately(controller.velocity.y, 0))
                {
                   // HandleEdge();
                }

                ApplyAirDrag();
            }

            // If no input is detect, current speed is 0f
            if (Vector2.zero == movementInput)
            {
                currentSpeed = 0f;
            }


            // Create movement vector based on player input and the current angle
            Vector3 currentMovement = new Vector3();
            currentMovement += transform.forward * movementInput.y;
            currentMovement += transform.right * movementInput.x;
            currentMovement = currentAngle * currentMovement;
            // Clamp the magnitude of the movement vector to 1f
            currentMovement = Vector3.ClampMagnitude(currentMovement, 1f);

            gizmosMovementVector = currentMovement;
            gizmosVelocityVector = velocity;

            Vector3 testVector = (currentMovement * currentSpeed + velocity) * Time.deltaTime;
            
            // Move the character based on all parameters
            controller.Move((currentMovement * currentSpeed + velocity) * Time.deltaTime);
            
        }

        /*
         * Used to slowly decrease the player's horizontal velocity while in the air to the off ground speed
         */
        private void ApplyAirDrag()
        {
            if (currentSpeed > offGroundSpeed)
            {
                currentSpeed = Mathf.Max(currentSpeed - airDrag * Time.deltaTime, offGroundSpeed);
            }
            else
            {
                currentSpeed = offGroundSpeed;
            }
        }

        /*
         * Handle gravity here
         */
        private void ApplyGravity()
        {

        }

        /*
         * Handles crouching up and down and setting target crouch height based on
         * whether the player can crouch and how much they can stand up
         */
        private void HandleCrouch()
        {
            // Determine base target height based on whether the player should crouch
            targetHeight = shouldCrouch ? crouchingHeight : standingHeight;

            // The center of the player capsule needs to be changed to keep the player's position constant
            Vector3 targetCenter = new Vector3(0f, shouldCrouch ? crouchingOffset : standingOffset, 0f);

            currentHeight = controller.height;

            if (IsCrouching() && !shouldCrouch)
            {
                // Cast a ray straight up at the top of the character
                // Cast origin accounts for changes in the character controller's height and its center offset
                // We divide controller height by 2 since it shrinks symmetrically
                // We add controller center to correct for the offset
                Vector3 castOrigin = transform.position + new Vector3(0, controller.height / 2, 0) + controller.center;
                if (Physics.Raycast(castOrigin, Vector3.up, out RaycastHit hit, 0.2f))
                {
                    float distanceToCeiling = hit.point.y - castOrigin.y;
                    float differenceHeight1 = standingHeight - crouchingHeight;
                    targetHeight = Mathf.Max(currentHeight + distanceToCeiling - 0.1f, crouchingHeight);

                    float differenceHeight2 = Mathf.Abs(targetHeight - currentHeight);
                    float ratio = differenceHeight2 / differenceHeight1;
                    targetTransitionTime = ratio * crouchTransitionTime;
                    if (targetTransitionTime < 0.1f)
                    {
                        targetTransitionTime = crouchTransitionTime;
                    }
                    elapsedTime = 0;

                    float newCenterY = Mathf.Max(targetHeight / 2, crouchingOffset);
                    targetCenter = new Vector3(0f, newCenterY, 0f);
                }
            }

            if (!Mathf.Approximately(targetHeight, currentHeight))
            {
                Vector3 currentCenter = controller.center;
                controller.height = Mathf.Lerp(currentHeight, targetHeight, elapsedTime / targetTransitionTime);
                controller.center = Vector3.Lerp(currentCenter, targetCenter, elapsedTime / targetTransitionTime);
                elapsedTime += Time.deltaTime;
            }
            else
            {
                controller.height = targetHeight;
                controller.center = targetCenter;
            }
        }

        private bool IsCrouching()
        {
            return standingHeight - currentHeight > .1f;
        }

        /*
         * If player's gravity is increasing but velocity is not increasing
         * The player is on an edge
         * This method checks where the edge is relative to the player and then
         * gives the player a push in the opposite direction
         */
        private void HandleEdge()
        {
            Vector3 rayOrigin = new Vector3(transform.position.x, transform.position.y, transform.position.z);

            if (Physics.Raycast(rayOrigin, Vector3.forward, out RaycastHit edgeHit, 2f, groundLayer))
            {
                velocity += -Vector3.forward * edgeSlideSpeed;
                Debug.Log("Edge detected forward");

            }
            //if (Physics.Raycast(rayOrigin, new Vector3( )
        }
        
        private bool IsSprinting()
        {
            if (canSprint)
            {
                return inputReader.PlayerActions.Sprint.IsPressed();
            }
            return false;
        }

        // TODO: Improve this
        private void OnJump(InputAction.CallbackContext ctx)
        {
            if (isSliding) return;

            if (IsCrouching() || shouldCrouch)
            {
                // Nothing should be above
                shouldCrouch = false;
            }


            if (canJump && IsGrounded())
            {
                velocity.y += Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }

        private void OnCrouch(InputAction.CallbackContext ctx)
        {  
            if (canCrouch && IsGrounded())
            {
                shouldCrouch = !shouldCrouch;
                targetTransitionTime = crouchTransitionTime;
                elapsedTime = 0;
            }
        }

        private void OnSprint(InputAction.CallbackContext ctx)
        {
            if (shouldCrouch)
            {
                shouldCrouch = false;
                targetTransitionTime = crouchTransitionTime;
                elapsedTime = 0;
            }
        }

        private bool IsGrounded()
        {
            //Vector3 groundCheck = new Vector3(transform.position.x, transform.position.y - groundOffset, transform.position.z);
            //return Physics.CheckSphere(groundCheck, groundCheckRadius, groundLayer);
            return controller.isGrounded;
        }

        private void TestSphereCast()
        {
            float verticalOffset = controller.height / 2 - controller.radius;
            Vector3 rayOrigin = transform.position - new Vector3(0, controller.skinWidth, 0);

            if (Physics.SphereCast(rayOrigin, controller.radius - 0.1f, velocity.normalized, out RaycastHit hit, velocity.magnitude, groundLayer, QueryTriggerInteraction.Ignore))
            {
                float angle = Vector3.Angle(Vector3.up, hit.normal);
                float angle2 = 90f - angle;
                Debug.Log(angle);
            }
        }

        /*
         * Checks if the player is on a slope
         * If the slope is steeper than the slope limit, it slides the player down
         * If the slope is greater than zero, the current angle is set to move the player smoothly up and down the slope
         * Otherwise, the current angle is set to zero because the player is not on a slope
         */
        private void HandleSlope()
        {
            Vector3 rayOrigin = new Vector3(transform.position.x, transform.position.y - groundOffset, transform.position.z);
            // Consider switching to a sphere cast

            //if (Physics.SphereCast(rayOrigin, .6f, Vector3.down, out RaycastHit slopeHit, 0.5f, groundLayer))
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit slopeHit, 5f, groundLayer))
            {
                Vector3 hitPointNormal = slopeHit.normal;
                // Testing
                surfaceNormal = hitPointNormal;
                float angle = Vector3.Angle(hitPointNormal, Vector3.up);
               
                if (angle > controller.slopeLimit)
                {
                    velocity = Vector3.ProjectOnPlane(new Vector3(0, -5f, 0), hitPointNormal) * slopeSlideSpeed;
                    isSliding = true;
                    currentAngle = Quaternion.FromToRotation(transform.up, hitPointNormal);

                    // Reduce speed based off of how steep the angle is over the slope limit
                    float speedReduction = 1f - ((angle - controller.slopeLimit) / (90f - controller.slopeLimit));
                    currentSpeed *= speedReduction;
                    return;
                }
                else if (angle > 0)
                {
                    velocity = new Vector3(0, velocity.y, 0);
                    isSliding = false;
                    currentAngle = Quaternion.FromToRotation(transform.up, hitPointNormal);
                    return;
                }
            }

            currentAngle = Quaternion.identity;
            isSliding = false;
            velocity = new Vector3(0, velocity.y, 0);
            // Testing
            surfaceNormal = Vector3.zero;
        }

        // For Testing
        public void OnDrawGizmos()
        {
            // Draw Ground Check
            Gizmos.color = Color.yellow;
            Vector3 groundCheck = new Vector3(transform.position.x, transform.position.y - groundOffset, transform.position.z);
            Gizmos.DrawWireSphere(groundCheck, groundCheckRadius);
            Vector3 rayOrigin = new Vector3(transform.position.x, transform.position.y - groundOffset, transform.position.z);
            Gizmos.DrawRay(rayOrigin, Vector3.down * 1f);

            Gizmos.DrawRay(new Vector3(transform.position.x, transform.position.y, transform.position.z), Vector3.forward * 1f);
            Gizmos.DrawRay(new Vector3(transform.position.x, transform.position.y, transform.position.z), Quaternion.Euler(0, 45, 0) * Vector3.forward * 1f);
            Gizmos.DrawRay(new Vector3(transform.position.x, transform.position.y, transform.position.z), Vector3.right * 1f);
            Gizmos.DrawRay(new Vector3(transform.position.x, transform.position.y, transform.position.z), Quaternion.Euler(0, 135, 0) * Vector3.forward * 1f);
            Gizmos.DrawRay(new Vector3(transform.position.x, transform.position.y, transform.position.z), -Vector3.forward * 1f);
            Gizmos.DrawRay(new Vector3(transform.position.x, transform.position.y, transform.position.z), Quaternion.Euler(0, 225, 0) * Vector3.forward * 1f);
            Gizmos.DrawRay(new Vector3(transform.position.x, transform.position.y, transform.position.z), -Vector3.right * 1f);
            Gizmos.DrawRay(new Vector3(transform.position.x, transform.position.y, transform.position.z), Quaternion.Euler(0, 315, 0) * Vector3.forward * 1f);

            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, gizmosMovementVector * 5f);
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, gizmosVelocityVector * 5f);
            Gizmos.color = Color.black;
            Gizmos.DrawRay(transform.position, surfaceNormal * 5f);
        }
    }
}
