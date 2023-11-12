using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game : Singleton<Game>
{
    [SerializeField] private Player _player;
    [SerializeField] private bool _wrapAroundTower = true;

    public static bool WrapAroundTower => Instance._wrapAroundTower;
}
