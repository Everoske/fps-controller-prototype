using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSTracker : MonoBehaviour
{
    private void Update()
    {
        Debug.Log(1 / Time.deltaTime);
    }
}
