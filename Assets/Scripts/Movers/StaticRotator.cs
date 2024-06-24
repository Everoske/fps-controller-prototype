using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticRotator : MonoBehaviour
{
    [SerializeField]
    private float rotationSpeed = 90.0f;

    private Rigidbody moverRB;

    private List<CharacterController> controllers = new List<CharacterController>();
    private List<Rigidbody> connectedBodies = new List<Rigidbody>();

    private void Awake()
    {
        moverRB = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        float rotationAmount = rotationSpeed * Time.fixedDeltaTime;
        Quaternion deltaRotation = Quaternion.Euler(transform.up * rotationAmount);
        moverRB.MoveRotation(moverRB.rotation * deltaRotation);

        RotateRiders(deltaRotation, rotationAmount);
    }

    private void RotateRiders(Quaternion delta, float rotationAmount)
    {
        foreach (CharacterController controller in controllers)
        {
            Vector3 controllerConnection = controller.transform.position - transform.position;
            Vector3 newControllerPosition = delta * controllerConnection;
            controller.Move(newControllerPosition - controllerConnection);

            controller.transform.Rotate(Vector3.up * rotationAmount);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<CharacterController>(out CharacterController controller))
        {
            controllers.Add(controller);
        } else if (other.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            connectedBodies.Add(rb);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<CharacterController>(out CharacterController controller))
        {
            controllers.Remove(controller);
        }
        else if (other.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            connectedBodies.Remove(rb);
        }
    }
}
