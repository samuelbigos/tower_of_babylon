using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cannon : MonoBehaviour
{
    [SerializeField] private Cannonball _cannonball;
    [SerializeField] private float _shotFrequency = 5.0f;
    [SerializeField] private float _shotPower;
    [SerializeField] private Transform _barrelBack;
    [SerializeField] private Transform _barrelFront;
    
    private float _timer;

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer > _shotFrequency)
        {
            _timer = 0.0f;
            Shoot();
        }
    }

    private void Shoot()
    {
        Cannonball ball = Instantiate(_cannonball);
        Vector3 dir = (_barrelFront.position - _barrelBack.position).normalized;
        ball.transform.position = _barrelFront.position + dir * 1.0f;
        ball.Launch(dir * _shotPower);
    }
}
