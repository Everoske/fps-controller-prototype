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
        [SerializeField]
        private LayerMask collisionMask;

        private bool isGrounded = false;
        private RaycastHit groundHit;

        [Header("Slope Handling")]
        [SerializeField]
        private float maxSlopeAngle = 75f;
        [SerializeField]
        private float slideDownAngle = 55f;
        [Tooltip("How fast the character slides down steep slopes")]
        [SerializeField]
        private float slideDownMultiplier = 15f;

        private CharacterController characterController;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            characterController.slopeLimit = maxSlopeAngle;
        }

        /// <summary>
        /// Determines player behavior based on their input
        /// </summary>
        /// <param name="inputs">Container for player inputs</param>
        public void SetInputs(MovementInputs inputs)
        {

        }

        public void ProcessMove(Vector2 movementInput)
        {
            Vector3 currentMovement = new Vector3();
            currentMovement += transform.forward * movementInput.y;
            currentMovement += transform.right * movementInput.x;

            currentMovement = Vector3.ClampMagnitude(currentMovement, 1f);
            currentMovement = currentMovement * walkSpeed * Time.deltaTime;

            // Move the character based on all parameters
            Move(currentMovement);
        }

        private void Move(Vector3 movementInput)
        {
            // Check if grounded
            DetermineGroundedState();

            Vector3 processedInput = movementInput;

            processedInput = ProcessSlope(processedInput);

#if UNITY_EDITOR
            Debug.DrawRay(transform.position, transform.TransformDirection(processedInput), Color.red, 0.5f);
#endif

            processedInput += ProcessGravity();

            characterController.Move(processedInput);
        }

        private Vector3 ProcessGravity()
        {
            Vector3 gravityToApply = Vector3.zero;
            if (!isGrounded)
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

            if (isGrounded)
            {
                Vector3 groundNormal = transform.InverseTransformDirection(groundHit.normal);
                float groundSlopeAngle = Vector3.Angle(groundNormal, transform.up);

                if (groundSlopeAngle != 0f)
                {
                    Quaternion slopeAngleRotation = Quaternion.FromToRotation(transform.up, groundHit.normal);
                    slopeMovement = slopeAngleRotation * slopeMovement;

                    if (groundSlopeAngle > slideDownAngle)
                    {
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

            return slopeMovement;
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
        private readonly bool sprintPressedThisFrame;
        private readonly bool sprintHeld;
        private readonly bool sprintReleasedThisFrame;
        private readonly bool jumpPressedThisFrame;
        private readonly bool jumpHeld;
        private readonly bool jumpReleasedThisFrame;
        private readonly bool crouchPressedThisFrame;
        private readonly bool crouchHeld;
        private readonly bool crouchReleasedThisFrame;
    }
}
