using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticMover : MonoBehaviour
{
    [SerializeField]
    public Vector3 movementDirection = Vector3.forward;
    [SerializeField]
    public float movementSpeed = 5.0f;

    private void Awake()
    {
        movementDirection = transform.rotation * movementDirection;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent<CharacterController>(out CharacterController controller))
        {
            controller.Move(movementDirection * movementSpeed * Time.deltaTime);
        } else if (other.TryGetComponent<Rigidbody>(out Rigidbody otherBody))
        {
            otherBody.Move(otherBody.position + movementDirection * Time.deltaTime, otherBody.rotation);
        }
    }

}
