using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements.Experimental;
using Easing = Utils.Easing;

public class GrapplePuzzle : MonoBehaviour
{
    [SerializeField] private List<SphereCollider> _pegs;
    [SerializeField] private List<Transform> _plaquePegs;
    [SerializeField] private GrappleVFX _plaqueVFXPrefab;
    [SerializeField] private List<Vector2Int> _requiredConnections;
    [SerializeField] private List<GameObject> _doors;
    [SerializeField] private float _doorOpenTime = 5.0f;
    [SerializeField] private AudioSource _doorOpenSFX;

    private Dictionary<Vector2Int, GrappleVFX> _plaqueVFXs = new Dictionary<Vector2Int, GrappleVFX>();

    private Vector2Int[] _dirs =
    {
        new Vector2Int(1, -1),
        new Vector2Int(1, 0),
        new Vector2Int(1, 1),
        new Vector2Int(0, 1),
    };
    private Vector2Int[] _coords =
    {
        new Vector2Int(0, 0),
        new Vector2Int(1, 0),
        new Vector2Int(2, 0),
        new Vector2Int(0, 1),
        new Vector2Int(1, 1),
        new Vector2Int(2, 1),
        new Vector2Int(0, 2),
        new Vector2Int(1, 2),
        new Vector2Int(2, 2)
    };
    private Dictionary<Vector2Int, int> _coordToIndex = new Dictionary<Vector2Int, int>();

    private bool _puzzleComplete;
    private float _doorOpenTimer;
    private bool _doorOpen;
    private List<Quaternion> _doorInitialRot = new List<Quaternion>();
    private List<Vector2Int> _allConnections = new List<Vector2Int>();
    private Dictionary<Vector2Int, bool> _connectionOn = new Dictionary<Vector2Int, bool>();
    
    private void Start()
    {
        for (int i = 0; i < _coords.Length; i++)
        {
            _coordToIndex[_coords[i]] = i; 
        }
        
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                for (int n = 0; n < _dirs.Length; n++)
                {
                    Vector2Int coord = new Vector2Int(x + _dirs[n].x, y + _dirs[n].y);
                    if (coord.x >= 3 || coord.y >= 3) continue;
                    if (coord.x < 0 || coord.y < 0) continue;
                    Vector2Int connection = new Vector2Int(_coordToIndex[new Vector2Int(x, y)], _coordToIndex[coord]);
                    _plaqueVFXs[connection] = Instantiate(_plaqueVFXPrefab);
                    _allConnections.Add(connection);
                    _connectionOn.Add(connection, false);
                }
            }
        }

        foreach (GameObject door in _doors)
        {
            _doorInitialRot.Add(door.transform.rotation);
        }
    }
    
    private void Update()
    {
        if (_puzzleComplete)
        {
            if (!_doorOpen)
            {
                _doorOpenTimer += Time.deltaTime;
                float t = Mathf.Clamp01(Easing.InOut(_doorOpenTimer / _doorOpenTime));

                for (int i = 0; i < _doors.Count; i++)
                {
                    Quaternion target = _doorInitialRot[i] * Quaternion.Euler(0.0f, i == 0 ? -90.0f : 90.0f, 0.0f);
                    _doors[i].transform.rotation = Quaternion.Slerp(_doorInitialRot[i], target, t);
                }

                if (_doorOpenTimer >= _doorOpenTime)
                    _doorOpen = true;
            }
            return;
        }

        List<GrappleController.GrappleSection> sections = GrappleController.Instance.ActiveGrappleSections;

        int completed = 0;
        int broken = 0;
        for (int i = 0; i < _allConnections.Count; i++)
        {
            int sectionState = 0;
            SphereCollider p1 = _pegs[_allConnections[i].x];
            SphereCollider p2 = _pegs[_allConnections[i].y];
            foreach (GrappleController.GrappleSection section in sections)
            {
                if ((p1.bounds.Contains(section.Base) && p2.bounds.Contains(section.Tip))
                 || (p2.bounds.Contains(section.Base) && p1.bounds.Contains(section.Tip)))
                {
                    if (_requiredConnections.Contains(_allConnections[i]))
                    {
                        sectionState = 1;
                        completed++;
                        break;
                    }
                    else
                    {
                        sectionState = 2;
                        broken++;
                        break;
                    }
                }
            }

            if (sectionState != 0)
            {
                _connectionOn[_allConnections[i]] = true;
            }
            
            float intensity = 100.0f;
            if (sectionState == 1)
            {
                Color col = Color.green * intensity;
                _plaqueVFXs[_allConnections[i]].On(_plaquePegs[_allConnections[i].x].position, _plaquePegs[_allConnections[i].y].position, col);
            }
            else if (sectionState == 2)
            {
                Color col = Color.red * intensity;
                _plaqueVFXs[_allConnections[i]].On(_plaquePegs[_allConnections[i].x].position, _plaquePegs[_allConnections[i].y].position, col);
            }
            else if (_requiredConnections.Contains(_allConnections[i]))
            {
                Color col = new Color((float)191/255 * intensity, (float)90/255 * intensity, 0.0f, 1.0f);
                _plaqueVFXs[_allConnections[i]].On(_plaquePegs[_allConnections[i].x].position, _plaquePegs[_allConnections[i].y].position, col);
            }
            else if (_connectionOn[_allConnections[i]])
            {
                _plaqueVFXs[_allConnections[i]].Off();
            }
        }

        if (completed == _requiredConnections.Count && broken == 0)
        {
            _doorOpenSFX.Play();
            _puzzleComplete = true;
        }
    }
}
