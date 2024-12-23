using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Class Name: Player Manager
 * Purpose: This class is responsible for receiving input from the player 
 * and relaying that output to the movement and look scripts for processing
 */
namespace FPSPrototype.Player
{
    public class PlayerManager : MonoBehaviour
    {
        private PlayerInput playerActions;
        private PlayerLook playerLook;
        private PlayerMovement playerMovement;

        private void Awake()
        {
            playerActions = new PlayerInput();
            playerLook = GetComponent<PlayerLook>();
            playerMovement = GetComponent<PlayerMovement>();
        }

        private void OnEnable()
        {
            playerActions.Enable();
        }

        private void OnDisable()
        {
            playerActions.Disable();
        }

        private void Update()
        {
            playerLook.ProcessLook(playerActions.Player.Look.ReadValue<Vector2>());
            playerLook.MoveCamera();

            // Process Movement Input
            MovementInputs movementInputs = new MovementInputs();
            movementInputs.sprintHeld = playerActions.Player.Sprint.IsPressed();
            movementInputs.jumpPressedThisFrame = playerActions.Player.Jump.WasPressedThisFrame();
            movementInputs.jumpHeld = playerActions.Player.Jump.IsPressed();
            movementInputs.crouchHeld = playerActions.Player.Crouch.IsPressed();
            movementInputs.crouchPressedThisFrame = playerActions.Player.Crouch.WasPressedThisFrame();
            movementInputs.crouchReleasedThisFrame = playerActions.Player.Crouch.WasReleasedThisFrame();

            playerMovement.SetInputs(movementInputs);
            playerMovement.ProcessMove(playerActions.Player.Move.ReadValue<Vector2>());
            playerMovement.HandleCrouch();
        }

        private void LateUpdate()
        {
            // Process Look Input
            //playerLook.ProcessLook(playerActions.Player.Look.ReadValue<Vector2>());
            

            // TODO: Remove. For dev only. 
            if (transform.position.y < -300)
            {
                transform.position = new Vector3(0, 2, 0);
            }
        }
    }
}
