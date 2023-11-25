using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Monument : MonoBehaviour
{
    [SerializeField] public string _text;
    
    private void Start()
    {
        BoxCollider collider = gameObject.AddComponent<BoxCollider>();
        collider.center = new Vector3(0.0f, 4.0f, 0.0f);
        collider.size = new Vector3(10.0f, 10.0f, 10.0f);
        collider.isTrigger = true;

        gameObject.layer = (int) Utilities.PhysicsLayers.Monument;
    }

    public void OnActivate()
    {
        Debug.Log("Activate");
        UI.Instance.SetText(_text);
    }

    public void OnDeactivate()
    {
        Debug.Log("Deactivate");
    }
}
