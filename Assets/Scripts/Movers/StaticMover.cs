using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticMover : MonoBehaviour
{
    [SerializeField]
    public Vector3 movementDirection = Vector3.forward;
    [SerializeField]
    public float movementSpeed = 5.0f;

    private List<CharacterController> controllers = new List<CharacterController>();

    private void Awake()
    {
        movementDirection = transform.rotation * movementDirection;
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
