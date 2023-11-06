using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game : MonoBehaviour
{
    [SerializeField] private float _segmentHeight;
    [SerializeField] private GameObject _towerSegment;
    [SerializeField] private Player _player;
    [SerializeField] private int _numSegments = 5;

    private List<GameObject> _segments = new List<GameObject>();
    private int _prevPlayerSegment = -99999;

    private void Start()
    {
        for (int i = 0; i <= _numSegments; i++)
        {
            GameObject segment = Instantiate(_towerSegment, Vector3.zero, Quaternion.identity);
            _segments.Add(segment);
        }
    }

    private void Update()
    {
        int playerSegment = Mathf.FloorToInt(_player.transform.position.y / _segmentHeight);
        if (_prevPlayerSegment != playerSegment)
        {
            UpdateSegmentPositions(playerSegment);
        }
        _prevPlayerSegment = playerSegment;
    }

    private void UpdateSegmentPositions(int playerSegment)
    {
        for (int i = 0; i < _numSegments; i++)
        {
            GameObject segment = _segments[i];
            int s = playerSegment + i - (_numSegments / 2) + 1;
            Vector3 pos = new Vector3(0.0f, s * _segmentHeight, 0.0f);
            segment.transform.position = pos;
        }
    }
}
