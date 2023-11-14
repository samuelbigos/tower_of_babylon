using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;

public class Elevator : MonoBehaviour
{
    [SerializeField] private float _initiateTime;
    [SerializeField] private float _transitionTime;
    [SerializeField] private Transform _moveTarget;

    public bool Complete => _state == State.Complete;
    
    private enum State
    {
        Initiating,
        Moving,
        Complete,
    }
    
    private float _timer;
    private State _state = State.Initiating;
    private Vector3 _startPosition;
    
    private void FixedUpdate()
    {
        if (GSM.Instance.CurrentState is not GameStateElevator)
            return;

        _timer += Time.deltaTime;

        switch (_state)
        {
            case State.Initiating:
                if (_timer > _initiateTime)
                {
                    _state = State.Moving;
                    _startPosition = transform.position;
                    _timer = 0.0f;
                }
                break;
            case State.Moving:
                float t = Easing.InOut(Mathf.Clamp01(_timer / _transitionTime));
                Vector3 pos = _startPosition;
                pos.y = _startPosition.y + (_moveTarget.transform.position.y - _startPosition.y) * t;
                transform.position = pos;
                if (_timer >= _transitionTime)
                {
                    _state = State.Complete;
                }
                break;
            case State.Complete:
            default:
                break;
        }
    }
}
