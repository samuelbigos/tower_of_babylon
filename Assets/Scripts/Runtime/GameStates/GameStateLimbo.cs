using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStateLimbo : IGameState
{
    public void OnEnter(IGameState prevState)
    {
    }
    
    public void OnExit(IGameState newState)
    {
    }
    
    public void Update()
    {
    }
    
    public bool ShouldEnter(IGameState currentState)
    {
        return false;
    }
}
