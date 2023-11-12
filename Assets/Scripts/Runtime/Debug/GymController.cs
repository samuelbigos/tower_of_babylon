using System.Collections;
using System.Collections.Generic;
using ImGuiNET;
using UnityEngine;

public class GymController : MonoBehaviour
{
    private void Start()
    {
        DebugImGui.Instance.RegisterWindow("gym", "Gym Controller", OnImGuiLayout, "ToggleGymWindow");
    }

    private void OnImGuiLayout()
    {
        if (ImGui.Button("Swing Tutorial"))
        {
            Player.Instance.Teleport(new Vector3(1.0f, 11.0f, 0.0f));
        }
        if (ImGui.Button("Extend Tutorial"))
        {
            Player.Instance.Teleport(new Vector3(60.0103531f,10.5019999f,-1.42196154e-06f));
        }
    }
}
