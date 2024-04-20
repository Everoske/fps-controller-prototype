using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CCMovementController : MonoBehaviour
{
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

    private CharacterController characterController;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    public void Move(Vector3 movementInput)
    {
        movementInput = ProcessSlope(movementInput);
        characterController.Move(movementInput);
        characterController.Move(ProcessGravity());
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
        float sphereCastRadius = characterController.radius;
        Vector3 playerCenterPoint = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        Physics.SphereCast(playerCenterPoint, sphereCastRadius, Vector3.down, out groundHit);
        float playerCenterToGroundDistance = groundHit.distance + sphereCastRadius;

        return (playerCenterToGroundDistance >= characterController.radius + characterController.skinWidth - groundCheckTolerance) &&
            (playerCenterToGroundDistance <= characterController.radius + characterController.skinWidth + groundCheckTolerance);
    }
}
