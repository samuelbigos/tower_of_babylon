using System;
using System.Collections;
using System.Collections.Generic;
using nickmaltbie.OpenKCC.Utils;
using UnityEngine;

public class CameraController : Singleton<CameraController>
{
    [SerializeField] private float _cameraDistance = 20.0f;
    [SerializeField] private float _yOffset = 2.0f;
    
    private Camera _camera;

    private float _playerDistanceMod = 1.0f;
    
    protected override void Awake()
    {
        base.Awake();
        
        _camera = GetComponent<Camera>();
    }

    public void SetPlayerDistanceMod(float mod)
    {
        _playerDistanceMod = mod;
    }
    
    private void FixedUpdate()
    {
        Vector3 pos = Utilities.Flatten(Player.Instance.transform.position).normalized * (_cameraDistance + Game.TowerRadius);
        if (!Game.WrapAroundTower)
        {
            pos = Player.Instance.transform.position - Vector3.forward * _cameraDistance * _playerDistanceMod;
        }
        
        pos.y = Player.Instance.transform.position.y + _yOffset;
        _camera.transform.position = pos;

        Vector3 lookAt = Player.Instance.transform.position;
        transform.LookAt(lookAt);
    }
}
