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
            // Process Movement Input
            playerMovement.ProcessMove(playerActions.Player.Move.ReadValue<Vector2>()); 
        }

        private void LateUpdate()
        {
            // Process Look Input
            playerLook.ProcessLook(playerActions.Player.Look.ReadValue<Vector2>());
        }
    }
}
