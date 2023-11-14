using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UImGui;
using UnityEngine;
using Utils;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class GameStateIntro : MonoBehaviour, IGameState
{
    [SerializeField] private float _transitionTime = 5.0f;
    [SerializeField] private Camera _targetCamera;
    [SerializeField] private Camera _camera;

    public bool IntroComplete => _canExit;
    
    private bool _transitioning;
    private float _timer;
    private Vector3 _initialPosition;
    private Quaternion _initialRotation;
    private bool _canExit;
    private bool _speed;

    public void OnEnter(IGameState prevState)
    {
        _initialPosition = _camera.transform.position;
        _initialRotation = _camera.transform.rotation;
        
        _camera.fieldOfView = _targetCamera.fieldOfView;
        _camera.nearClipPlane = _targetCamera.nearClipPlane;
        _camera.farClipPlane = _targetCamera.farClipPlane;

        if (Game.Instance.InGym)
        {
            _timer = _transitionTime;
        }
    }
    
    public void OnExit(IGameState newState)
    {
    }
    
    public void ManualUpdate()
    {
        if ((InputManager.Instance.PlayerShoot.action.WasPressedThisFrame() || Game.Instance.InGym) && !_canExit)
        {
            if (_transitioning)
            {
                _speed = true;
            }
            _transitioning = true;
        }

        if (_transitioning)
        {
            Transform targetTransform = _targetCamera.transform;

            _camera.transform.position = Vector3.Lerp(_initialPosition, targetTransform.position, T());
            _camera.transform.rotation = Quaternion.Slerp(_initialRotation, targetTransform.rotation, T());

            _timer += Time.deltaTime * (_speed ? 10.0f : 1.0f);

            if (_timer >= _transitionTime)
            {
                _camera.transform.position = targetTransform.position;
                _camera.transform.rotation = targetTransform.rotation;

                _transitioning = false;
                _canExit = true;
                
                _camera.gameObject.SetActive(false);
            }
        }
    }

    private float T()
    {
        return Easing.InOut(_timer / _transitionTime);
    }
    
    public bool ShouldEnter(IGameState currentState)
    {
        return currentState == null;
    }
}
