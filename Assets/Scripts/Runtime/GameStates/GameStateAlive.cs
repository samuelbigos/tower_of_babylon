using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStateAlive : MonoBehaviour, IGameState
{
    private Player _player;
    
    public void OnEnter(IGameState prevState)
    {
        _player = Player.Instance;
        _player.Camera.gameObject.SetActive(true);
    }
    
    public void OnExit(IGameState newState)
    {
    }
    
    public void ManualUpdate()
    {
    }
    
    public bool ShouldEnter(IGameState currentState)
    {
        if (GSM.Instance.CurrentState is GameStateIntro && (GSM.Instance.CurrentState as GameStateIntro).IntroComplete)
            return true;

        if (GSM.Instance.CurrentState is GameStateElevator && (GSM.Instance.CurrentState as GameStateElevator).ElevatorComplete)
            return true;

        if (GSM.Instance.CurrentState is GameStateDead && !Player.Instance.IsDead)
            return true;
        
        return false;
    }
}
