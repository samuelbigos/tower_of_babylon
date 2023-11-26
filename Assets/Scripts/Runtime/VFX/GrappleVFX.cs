using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;
using UnityEngine.VFX;

public class GrappleVFX : MonoBehaviour
{
    [SerializeField] private VisualEffect _arc;
    [SerializeField] private Transform _grappleP1;
    [SerializeField] private Transform _grappleP2;
    [SerializeField] private Transform _grappleP3;
    [SerializeField] private Transform _grappleP4;

    private bool _toDisable;

    public void On(Vector3 from, Vector3 to, Color color)
    {
        _arc.SetVector4("Color", color);
        On(from, to);
    }
    
    public void On(Vector3 from, Vector3 to)
    {
        transform.position = from;
        
        _grappleP1.position = from;
        _grappleP2.position = from;
        _grappleP3.position = to;
        _grappleP4.position = to;
        
        _arc.enabled = true;
    }

    public void Off()
    {
        _toDisable = true;
    }

    public void Update()
    {
        if (_toDisable)
        {
            _toDisable = false;
            _grappleP1.transform.position = Vector3.zero;
            _grappleP2.transform.position = Vector3.zero;
            _grappleP3.transform.position = Vector3.zero;
            _grappleP4.transform.position = Vector3.zero;
        }
    }
}
