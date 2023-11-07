using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialSwapper : MonoBehaviour
{
    [SerializeField] private Material _matSwap;
    
    private void Awake()
    {
        // MeshRenderer[] renderers = transform.GetComponentsInChildren<MeshRenderer>();
        // foreach (MeshRenderer mr in renderers)
        // {
        //     mr.material = _matSwap;
        // }
        //
        // MeshFilter[] mfs = transform.GetComponentsInChildren<MeshFilter>();
        // foreach (MeshFilter mf in mfs)
        // {
        //     mf.mesh.bounds = new Bounds(Vector3.zero, new Vector3(999999.0f, 999999.0f, 999999.0f));
        // }
    }
}
