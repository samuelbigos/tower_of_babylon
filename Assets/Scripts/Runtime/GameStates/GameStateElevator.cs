using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStateElevator : MonoBehaviour, IGameState
{
    private Player _player;

    public void OnEnter(IGameState prevState)
    {
    }
    
    public void OnExit(IGameState newState)
    {
    }
    
    public void ManualUpdate()
    {
    }
    
    public bool ShouldEnter(IGameState currentState)
    {
        return GSM.Instance.CurrentState is GameStateAlive && Game.Instance.PlayerOnTopOfTower;
    }
}
