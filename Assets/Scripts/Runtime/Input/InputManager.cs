using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : Singleton<InputManager>
{
    [SerializeField] private InputActionReference _playerShoot;

    public InputActionReference PlayerShoot => _playerShoot;
}
