using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    }
}
