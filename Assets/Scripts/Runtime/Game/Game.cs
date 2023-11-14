using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Utils;

public class Game : Singleton<Game>
{
    [SerializeField] private Player _player;
    [SerializeField] private bool _wrapAroundTower = true;
    [SerializeField] private float _defaultTowerRadius = 50.0f;
    [SerializeField] private float _tipTowerRadius = 10.0f;

    [SerializeField] private BoxCollider _tipVolume;
    [SerializeField] private BoxCollider _elevatorVolume;
    [SerializeField] private BoxCollider _roofVolume;
    [SerializeField] private BoxCollider _tunnelVolume;
    [SerializeField] private BoxCollider _tunnelVolume2;
    [SerializeField] private BoxCollider _teleportVolume;
    [SerializeField] private BoxCollider _towerBottomVolume;

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

    public static float TowerRadius => Instance._towerRadius;
    public static float TowerCircumference => Instance._towerRadius * Mathf.PI * 2.0f;

    public static bool WrapAroundTower => Instance._wrapAroundTower;

    public bool PlayerOnTopOfTower;

    protected override void Awake()
    {
        base.Awake();

        _towerRadius = _defaultTowerRadius;
    }

    private bool InTunnelVolume()
    {
        return _tunnelVolume.bounds.Contains(_player.transform.position) ||
               _tunnelVolume2.bounds.Contains(_player.transform.position);
    }

    private void Update()
    {
        GSM.Instance.ManualUpdate();

        if (!PlayerOnTopOfTower)
        {
            float tipBottom = _tipVolume.transform.position.y - _tipVolume.size.y * 0.5f;
            float tipTop = _tipVolume.transform.position.y + _tipVolume.size.y * 0.5f;

            float t = 1.0f - Mathf.InverseLerp(tipBottom, tipTop, _player.transform.position.y);
            _towerRadius = _tipTowerRadius + (_defaultTowerRadius - _tipTowerRadius) * t;

            Vector3 playerPos = _player.transform.position;
            if (_elevatorVolume.bounds.Contains(_player.transform.position))
            {
                Vector3 posN = new Vector3(playerPos.x, 0.0f, playerPos.z);
                posN = posN.normalized;
                float atan = -Mathf.Atan2(posN.x, posN.z);

                float atanDelta = atan - _atanPrev;
                _atanPrev = atan;

                if (!_playerInElevatorVolume)
                {
                    _radiusOnEnterElevatorVolume = _towerRadius;
                    _playerInElevatorVolume = true;
                    _player.Controller._disallowLeftMovement = true;
                    ShouldGrapple = false;
                }
                else
                {
                    _angle += Mathf.Max(atanDelta, 0.0f);
                    t = _angle / Mathf.PI;
                    _towerRadius = Mathf.Max((1.0f - t) * _radiusOnEnterElevatorVolume, 5.0f);

                    if (t > 0.9f)
                    {
                        PlayerOnTopOfTower = true;
                    }
                }
            }
        }

        if (_playerInElevatorVolume && !_elevatorVolume.bounds.Contains(_player.transform.position))
        {
            _playerInElevatorVolume = false;
            PlayerOnTopOfTower = false;
            _player.Controller._disallowLeftMovement = false;
            ShouldGrapple = true;
        }

        if (!_playerInRoofVolume && _roofVolume.bounds.Contains(_player.transform.position))
        {
            _playerInRoofVolume = true;
            _wrapAroundTower = false;
        }
        
        if (!_playerInTunnelVolume && InTunnelVolume())
        {
            _playerInTunnelVolume = true;
            _player.Controller._allowDepthMovement = true;
            _playerZOnEnterTunnelVolume = _player.transform.position.z - _tunnelVolume.transform.position.z;
        }

        if (_playerInTunnelVolume && !InTunnelVolume())
        {
            _playerInTunnelVolume = false;
            _player.Controller._allowDepthMovement = false;
        }

        if (_playerInTunnelVolume)
        {
            float mod = _player.transform.position.z - _tunnelVolume.transform.position.z - _playerZOnEnterTunnelVolume + _teleportZDelta;
            mod /= 50.0f;
            mod = Easing.InOut(Mathf.Clamp01(mod));
            mod *= 0.75f;
            mod = 1.0f - mod;
            CameraController.Instance.SetPlayerDistanceMod(mod);
            ShouldGrapple = false;
        }

        if (!_playerInTeleportVolume && _teleportVolume.bounds.Contains(_player.transform.position))
        {
            float zPrev = _player.transform.position.z;
            Vector3 offset = _player.transform.position - _tunnelTop.transform.position;
            _player.transform.position = _tunnelBot.position + offset;
            _teleportZDelta = zPrev - _player.transform.position.z;
        }
        
        if (!_playerInTowerBottomZone && _towerBottomVolume.bounds.Contains(_player.transform.position))
        {
            _playerInTowerBottomZone = true;
            _wrapAroundTower = true;
            _player.Controller._allowDepthMovement = false;
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
