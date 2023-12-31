using UnityEngine;
using UnityEngine.InputSystem;

public static class Utilities
{
    public static readonly System.Random RNG = new System.Random(0);

    public enum PhysicsLayers
    {
        Default = 0,
        TransparentFX = 1,
        IgnoreRaycast = 2,
        Player = 3,
        Water = 4,
        UI = 5,
        KillZone = 6,
        Blocker = 7,
        Grapple = 8,
        Monument = 10,
    }

    public const int KillZoneCollisionMask = 1 << (int)PhysicsLayers.KillZone;
    public const int BlockerCollisionMask = 1 << (int)PhysicsLayers.KillZone;
    public const int GrappleCollisionMask = 1 << (int)PhysicsLayers.Grapple;

    public static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0.0f, vec.z);
    }
    
    // https://answers.unity.com/questions/421968/normal-distribution-random.html
    public static float RandomGaussian(float minValue = -1.0f, float maxValue = 1.0f, System.Random rng = null)
    {
        rng ??= RNG;
        
        float u, v, S;
     
        do
        {
            u = 2.0f * (float)rng.NextDouble() - 1.0f;
            v = 2.0f * (float)rng.NextDouble() - 1.0f;
            S = u * u + v * v;
        }
        while (S >= 1.0f);
     
        // Standard Normal Distribution
        float std = u * Mathf.Sqrt(-2.0f * Mathf.Log(S) / S);
     
        // Normal Distribution centered between the min and max value
        // and clamped following the "three-sigma rule"
        float mean = (minValue + maxValue) / 2.0f;
        float sigma = (maxValue - mean) / 3.0f;
        return Mathf.Clamp(std * sigma + mean, minValue, maxValue);
    }
    
    public static float EaseInOutQuad(float x)
    {
        return x < 0.5 ? 8 * x * x * x * x : 1 - Mathf.Pow(-2 * x + 2, 4) / 2;
    }

    public static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1.0f;

        return 1 + c3 * Mathf.Pow(x - 1, 3) + c1 * Mathf.Pow(x - 1, 2);
    }

    public static string ActionToHotkeyString(InputAction action)
    {
        if (action == null) return "";
        string hotkey = "";
        for (int i = 0; i < action.controls.Count; i++)
        {
            InputControl control = action.controls[i];
            hotkey += control.displayName;
            if (i < action.controls.Count - 1)
                hotkey += "+";
        }
        return hotkey;
    }
    
    public static bool IntersectPlane(Vector3 planeNormal, Vector3 planeOrigin, Vector3 rayDir, Vector3 rayOrigin, out float t)
    {
        float denom = Vector3.Dot(planeNormal, rayDir);
        if (denom > 1e-6) 
        {
            Vector3 p0l0 = planeOrigin - rayOrigin;
            t = Vector3.Dot(p0l0, planeNormal) / denom; 
            return (t >= 0);
        }

        t = 0;
        return false;
    }
}
