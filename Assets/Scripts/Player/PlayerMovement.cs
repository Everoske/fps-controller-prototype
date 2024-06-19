using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
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
        private bool wasGroundedLastFrame = false;
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
        [Tooltip("How much jump height will be reduced if jumping while crouched")]
        [SerializeField]
        private float crouchJumpMultiplier = 0.5f;
        [Tooltip("Determines how long the jumping grace period since player left the ground")]
        [SerializeField]
        private float coyoteTime = 0.15f;
        [SerializeField]
        private float jumpBufferTime = 0.15f;

        private bool jumpPressedThisFrame = false;
        private bool jumpHeld = false;
        private bool jumpInProgress = false;
        private bool playerHasJumped = false;

        private bool jumpHeldContinuously = false;

        private float jumpProgressTimer = 0.0f;
        private float coyoteTimeCounter = 0.0f;
        private float jumpBufferCounter = 0.0f;

        [Header("Crouch Settings")]
        [SerializeField]
        private float crouchHeight = 1f;
        [SerializeField]
        private float crouchTransitionTime = 10f;
        [Tooltip("Small margin used to account for floating numbers")]
        [SerializeField]
        private float crouchHeightConfidence = 0.001f;

        private float standingHeight = 0f;
        private float targetHeight = 0f;
        private float standingYCenter = 0f;
        private float targetYCenter = 0.0f;

        private float targetTransitionTime = 0.0f;
        private float crouchTimeCounter = 0.0f;

        private bool isCrouching = false;
        private bool crouchHeld = false;
        private bool crouchReleasedThisFrame = false;
        private bool crouchPressedThisFrame = false;

        [SerializeField]
        private float headCheckTolerance = 0.015f;

        [Header("Slope Handling")]
        [SerializeField]
        private float maxSlopeAngle = 75f;
        [SerializeField]
        private float slideDownAngle = 55f;
        [Tooltip("How fast the character slides down steep slopes")]
        [SerializeField]
        private float slideDownMultiplier = 15f;

        private bool playerOnSteepSlope = false;

        [Header("Step Handling")]
        [SerializeField]
        private float maxStepHeight = 0.5f;
        [SerializeField]
        private float snapDownDepth = 0.5f;
        [Tooltip("Minimum depth of a step required to ascend")]
        [SerializeField]
        private float minStepDepth = 0.25f;
        [SerializeField]
        private float stepHeightTolerance = 0.01f;

        private bool steppingUp = false;

        [SerializeField]
        private bool shouldSnapDown = true;

        [Header("RigidBody Interactions")]
        [SerializeField]
        private float playerForceMultiplier = 200f;

        private CharacterController characterController;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            characterController.slopeLimit = maxSlopeAngle;
            currentGravity = baseGravity;
            standingHeight = characterController.height;
            standingYCenter = characterController.center.y;
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
            crouchPressedThisFrame = movementInputs.crouchPressedThisFrame;
            crouchHeld = movementInputs.crouchHeld;
            crouchReleasedThisFrame = movementInputs.crouchReleasedThisFrame;
        }

        /// <summary>
        /// Determines how the player should move based on raw player input
        /// </summary>
        /// <param name="movementInput">Raw movement input from player</param>
        public void ProcessMove(Vector2 movementInput)
        {
            Vector3 currentMovement = new Vector3();
            currentMovement += transform.forward * movementInput.y;
            currentMovement += transform.right * movementInput.x;

            currentMovement = Vector3.ClampMagnitude(currentMovement, 1f);
            targetSpeed = DetermineSpeed();

            currentMovement = currentMovement * targetSpeed * Time.deltaTime;

            Move(currentMovement);
        }

        /// <summary>
        /// Moves the player based on processed movement input
        /// </summary>
        /// <param name="movementInput">Processed movement input</param>
        private void Move(Vector3 movementInput)
        {
            DetermineGroundedState();

            Vector3 processedInput = movementInput;

            DetermineSteppingUp(processedInput);

            processedInput = ProcessSlope(processedInput);

            processedInput = ProcessJump(processedInput);
            processedInput += ProcessGravity();
            

#if UNITY_EDITOR
            Debug.DrawRay(transform.position, transform.TransformDirection(processedInput), Color.red, 0.5f);
#endif

            characterController.Move(processedInput);
        }

        /// <summary>
        /// Determines how much gravity should be applied to the player
        /// </summary>
        /// <returns>Gravity vector</returns>
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
                if (shouldSnapDown)
                {
                    gravityToApply = new Vector3(0f, gravityGrounded * Time.deltaTime, 0f);
                }
                
            }

            return gravityToApply;
        }

        /// <summary>
        /// Determines how the player is affected by the slope beneath their feet
        /// </summary>
        /// <param name="moveInput">Intended movement vector before slope is processed</param>
        /// <returns>Movement input with slope affects accounted for</returns>
        private Vector3 ProcessSlope(Vector3 moveInput)
        {
            if (steppingUp) return moveInput;

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

        /// <summary>
        /// Responsible for making the player jump
        /// </summary>
        /// <param name="movementInput">Movement input before jump is processed</param>
        /// <returns>New movement vector with processed jump input</returns>
        private Vector3 ProcessJump(Vector3 movementInput)
        {
            Vector3 jumpInput = movementInput;

            SetJumpProgressTime();
            SetCoyoteTime();
            SetJumpBufferTime();

            bool canJump = isGrounded || coyoteTimeCounter < coyoteTime && jumpProgressTimer < timeToJump;

            if (canJump && !playerOnSteepSlope && !jumpInProgress && JumpCalled())
            {
                shouldSnapDown = false;
                jumpInProgress = true;
                playerHasJumped = true;
                jumpHeldContinuously = jumpHeld;
                jumpBufferCounter = 0;

                float jumpAmount = (baseJumpHeight / timeToJump * Time.deltaTime);

                if (timeSinceLeftGround > 0)
                {
                    jumpAmount -= baseGravity * (timeSinceLeftGround) * Time.deltaTime;
                    timeSinceLeftGround = 0;
                }

                if (isCrouching)
                {
                    jumpAmount *= crouchJumpMultiplier;
                }

                jumpInput.y += jumpAmount;
            }
            else if (jumpInProgress && jumpProgressTimer > 0 && jumpProgressTimer < timeToJump)
            {
                if (CheckHeadCollision(0, out float distanceToCollider))
                {
                    jumpHeldContinuously = false;
                    jumpInProgress = false;
                    return jumpInput;
                }

                if (!jumpHeld)
                {
                    jumpHeldContinuously = false;
                }

                float jumpAmount = (baseJumpHeight / timeToJump * Time.deltaTime);

                if (jumpHeldContinuously)
                {
                    jumpAmount *= jumpHeldMultiplier;
                }

                if (isCrouching)
                {
                    jumpAmount *= crouchJumpMultiplier;
                }

                jumpInput.y += jumpAmount;
            }
            else
            {
                jumpInProgress = false;
                jumpHeldContinuously = false;
            }
            return jumpInput;
        }

        /// <summary>
        /// Increments coyote time when the player is not grounded
        /// </summary>
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

        /// <summary>
        /// Increments the jump timer while the player should be jumping
        /// </summary>
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

        /// <summary>
        /// Determines the window for jumping
        /// </summary>
        private void SetJumpBufferTime()
        {
            if (jumpPressedThisFrame)
            {
                jumpBufferCounter = jumpBufferTime;
            }
            else
            {
                jumpBufferCounter -= Time.deltaTime;
            }
        }

        /// <summary>
        /// Determines whether the player should jump based on whether the jump
        /// window is open
        /// </summary>
        /// <returns>Returns true if jump window is open</returns>
        private bool JumpCalled()
        {
            return jumpBufferCounter > 0;
        }

        /// <summary>
        /// Determines if the player should crouch or stand up during a given frame
        /// </summary>
        public void HandleCrouch()
        {
            bool canInitiateCrouch = isGrounded && (crouchPressedThisFrame || crouchHeld);
            bool continueCrouching = isCrouching && crouchHeld;

            bool shouldCrouch = canInitiateCrouch || continueCrouching;

            if (shouldCrouch)
            {
                SetCrouchParameters(crouchHeight);
                Crouch();
            }
            else if (isCrouching)
            {
                float headCheckDistance = standingHeight - crouchHeight;
                float newHeight = standingHeight;

                if (CheckHeadCollision(headCheckDistance, out float distanceToCollider))
                {
                    float heightTolerance = headCheckTolerance + characterController.skinWidth;
                    newHeight = Mathf.Max(characterController.height / 2 + distanceToCollider - heightTolerance, crouchHeight);
                }

                SetCrouchParameters(newHeight);
                StandUp();
            }
        }

        /// <summary>
        /// Sets target height, target center, and target transition time for
        /// determining how the player should crouch
        /// </summary>
        /// <param name="newHeight">New target height for the player's collider</param>
        private void SetCrouchParameters(float newHeight)
        {
            targetHeight = Mathf.Clamp(newHeight, crouchHeight, standingHeight);
            targetYCenter = standingYCenter - (1 - (targetHeight / 2));

            float timeRatio = (targetHeight - characterController.height) / (standingHeight - crouchHeight);

            if (timeRatio < 0)
            {
                timeRatio *= -1;
            }

            targetTransitionTime = Mathf.Clamp(timeRatio * crouchTransitionTime, 0.1f, crouchTransitionTime);
        }

        /// <summary>
        /// Smoothly shrinks or increases the player's collider's height and center
        /// until they reach the target height and center
        /// </summary>
        private void ProcessCrouch()
        {
            characterController.height = Mathf.Lerp(characterController.height, targetHeight, crouchTimeCounter / targetTransitionTime);
            float currentYCenter = Mathf.Lerp(characterController.center.y, targetYCenter, crouchTimeCounter / targetTransitionTime);

            characterController.center = new Vector3(
                characterController.center.x,
                currentYCenter,
                characterController.center.z
                );

            crouchTimeCounter += Time.deltaTime;
        }

        /// <summary>
        /// Decreases the player's collider's height to the specified crouching height
        /// </summary>
        private void Crouch()
        {
            if (characterController.height >= crouchHeight + crouchHeightConfidence)
            {
                ProcessCrouch();
                isCrouching = true;
            }
            else
            {
                EnforceExactHeight();
                crouchTimeCounter = 0;
            }
        }


        /// <summary>
        /// Attempts to make the player's collider match the standing height
        /// or the maximum height they can stand
        /// </summary>
        private void StandUp()
        {
            if (characterController.height <= standingHeight - crouchHeightConfidence)
            {
                ProcessCrouch();
            }
            else
            {
                SetCrouchParameters(standingHeight);
                EnforceExactHeight();
                crouchTimeCounter = 0;
                isCrouching = false;
            }
        }

        /// <summary>
        /// Ensures that the player's collider is of the exact height and
        /// center as its target height and center
        /// </summary>
        private void EnforceExactHeight()
        {
            if (isCrouching)
            {
                characterController.height = targetHeight;
                characterController.center = new Vector3(characterController.center.x, targetYCenter, characterController.center.z);
            }
        }

        /// <summary>
        /// Determines if the character is grounded
        /// </summary>
        public void DetermineGroundedState()
        {
            wasGroundedLastFrame = isGrounded;

            float sphereCastRadius = characterController.radius;
            Vector3 playerCenterPoint = new Vector3(transform.position.x, transform.position.y, transform.position.z);
            float maxGroundDistance = standingHeight / 2 + characterController.skinWidth - characterController.radius + groundCheckTolerance;
            float sphereCastDistance = maxGroundDistance + snapDownDepth;

            if (Physics.SphereCast(playerCenterPoint, sphereCastRadius, Vector3.down, out groundHit, sphereCastDistance, collisionMask, QueryTriggerInteraction.Ignore))
            {
                if (groundHit.distance < maxGroundDistance)
                {
                    isGrounded = true;
                    playerHasJumped = false;
                } else if (wasGroundedLastFrame && !playerHasJumped)
                {
                    shouldSnapDown = true;

                    if (groundHit.transform.TryGetComponent<DynamicMover>(out DynamicMover mover))
                    {
                        shouldSnapDown = false;
                    }
                }
            }
            else
            {
                isGrounded = false;
                shouldSnapDown = false;
            }
        }

        /// <summary>
        /// Checks whether a collision occurs above the player
        /// </summary>
        /// <param name="distance">Distance to check for a collision</param>
        /// <param name="distanceToCollider">Distance to the collision source</param>
        /// <returns>True if a collision was detected, false otherwise.</returns>
        private bool CheckHeadCollision(float distance, out float distanceToCollider)
        {
            distanceToCollider = -1f;

            float sphereCastRadius = characterController.radius;
            float relativeVerticalCenterPoint = transform.position.y + characterController.center.y;
            Vector3 playerCenterPoint = new Vector3(transform.position.x, relativeVerticalCenterPoint, transform.position.z);

            float checkDistance = characterController.height / 2 + characterController.skinWidth - characterController.radius + headCheckTolerance - characterController.center.y;
            checkDistance += distance;

            if (Physics.SphereCast(playerCenterPoint, sphereCastRadius, Vector3.up, out RaycastHit ceilingHit, checkDistance, collisionMask, QueryTriggerInteraction.Ignore))
            {
                distanceToCollider = ceilingHit.point.y - playerCenterPoint.y;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines the speed the player should be moving at 
        /// </summary>
        /// <returns>Speed to move player</returns>
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

            return isCrouching ? crouchSpeed : isRunning ? sprintSpeed : walkSpeed;
        }

        /// <summary>
        /// Determines whether the player should step up such as when they are going up stairs.
        /// Sets the Character Controller's step offset accordingly as to prevent the player from
        /// bouncing when not stepping up.
        /// </summary>
        /// <param name="moveDirection">Movement vector of player</param>
        private void DetermineSteppingUp(Vector3 moveDirection)
        {
            steppingUp = false;
            if (playerHasJumped || !isGrounded)
            {
                characterController.stepOffset = 0.0f;
                return;
            }

            float playerGroundHeight = transform.position.y - (standingHeight / 2) - characterController.skinWidth;
            Vector3 lowerOrigin = new Vector3(transform.position.x, playerGroundHeight, transform.position.z);
            float rayDistance = moveDirection.magnitude + characterController.radius + characterController.skinWidth + 0.1f;

            if (Physics.Raycast(lowerOrigin, moveDirection.normalized, out RaycastHit lowerHit, rayDistance, collisionMask, QueryTriggerInteraction.Ignore))
            {
                float stepSlopeAngle = Vector3.Angle(lowerHit.normal, transform.up);

                if (stepSlopeAngle >= maxSlopeAngle)
                {
                    Vector3 upperOrigin = new Vector3(transform.position.x, playerGroundHeight + maxStepHeight + stepHeightTolerance, transform.position.z);
                    rayDistance += minStepDepth - 0.2f;
                    if (!Physics.Raycast(upperOrigin, moveDirection.normalized, out RaycastHit upperHit, rayDistance, collisionMask, QueryTriggerInteraction.Ignore))
                    {
                        shouldSnapDown = false;
                        steppingUp = true;
                        characterController.stepOffset = maxStepHeight;
                    }
                }
            } else
            {
                characterController.stepOffset = 0.0f;
            }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Rigidbody body = hit.collider.attachedRigidbody;

            if (body != null && !body.isKinematic)
            {
                if (hit.moveDirection.y < -0.3f) return;

                Vector3 pushDirection = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);

                body.AddForce(pushDirection * targetSpeed * Time.deltaTime * playerForceMultiplier);
            }
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                float distance = standingHeight / 2 + characterController.skinWidth - characterController.radius + groundCheckTolerance;
                Vector3 sphereOrigin = new Vector3(transform.position.x, transform.position.y - distance, transform.position.z);
                Gizmos.DrawWireSphere(sphereOrigin, characterController.radius);

                float distance2 = characterController.height / 2 + characterController.skinWidth - characterController.radius + headCheckTolerance;
                Vector3 sphereOrigin2 = new Vector3(transform.position.x, transform.position.y + characterController.center.y + distance2, transform.position.z);
                Gizmos.DrawWireSphere(sphereOrigin2, characterController.radius);

                float rayYPos = transform.position.y - (standingHeight / 2) - characterController.skinWidth;
                Vector3 rayPosition = new Vector3(transform.position.x, rayYPos, transform.position.z);
                Gizmos.DrawRay(rayPosition, transform.forward);
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
