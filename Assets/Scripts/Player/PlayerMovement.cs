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

        private CCMovementController movementController;

        private void Awake()
        {
            movementController = GetComponent<CCMovementController>();
        }

        public void ProcessMove(Vector2 movementInput)
        {
            Vector3 currentMovement = new Vector3();
            currentMovement += transform.forward * movementInput.y;
            currentMovement += transform.right * movementInput.x;

            currentMovement = Vector3.ClampMagnitude(currentMovement, 1f);
            currentMovement = currentMovement * walkSpeed * Time.deltaTime;

            // Move the character based on all parameters
            movementController.Move(currentMovement);
        }

        
    }
}
