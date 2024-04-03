using FPSPrototype.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class TestPlayerMove : MonoBehaviour
{
    [SerializeField]
    private InputReader inputReader;
    [SerializeField]
    private float walkSpeed = 4f;
    private TestController controller;
    private float height;

    private void Awake()
    {
        controller = GetComponent<TestController>();
        height = transform.localScale.y / 2;
    }

    private void Update()
    {
        ProcessMove(inputReader.PlayerActions.Move.ReadValue<Vector2>());
    }

    private void ProcessMove(Vector2 movementInput)
    {
        // Create movement vector based on player input and the current angle
        Vector3 currentMovement = new Vector3(movementInput.x, 0, movementInput.y);
        
        // Clamp the magnitude of the movement vector to 1f
        currentMovement = Vector3.ClampMagnitude(currentMovement, 1f);
        currentMovement = currentMovement * walkSpeed * Time.deltaTime;

        // Move the character based on all parameters
        controller.Move(currentMovement);

    }
}
