using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntroCam : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == (int)Utilities.PhysicsLayers.Monument)
        {
            Monument monument = other.gameObject.GetComponent<Monument>();
            UI.Instance.EnterCinematic();
            UI.Instance.SetText(monument._text);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == (int)Utilities.PhysicsLayers.Monument)
        {
            UI.Instance.ExitCinematic();
        }
    }
}
