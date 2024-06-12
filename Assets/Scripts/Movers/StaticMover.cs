using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticMover : MonoBehaviour
{
    [SerializeField]
    private Vector3 movementDirection = Vector3.forward;
    [SerializeField]
    private float movementSpeed = 5.0f;

    private List<CharacterController> controllers = new List<CharacterController>();

    private void Awake()
    {
        movementDirection = transform.rotation * movementDirection;
    }

    private void FixedUpdate()
    {
        foreach (var controller in controllers)
        {
            controller.Move(movementDirection * movementSpeed * Time.fixedDeltaTime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<CharacterController>(out CharacterController controller))
        {
            controllers.Add(controller);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<CharacterController>(out CharacterController controller))
        {
            controllers.Remove(controller);
        }
    }

}
