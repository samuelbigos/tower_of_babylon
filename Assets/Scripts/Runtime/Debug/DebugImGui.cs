using System;
using System.Collections.Generic;
using ImGuiNET;
using UImGui;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class DebugImGui : Singleton<DebugImGui>, Input.IDebugActions
{
    private struct RegisteredWindow
    {
        public string Id;
        public string Name;
        public Action Callback;
        public string InputActionName;
    }

    private float _timescale;
    private List<RegisteredWindow> _registeredWindows = new();
    private ImGuiWindowFlags _windowFlags = ImGuiWindowFlags.AlwaysAutoResize;
    private Dictionary<string, bool> _windowsSelected = new Dictionary<string, bool>();
    private Dictionary<string, string> _inputActionToWindowName = new Dictionary<string, string>();
    private Input _input;

    private const float _windowAlpha = 1.0f;

    protected override void Awake()
    {
        base.Awake();

        RegisterWindow("performance", "Performance", OnImGuiLayoutPerformance, "TogglePerformanceWindow");
        RegisterWindow("debug", "Debug", OnImGuiLayoutDebug, "ToggleDebugWindow");
    }

    private void Start()
    {
        foreach (RegisteredWindow window in _registeredWindows)
        {
            _windowsSelected[window.Id] = PlayerPrefs.GetInt(window.Id) == 1;
        }

        _input = new Input();
        //_input.Debug.SetCallbacks(this);
        //_input.Debug.Enable();
    }

    private void OnEnable()
    {
        UImGuiUtility.Layout += OnLayout;
    }

    private void OnDisable()
    {
        UImGuiUtility.Layout -= OnLayout;
    }

    private void OnLayout(UImGui.UImGui imGui)
    {
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("Menu"))
            {
                if (ImGui.MenuItem("Reset Save"))
                {
                }
                if (ImGui.MenuItem("Reload"))
                {
                }
                if (ImGui.MenuItem("Save and Quit"))
                {
                }
                ImGui.EndMenu();
            }
            
            if (ImGui.BeginMenu("Windows"))
            {
                foreach (RegisteredWindow window in _registeredWindows)
                {
                    bool selected = _windowsSelected[window.Id];
                    // if (ImGui.MenuItem($"{window.Name}",  Utilities.ActionToHotkeyString(_input.FindAction(window.InputActionName, true)), selected))
                    // {
                    //     SetWindowSelected(window.Id, selected);
                    // }
                }
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Debug"))
            {
                LayoutDebugMenu();
                ImGui.EndMenu();
            }

            ImGui.EndMainMenuBar();
        }

        foreach (RegisteredWindow window in _registeredWindows)
        {
            if (_windowsSelected[window.Id])
            {
                ImGui.SetNextWindowBgAlpha(_windowAlpha);
                if (ImGui.Begin(window.Name, _windowFlags))
                {
                    window.Callback?.Invoke();
                }
            }
        }
    }

    private void ToggleWindowSelected(InputAction inputAction)
    {
        string id = _inputActionToWindowName[inputAction.name];
        SetWindowSelected(id, _windowsSelected[id]);
    }
    private void SetWindowSelected(string id, bool selected)
    {
        _windowsSelected[id] = !selected;
        PlayerPrefs.SetInt(id, !selected ? 1 : 0);
    }

    private void LayoutDebugMenu()
    {
    }

    public void RegisterWindow(string id, string name, Action callback, string inputActionName)
    {
        RegisteredWindow window = new() {Id = id, Name = name, Callback = callback, InputActionName = inputActionName};

        _registeredWindows.Add(window);
        _windowsSelected.TryAdd(id, false);
        _inputActionToWindowName.TryAdd(inputActionName, id);
    }

    public void UnRegisterWindow(string id, Action callback)
    {
        int index = -1;
        for (int i = 0; i < _registeredWindows.Count; i++)
        {
            RegisteredWindow window = _registeredWindows[i];
            if (window.Id == id)
            {
                index = i;
                break;
            }
        }

        if (index != -1)
        {
            _registeredWindows.RemoveAt(index);
            _windowsSelected.TryAdd(id, false);
        }
    }

    private void OnImGuiLayoutPerformance()
    {
        ImGui.Text($"FPS: {1.0f / Time.smoothDeltaTime:F0}");
        
        ImGui.Text(" ### Processing");
        //ImGui.Text($"TimeProcess: {Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000.0f:F0}ms");
        // ImGui.Text($"ObjectCount: {Performance.GetMonitor(Performance.Monitor.ObjectCount):F0}");
        // ImGui.Text($"ObjectNodeCount: {Performance.GetMonitor(Performance.Monitor.ObjectNodeCount):F0}");
        // ImGui.Text($"ObjectResourceCount: {Performance.GetMonitor(Performance.Monitor.ObjectResourceCount):F0}");
        // ImGui.Text($"ObjectOrphanNodeCount: {Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount):F0}");
        //
        // ImGui.Text(" ### Rendering");
        // ImGui.Text($"RenderVerticesInFrame: {Performance.GetMonitor(Performance.Monitor.RenderVerticesInFrame):F0}");
        // ImGui.Text($"RenderDrawCallsInFrame: {Performance.GetMonitor(Performance.Monitor.RenderDrawCallsInFrame):F0}");
        // ImGui.Text(
        //     $"Render2dDrawCallsInFrame: {Performance.GetMonitor(Performance.Monitor.Render2dDrawCallsInFrame):F0}");
        //
        // ImGui.Text(" ### Memory");
        // ImGui.Text($"MemoryDynamic: {Performance.GetMonitor(Performance.Monitor.MemoryDynamic) / 1024.0f:F0}KiB");
        // ImGui.Text($"MemoryStatic: {Performance.GetMonitor(Performance.Monitor.MemoryStatic) / 1024.0f:F0}KiB");
        // ImGui.Text(
        //     $"MemoryMessageBufferMax: {Performance.GetMonitor(Performance.Monitor.MemoryMessageBufferMax) / 1024.0f:F0}KiB");
        //
        // ImGui.Text(" ### Physics");
        // ImGui.Text($"Physics3dActiveObjects: {Performance.GetMonitor(Performance.Monitor.Physics3dActiveObjects):F0}");
        // ImGui.Text($"Physics2dActiveObjects: {Performance.GetMonitor(Performance.Monitor.Physics2dActiveObjects):F0}");
        // ImGui.Text($"Physics3dIslandCount: {Performance.GetMonitor(Performance.Monitor.Physics3dIslandCount):F0}KiB");
        // ImGui.Text($"Physics2dIslandCount: {Performance.GetMonitor(Performance.Monitor.Physics2dIslandCount):F0}KiB");
    }

    private void OnImGuiLayoutDebug()
    {
        if (ImGui.SliderFloat("Timescale", ref _timescale, 0.0f, 1.0f))
        {
            Time.timeScale = _timescale;
        }
    }
    public void OnToggleWASD(InputAction.CallbackContext context)
    {
        throw new NotImplementedException();
    }
    public void OnTogglePerformanceWindow(InputAction.CallbackContext context)
    {
        if (context.performed)
            ToggleWindowSelected(context.action);
    }
    public void OnToggleDebugWindow(InputAction.CallbackContext context)
    {
        if (context.performed)
         ToggleWindowSelected(context.action);
    }
    public void OnToggleSteeringWindow(InputAction.CallbackContext context)
    {
        if (context.performed)
            ToggleWindowSelected(context.action);
    }
    public void OnToggleSplatterWindow(InputAction.CallbackContext context)
    {
        if (context.performed)
            ToggleWindowSelected(context.action);
    }
}
