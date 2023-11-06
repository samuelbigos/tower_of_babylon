using System;
using ImGuiNET;
using UnityEngine;
using UnityEngine.InputSystem;
using Vector3 = UnityEngine.Vector3;

public class Player : Singleton<Player>
{
    private Camera _camera;
    private PlayerInput _input;

    private bool _prevOverUi;

    public static Action<Player> OnPlayerCreated;
    public static Action<Player> OnPlayerDestroyed;
    
    protected override void Awake()
    {
        base.Awake();
        
        _camera = Camera.main;

        _input = GetComponent<PlayerInput>();
    }

    private void Start()
    {
        OnPlayerCreated?.Invoke(this);
    }

    protected override void OnDestroy()
    {
        OnPlayerDestroyed?.Invoke(this);
        
        base.OnDestroy();
    }

    private void Update()
    {
        // Disable player input if mouse over ImGui elements.
        ImGuiIOPtr io = ImGui.GetIO();
        if (io.WantCaptureMouse && !_prevOverUi)
        {
            _input.DeactivateInput();
            _prevOverUi = true;
        }
        else if (!io.WantCaptureMouse && _prevOverUi)
        {
            _input.ActivateInput();
            _prevOverUi = false;
        }

        Vector3 camPos = transform.position;
        camPos.z = _camera.transform.position.z;
        _camera.transform.position = camPos;
    }
}
