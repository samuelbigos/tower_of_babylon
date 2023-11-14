using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGameState
{
    public void OnEnter(IGameState prevState);
    public void OnExit(IGameState newState);
    public void Update();
    public bool ShouldEnter(IGameState currentState);
}
