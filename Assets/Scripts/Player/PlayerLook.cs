using FPSPrototype.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Contains the logic for the player looking around the game world
 */
namespace FPSPrototype.Player
{
    public class PlayerLook : MonoBehaviour
    {
        [Header("Look Parameters")]
        [Tooltip("Horizontal look sensitivity")]
        [SerializeField]
        private float sensitivityX = 25.0f;
        [Tooltip("Vertical look sensitivity")]
        [SerializeField]
        private float sensitivityY = 25.0f;
        [Tooltip("How many degrees you can look up")]
        [SerializeField]
        private float maxPitch = 90.0f;
        [Tooltip("How many degrees you can look down")]
        [SerializeField]
        private float minPitch = -90.0f;

        [SerializeField]
        private InputReader inputReader;
        [SerializeField]
        private Camera playerCamera;
        private Vector3 initialCameraPosition;
        private float initialControllerHeight;

        private CharacterController controller;

        private float totalPitch = 0f;

        private void Awake()
        {
            HideCursor();
            controller = GetComponent<CharacterController>();
            initialControllerHeight = controller.height;
            initialCameraPosition = playerCamera.transform.localPosition;
        }

        private void LateUpdate()
        {
            MoveCamera();
            ProcessLook(inputReader.PlayerActions.Look.ReadValue<Vector2>());
        }

        // Moves the camera based on where the player is looking
        private void ProcessLook(Vector2 input)
        {
            // Process Input
            float lookX = input.x * sensitivityX * Time.deltaTime;
            float lookY = input.y * sensitivityY * Time.deltaTime;

            // Subtract look-y input from total pitch
            totalPitch -= lookY;
            // Clamp the degrees of total pitch to min and max pitch
            totalPitch = Mathf.Clamp(totalPitch, minPitch, maxPitch);

            // Rotate the player camera about the x axis to move the pitch up and down
            playerCamera.transform.localRotation = Quaternion.Euler(totalPitch, 0f, 0f);

            // Rotate body left and right
            transform.Rotate(Vector3.up * lookX);
        }

        private void MoveCamera()
        {
            Vector3 halfHeightDifference = new Vector3(0, (initialControllerHeight - controller.height) / 2, 0f) - controller.center;           
            Vector3 desiredCameraPosition = initialCameraPosition - halfHeightDifference;
            
            playerCamera.transform.localPosition = desiredCameraPosition;
        }

        // Locks and hides cursor
        private void HideCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

    }

}