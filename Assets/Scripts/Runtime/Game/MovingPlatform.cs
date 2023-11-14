using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshCollider))]
public class MovingPlatform : MonoBehaviour
{
    private Vector3 _prevPos;
    private MeshCollider _collider;

    private void Awake()
    {
        _collider = GetComponent<MeshCollider>();
    }

    private void FixedUpdate()
    {
        Vector3 delta = transform.position - _prevPos;
        _prevPos = transform.position;

        if (Player.Instance.Controller.GroundCollider == _collider)
        {
            Player.Instance.Controller.ApplyPositionDelta(delta);
        }
    }
}
