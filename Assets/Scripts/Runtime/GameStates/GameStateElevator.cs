using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStateElevator : MonoBehaviour, IGameState
{
    [SerializeField] private Elevator _elevator;
    
    private Player _player;

    public bool ElevatorComplete => _elevator.Complete;
    
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
