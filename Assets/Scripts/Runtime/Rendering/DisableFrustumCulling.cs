using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableFrustumCulling : MonoBehaviour
{
    private Camera _camera;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void OnPreCull()
    {
        // Need to disable CPU frustum culling because we're changing vertex positions.
        // EDIT doesn't seem to work.
        _camera.cullingMatrix = Matrix4x4.Ortho(-99999, 99999, -99999, 99999, 0.001f, 99999) * 
                                Matrix4x4.Translate(Vector3.forward * -99999 / 2f) * 
                                _camera.worldToCameraMatrix;
    }
}
