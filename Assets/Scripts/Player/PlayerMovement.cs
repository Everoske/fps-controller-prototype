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
        [Header("Movement")]
        [SerializeField]
        private float walkSpeed = 8.0f;
        [SerializeField]
        private float sprintSpeed = 12.0f;
        [SerializeField]
        private float crouchSpeed = 4.0f;
        [SerializeField]
        private float airSpeed = 2.0f;
        [Tooltip("Determines how fast player slows down in air")]
        [SerializeField]
        private float airDrag = 2.5f;

        private bool isRunning = false;
        private float targetSpeed = 0.0f;

        [Header("Gravity")]
        [SerializeField]
        private float baseGravity = -12.8f;
        [SerializeField]
        private float terminalVelocity = -98f;
        [SerializeField]
        private float gravityGrounded = -1.0f;
        [SerializeField]
        private float groundCheckTolerance = 0.015f;
        [SerializeField]
        private LayerMask collisionMask;

        private bool isGrounded = false;
        private RaycastHit groundHit;

        private float timeSinceLeftGround = 0.0f;
        private float currentGravity = 0.0f;

        [Header("Jump")]
        [SerializeField]
        private float baseJumpHeight = 1.0f;
        [Tooltip("How much the jump height will increase if jump is held continuously")]
        [SerializeField]
        private float jumpHeldMultiplier = 3f;
        [Tooltip("Time to reach max jump height")]
        [SerializeField]
        private float timeToJump = 0.25f;
        [Tooltip("Determines how long the jumping grace period since player left the ground")]
        [SerializeField]
        private float coyoteTime = 0.15f;
        [SerializeField]
        private float jumpBufferTime = 0.15f;

        private bool jumpPressedThisFrame = false;
        private bool jumpHeld = false;
        private bool jumpInProgress = false;

        private bool jumpHeldContinuously = false;

        private float jumpProgressTimer = 0.0f;
        private float coyoteTimeCounter = 0.0f;
        private float jumpBufferCounter = 0.0f;

        [Header("Slope Handling")]
        [SerializeField]
        private float maxSlopeAngle = 75f;
        [SerializeField]
        private float slideDownAngle = 55f;
        [Tooltip("How fast the character slides down steep slopes")]
        [SerializeField]
        private float slideDownMultiplier = 15f;

        private bool playerOnSteepSlope = false;

        private CharacterController characterController;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            characterController.slopeLimit = maxSlopeAngle;
            currentGravity = baseGravity;
        }

        /// <summary>
        /// Determines player behavior based on their input
        /// </summary>
        /// <param name="movementInputs">Container for player inputs</param>
        public void SetInputs(MovementInputs movementInputs)
        {
            isRunning = movementInputs.sprintHeld;
            jumpPressedThisFrame = movementInputs.jumpPressedThisFrame;
            jumpHeld = movementInputs.jumpHeld;
        }

        public void ProcessMove(Vector2 movementInput)
        {
            Vector3 currentMovement = new Vector3();
            currentMovement += transform.forward * movementInput.y;
            currentMovement += transform.right * movementInput.x;

            currentMovement = Vector3.ClampMagnitude(currentMovement, 1f);
            targetSpeed = DetermineSpeed();

            currentMovement = currentMovement * targetSpeed * Time.deltaTime;

            // Move the character based on all parameters
            Move(currentMovement);
        }

        private void Move(Vector3 movementInput)
        {
            // Check if grounded
            DetermineGroundedState();

            Vector3 processedInput = movementInput;

            processedInput = ProcessSlope(processedInput);

            processedInput += ProcessGravity();
            processedInput = ProcessJump(processedInput);

#if UNITY_EDITOR
            Debug.DrawRay(transform.position, transform.TransformDirection(processedInput), Color.red, 0.5f);
#endif

            characterController.Move(processedInput);
        }

        private Vector3 ProcessGravity()
        {
            Vector3 gravityToApply = Vector3.zero;
            if (!isGrounded)
            {
                timeSinceLeftGround += Time.deltaTime;

                currentGravity = Mathf.Max(baseGravity * timeSinceLeftGround, terminalVelocity);

                gravityToApply = new Vector3(0f, currentGravity * Time.deltaTime, 0f);
            }
            else
            {
                timeSinceLeftGround = 0.0f;
                currentGravity = baseGravity;
                gravityToApply = new Vector3(0f, gravityGrounded * Time.deltaTime, 0f);
            }

            return gravityToApply;
        }

        private Vector3 ProcessSlope(Vector3 moveInput)
        {
            Vector3 slopeMovement = moveInput;

            if (isGrounded)
            {
                Vector3 groundNormal = transform.InverseTransformDirection(groundHit.normal);
                float groundSlopeAngle = Vector3.Angle(groundNormal, transform.up);
                playerOnSteepSlope = false;

                if (groundSlopeAngle != 0f)
                {
                    Quaternion slopeAngleRotation = Quaternion.FromToRotation(transform.up, groundHit.normal);
                    slopeMovement = slopeAngleRotation * slopeMovement;

                    if (groundSlopeAngle > slideDownAngle)
                    {
                        playerOnSteepSlope = true;

                        float maxSlideLimit = maxSlopeAngle - slideDownAngle;
                        float currentSlideLimit = maxSlopeAngle - groundSlopeAngle;
                        float slideFactor = 1;

                        if (currentSlideLimit > 0)
                        {
                            slideFactor = Mathf.Clamp(currentSlideLimit / maxSlideLimit, 0.1f, 1f);
                        }

                        float currentSlideMultiplier = slideDownMultiplier * slideFactor * Time.deltaTime;

                        Vector3 slideDownVector = Vector3.ProjectOnPlane(transform.up, groundHit.normal).normalized * currentSlideMultiplier;

                        slopeMovement = slopeMovement - slideDownVector;
                    }
                }
            } 
            else
            {
                playerOnSteepSlope = false;
            }

            return slopeMovement;
        }

        private Vector3 ProcessJump(Vector3 movementInput)
        {
            Vector3 jumpInput = movementInput;

            SetJumpProgressTime();
            SetCoyoteTime();
            SetJumpBufferTime();

            bool canJump = isGrounded || coyoteTimeCounter < coyoteTime && jumpProgressTimer < timeToJump;

            if (canJump && !playerOnSteepSlope && !jumpInProgress && JumpCalled())
            {
                jumpInProgress = true;
                jumpHeldContinuously = jumpHeld;
                jumpBufferCounter = 0;

                float jumpAmount = (baseJumpHeight / timeToJump * Time.deltaTime);

                if (timeSinceLeftGround > 0)
                {
                    jumpAmount -= baseGravity * (timeSinceLeftGround) * Time.deltaTime;
                    timeSinceLeftGround = 0;
                }

                jumpInput.y += jumpAmount;
            }
            else if (jumpInProgress && jumpProgressTimer > 0 && jumpProgressTimer < timeToJump) 
            {
                if (!jumpHeld)
                {
                    jumpHeldContinuously = false;
                }

                float jumpAmount = (baseJumpHeight / timeToJump * Time.deltaTime);

                if (jumpHeldContinuously)
                {
                    jumpAmount *= jumpHeldMultiplier;
                }
                jumpInput.y += jumpAmount;
            }
            else
            {
                jumpInProgress = false;
            }
            return jumpInput;
        }

        private void SetCoyoteTime()
        {
            if (isGrounded)
            {
                coyoteTimeCounter = 0.0f;
            }
            else
            {
                coyoteTimeCounter += Time.deltaTime;
            }
        }

        private void SetJumpProgressTime()
        {
            if (jumpInProgress)
            {
                jumpProgressTimer += Time.deltaTime;
            }
            else if (isGrounded)
            {
                jumpProgressTimer = 0.0f;
            }
        }

        private void SetJumpBufferTime()
        {
            if (jumpPressedThisFrame || jumpHeld)
            {
                jumpBufferCounter = jumpBufferTime;
            }
            else
            {
                jumpBufferCounter -= Time.deltaTime;
            }
        }

        private bool JumpCalled()
        {
            return jumpBufferCounter > 0;
        }

        /// <summary>
        /// Determines if the character is grounded
        /// </summary>
        public void DetermineGroundedState()
        {
            float sphereCastRadius = characterController.radius;
            Vector3 playerCenterPoint = new Vector3(transform.position.x, transform.position.y, transform.position.z);
            float distance = characterController.height / 2 + characterController.skinWidth - characterController.radius + groundCheckTolerance;

            if (Physics.SphereCast(playerCenterPoint, sphereCastRadius, Vector3.down, out groundHit, distance, collisionMask))
            {
                isGrounded = true;
            }
            else
            {
                isGrounded = false;
            }
        }

        /// <summary>
        /// Determines the speed the player should be moving at 
        /// </summary>
        private float DetermineSpeed()
        {
            if (!isGrounded)
            {
                if (targetSpeed > airSpeed)
                {
                    return Mathf.Max(targetSpeed - airDrag * Time.deltaTime, airSpeed);
                }
                return airSpeed;
            }
            return isRunning ? sprintSpeed : walkSpeed;
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                float distance = characterController.height / 2 + characterController.skinWidth - characterController.radius + groundCheckTolerance;
                Vector3 sphereOrigin = new Vector3(transform.position.x, transform.position.y - distance, transform.position.z);
                Gizmos.DrawWireSphere(sphereOrigin, characterController.radius);
            }
        }
    }

    public struct MovementInputs
    {
        public bool sprintPressedThisFrame;
        public bool sprintHeld;
        public bool sprintReleasedThisFrame;
        public bool jumpPressedThisFrame;
        public bool jumpHeld;
        public bool jumpReleasedThisFrame;
        public bool crouchPressedThisFrame;
        public bool crouchHeld;
        public bool crouchReleasedThisFrame;
    }
}
