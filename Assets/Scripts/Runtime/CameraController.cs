using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float _segmentWidth = 50.0f;
    [SerializeField] private Vector3 _origin = new Vector3(0.0f, 0.0f, 20.0f);
    [SerializeField] private float _x;
    
    private Camera _camera;
    
    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void Start()
    {
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

        Vector3 pos = (Player.Instance.transform.position - _origin);
        pos.y = 0.0f;
        pos = pos.normalized * 50.0f;
        pos.y = Player.Instance.transform.position.y;
        _camera.transform.position = pos;
            
        Vector3 lookAt = _origin;
        lookAt.y = pos.y;
        transform.LookAt(lookAt);
    }
}
