using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Utils;

public class Game : Singleton<Game>
{
    [SerializeField] private bool _gymMode;
    
    [SerializeField] private Player _player;
    [SerializeField] private bool _wrapAroundTower = true;
    [SerializeField] private float _defaultTowerRadius = 50.0f;
    [SerializeField] private float _tipTowerRadius = 10.0f;

    [SerializeField] private BoxCollider _tipVolume;
    [SerializeField] private BoxCollider _elevatorVolume;

    [SerializeField] private Transform _tunnelTop;
    [SerializeField] private Transform _tunnelBot;
    
    private float _towerRadius;

    private bool _playerInElevatorVolume;
    private float _radiusOnEnterElevatorVolume;

    private bool _playerInRoofVolume;
    private bool _playerInTunnelVolume;
    private float _playerZOnEnterTunnelVolume;
    private bool _playerInTeleportVolume;
    private float _teleportZDelta;
    private bool _playerInTowerBottomZone;

    private float _atanPrev;
    private float _angle;

    public bool ShouldGrapple = true;
    public bool InGym => _gymMode;

    public static float TowerRadius => Instance._towerRadius;
    public static float TowerCircumference => Instance._towerRadius * Mathf.PI * 2.0f;

    public static bool WrapAroundTower => Instance._wrapAroundTower;

    public bool PlayerOnTopOfTower;

    protected override void Awake()
    {
        base.Awake();

        _towerRadius = _defaultTowerRadius;
    }

    private void Update()
    {
        GSM.Instance.ManualUpdate();

        if (_gymMode)
            return;
        
        if (!PlayerOnTopOfTower)
        {
            float tipBottom = _tipVolume.transform.position.y - _tipVolume.size.y * 0.5f;
            float tipTop = _tipVolume.transform.position.y + _tipVolume.size.y * 0.5f;

            float t = 1.0f - Mathf.InverseLerp(tipBottom, tipTop, _player.transform.position.y);
            _towerRadius = _tipTowerRadius + (_defaultTowerRadius - _tipTowerRadius) * t;

            Vector3 playerPos = _player.transform.position;
            // if (_elevatorVolume.bounds.Contains(_player.transform.position))
            // {
            //     Vector3 posN = new Vector3(playerPos.x, 0.0f, playerPos.z);
            //     posN = posN.normalized;
            //     float atan = -Mathf.Atan2(posN.x, posN.z);
            //
            //     float atanDelta = atan - _atanPrev;
            //     _atanPrev = atan;
            //
            //     if (!_playerInElevatorVolume)
            //     {
            //         _radiusOnEnterElevatorVolume = _towerRadius;
            //         _playerInElevatorVolume = true;
            //         _player.Controller._disallowLeftMovement = true;
            //         //ShouldGrapple = false;
            //     }
            //     else
            //     {
            //         _angle += Mathf.Max(atanDelta, 0.0f);
            //         t = _angle / Mathf.PI;
            //         _towerRadius = Mathf.Max((1.0f - t) * _radiusOnEnterElevatorVolume, 5.0f);
            //
            //         if (t > 0.9f)
            //         {
            //             PlayerOnTopOfTower = true;
            //         }
            //     }
            // }
        }

        if (_playerInElevatorVolume && !_elevatorVolume.bounds.Contains(_player.transform.position))
        {
            _playerInElevatorVolume = false;
            PlayerOnTopOfTower = false;
            _player.Controller._disallowLeftMovement = false;
            ShouldGrapple = true;
        }
    }
    
    public static Vector3 ProjectOnTower(Vector3 point)
    {
        Vector3 pos = point;
        pos.y = 0.0f;
        pos = pos.normalized * Instance._towerRadius;
        pos.y = point.y;
        return pos;
    }
}
