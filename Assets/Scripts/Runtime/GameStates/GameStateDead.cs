using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStateDead : MonoBehaviour, IGameState
{
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
        return Player.Instance.IsDead;
    }
}
