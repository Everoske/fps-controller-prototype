using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [SerializeField]
    private float horizontalRange = 0f;
    [SerializeField]
    private float verticalRange = 2f;

    private Vector3 initialPosition;
    private float targetX;
    private float targetY;

    private void Awake()
    {
        initialPosition = transform.position;
        targetX = initialPosition.x + horizontalRange;
        targetY = initialPosition.y + verticalRange;
    }

    private void Update()
    {
        ProcessMove();
    }

    private void ProcessMove()
    {
        
    }
}
