using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FPSPrototype.Input
{
    public class InputReader : MonoBehaviour
    {
        private PlayerInput playerInput;

        public PlayerInput.PlayerActions PlayerActions
        {
            get
            {
                if (playerInput == null)
                {
                    playerInput = new PlayerInput();
                }
                return playerInput.Player;
            }
        }

        private void Awake()
        {
            if (playerInput == null)
            {
                playerInput = new PlayerInput();
            }
           
            playerInput.Enable();
        }

        private void OnDisable()
        {
            playerInput.Disable();
        }
    }
}
