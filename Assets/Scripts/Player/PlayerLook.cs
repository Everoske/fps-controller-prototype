using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR;
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
        private float maxPitch = 80f;
        [Tooltip("How many degrees player can look down")]
        [SerializeField]
        private float minPitch = -80f;
        [SerializeField]
        private bool inverted = false;

        [Header("Smooth Rotation Settings")]
        [SerializeField]
        private Transform lookTarget;
        [SerializeField]
        private float smoothTime;

        private float oldYaw;
        private float oldPitch;
        private float pitchAngularVelocity;
        private float horizontalAngularVelocity;


        private CharacterController characterController;
        private Vector3 initialCameraPosition;
        private float initialControllerHeight;

        [SerializeField]
        private Camera playerCamera;

        private float currentPitch;
        private float currentYaw;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            initialControllerHeight = characterController.height;
            initialCameraPosition = playerCamera.transform.localPosition;
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            lookTarget.localRotation = transform.localRotation;
        }

        /// <summary>
        /// Handles rotating the camera and the player based on the player's look input
        /// </summary>
        /// <param name="look">Vector2 representing the player's look input</param>
        public void ProcessLook(Vector2 look)
        {
            float lookX = look.x * xSensitivity;
            float lookY = look.y * ySensitivity;

            if (!inverted)
            {
                currentPitch -= lookY;
            }
            else
            {
                currentPitch += lookY;
            }

            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

            playerCamera.transform.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);

            transform.Rotate(Vector3.up * lookX);
        }

        /// <summary>
        /// Sets the camera's position relative to the player's current crouching height
        /// </summary>
        public void MoveCamera()
        {
            Vector3 halfHeightDifference = new Vector3(0, (initialControllerHeight - characterController.height) / 2, 0f) - characterController.center;
            Vector3 desiredCameraPosition = initialCameraPosition - halfHeightDifference;

            playerCamera.transform.localPosition = desiredCameraPosition;
        }

        private float ClampAngle(float angle)
        {
            return angle < 0f ? -1 * angle % 360f : angle % 360f;
        }
    }
}
