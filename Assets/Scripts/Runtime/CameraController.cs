using System;
using System.Collections;
using System.Collections.Generic;
using nickmaltbie.OpenKCC.Utils;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float _cameraDistance = 20.0f;
    [SerializeField] private float _yOffset = 2.0f;
    
    private Camera _camera;
    
    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void Update()
    {
        Vector3 pos = Utilities.Flatten(Player.Instance.transform.position).normalized * (_cameraDistance + Utilities.TOWER_RADIUS);
        if (!Game.WrapAroundTower)
        {
            pos = Player.Instance.transform.position - Vector3.forward * _cameraDistance;
        }
        
        pos.y = Player.Instance.transform.position.y + _yOffset;
        _camera.transform.position = pos;

        Vector3 lookAt = Player.Instance.transform.position;
        transform.LookAt(lookAt);
    }
}
