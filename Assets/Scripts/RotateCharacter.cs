using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateCharacter : MonoBehaviour
{
    public float speed = 0.5f;

    private float _lastX;
    private Vector3 _lastRot;

    void LateUpdate()
    {
        if (Input.GetMouseButtonDown(0))
            _lastX = Input.mousePosition.x;

        if (Input.GetMouseButton(0))
        {
            float rotate = _lastX - Input.mousePosition.x;
            transform.localEulerAngles = _lastRot + new Vector3(0, rotate * speed, 0);
        }

        if (Input.GetMouseButtonUp(0))
            _lastRot = transform.localEulerAngles;
    }
}


