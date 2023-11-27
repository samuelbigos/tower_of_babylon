using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scarf : Singleton<Scarf>
{
    [SerializeField] private List<Cloth> _levels;

    private SkinnedMeshRenderer _smr;
    private int _current = 0;

    protected override void Awake()
    {
        base.Awake();
        foreach (Cloth cloth in _levels)
        {
            cloth.transform.parent = transform.parent;
        }
        _levels[_current].gameObject.SetActive(true);
    }
    private void Update()
    {
        float offset = GrappleController.Instance.IsGrappling ? 1.0f : 2.0f;
        
        _levels[_current].transform.position = Player.Instance.transform.position + new Vector3(0.0f, offset,0.0f);

        Vector3 perp = Utilities.Flatten(Player.Instance.transform.position).normalized;
        _levels[_current].externalAcceleration = -Player.Instance.Controller.Velocity + Vector3.down * 5.0f + perp * 10.0f;
    }

    public void Upgrade()
    {
        if (_current >= _levels.Count - 1)
            return;
        
        _levels[_current].gameObject.SetActive(false);
        _current++;
        _levels[_current].gameObject.SetActive(true);
    }
}
