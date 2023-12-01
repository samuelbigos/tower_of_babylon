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

    [SerializeField] private BoxCollider _elevatorVolume;
    [SerializeField] private Transform _towerTop;
    
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

    public bool TimingRun = false;
    public float RunTime = 0.0f;
    
    protected override void Awake()
    {
        base.Awake();

        _towerRadius = _defaultTowerRadius;
    }

    private void Update()
    {
        GSM.Instance.ManualUpdate();

        if (TimingRun)
        {
            RunTime += Time.deltaTime;
        }

        if (_gymMode)
            return;
        
        if (!PlayerOnTopOfTower)
        {
            if (_elevatorVolume.bounds.Contains(_player.transform.position))
            {
                PlayerOnTopOfTower = true;
                ShouldGrapple = false;
                _towerRadius = 5.0f;
                _player.transform.position = ProjectOnTower(_towerTop.transform.position);
            }
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
