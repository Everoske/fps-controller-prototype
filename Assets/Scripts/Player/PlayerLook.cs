using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows;

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

        [Header("Camera Position")]
        [SerializeField]
        private float cameraCrouchHeight = -0.4f;
        [SerializeField]
        private float cameraPositionTolerance = 0.025f;

        private float cameraStandHeight = 0.0f;

        [SerializeField]
        private Camera playerCamera;

        private float totalPitch;

        private void Awake()
        {
            cameraStandHeight = playerCamera.transform.localPosition.y;
        }

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
            float lookX = look.x * xSensitivity * Time.deltaTime;
            float lookY = look.y * ySensitivity * Time.deltaTime;

            if (!inverted)
            {
                totalPitch -= lookY;
            }
            else
            {
                totalPitch += lookY;
            }
            

            totalPitch = Mathf.Clamp(totalPitch, minPitch, maxPitch);

            playerCamera.transform.localRotation = Quaternion.Euler(totalPitch, 0f, 0f);

            transform.Rotate(Vector3.up * lookX);
        }

        public void SetCameraCrouchPosition(float ratio)
        {
            float heightDifference = cameraStandHeight - cameraCrouchHeight;

            float currentHeight = (heightDifference * ratio) + cameraCrouchHeight;

            if (currentHeight < cameraPositionTolerance)
            {
                currentHeight = cameraCrouchHeight;
            }
            else if (currentHeight > cameraStandHeight - cameraPositionTolerance)
            {
                currentHeight = cameraStandHeight;
            } 

            playerCamera.transform.localPosition = new Vector3(playerCamera.transform.localPosition.x,
                currentHeight, playerCamera.transform.localPosition.z);
        }
    }
}
