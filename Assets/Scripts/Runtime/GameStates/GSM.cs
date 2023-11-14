using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GSM : Singleton<GSM>
{
    private IGameState _currentState;
    public IGameState CurrentState => _currentState;

    private List<IGameState> _states;

    protected override void Awake()
    {
        base.Awake();
        
        _states = new List<IGameState>();
        _states.Add(new GameStateIntro());
        _states.Add(new GameStateLimbo());
        _states.Add(new GameStateAlive());
    }

    public void ManualUpdate()
    {
        foreach (IGameState state in _states)
        {
            if (state.ShouldEnter(_currentState))
            {
                if (_currentState != null)
                    Debug.Log($"Changing state from {_currentState.GetType().Name} to {state.GetType().Name}");

                _currentState?.OnExit(state);
                state.OnEnter(_currentState);
                _currentState = state;
            }
        }
        
        if (_currentState != null)
            _currentState.Update();
    }
}
