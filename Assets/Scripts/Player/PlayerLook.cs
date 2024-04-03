using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Name: Player Look
 * Purpose: This class is responsible for handling the player's camera input
 * and rotating the player to match where they are looking
 */
namespace FPSPrototype.Player
{
    public class PlayerLook : MonoBehaviour
    {
        [Header("Look Parameters")]
        [Tooltip("Horizontal look sensitivity")]
        [SerializeField]
        private float xSensitivity = 25f;
        [Tooltip("Vertical look sensitivity")]
        [SerializeField]
        private float ySensitivity = 25f;
        [Tooltip("How many degrees player can look up")]
        [SerializeField]
        private float maxPitch = 90f;
        [Tooltip("How many degrees player can look down")]
        [SerializeField]
        private float minPitch = -90f;
        [SerializeField]
        private bool inverted = false;

        [SerializeField]
        private Camera playerCamera;

        private float totalPitch;
        private float horizontalRotation;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        /// <summary>
        /// Handles rotating the camera and the player based on the player's look input
        /// </summary>
        /// <param name="look">Vector2 representing the player's look input</param>
        public void ProcessLook(Vector2 look)
        {
            // Process input
            float lookX = look.x * xSensitivity * Time.deltaTime;
            float lookY = look.y * ySensitivity * Time.deltaTime;

            // Add horizontal input
            horizontalRotation += lookX;

            // Process vertical input
            if (inverted)
            {
                totalPitch += lookY;
            }
            else
            {
                totalPitch -= lookY;
            }
            
            // Clamp vertical input to min and max pitch
            totalPitch = Mathf.Clamp(totalPitch, minPitch, maxPitch);

            // Rotate camera based on total pitch and horizontal rotation
            playerCamera.transform.localRotation = Quaternion.Euler(totalPitch, horizontalRotation, 0);
            // Rotate player based on current rotation
            transform.localRotation = Quaternion.Euler(0, horizontalRotation, 0);
        }
    }
}
