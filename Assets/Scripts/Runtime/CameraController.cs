using System;
using System.Collections;
using System.Collections.Generic;
using nickmaltbie.OpenKCC.Utils;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float _cameraDistance = 20.0f;
    private Camera _camera;
    
    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void Update()
    {
        // float playerPosX = Player.Instance.transform.position.x;
        // Vector3 pos = Vector3.zero;
        // pos.x = Mathf.Sin(playerPosX / _segmentWidth * Mathf.PI * 2.0f) * _segmentWidth;
        // pos.z = -Mathf.Cos(playerPosX / _segmentWidth * Mathf.PI * 2.0f) * _segmentWidth;
        // pos.y = Player.Instance.transform.position.y;
        // transform.position = _origin + pos;
        // Vector3 lookAt = _origin;
        // lookAt.y = pos.y;
        // transform.LookAt(lookAt);

        Vector3 pos = Utilities.Flatten(Player.Instance.transform.position).normalized * (_cameraDistance + Utilities.TOWER_RADIUS );
        pos.y = Player.Instance.transform.position.y;
        _camera.transform.position = pos;
            
        Vector3 lookAt = Vector3.zero;
        lookAt.y = pos.y;
        transform.LookAt(lookAt);
    }
}
