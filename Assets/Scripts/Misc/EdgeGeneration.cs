﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EdgeGeneration : MonoBehaviour {
    public GameObject front;

    private MeshFilter _filter;
    private MeshRenderer _renderer;
    private Dictionary<Vector3, float> _angleDict;
    private List<Vector3> _candidates;
    private Dictionary<Vector3, Vector3> _normalDict;
    private Dictionary<Vector3, List<Vector3>> _neighbors;
    private Dictionary<Vector3, Edge> _edgeDict;
    private Vector3 _spawnNode;

    private List<Vector3> _edgeList;

    /**
     * Initialization
     */
    void Start () {
        _candidates = new List<Vector3>();
        _normalDict = new Dictionary<Vector3, Vector3>();
        _angleDict = new Dictionary<Vector3, float>();
        _neighbors = new Dictionary<Vector3, List<Vector3>>();
        _filter = GetComponent<MeshFilter>();
        _renderer = GetComponent<MeshRenderer>();
        _edgeDict = new Dictionary<Vector3, Edge>();
        if (_filter == null || _renderer == null) {
            Debug.LogError("No mesh renderer or filter on edge generation object!");
            return;
        }
        initGeneration();
    }

    /**
     * Go through all of the triangles and verticies for
     * a mesh level and generate the correct edges
     */
    private void initGeneration() {
        int[] triangles = _filter.mesh.triangles;
        Vector3[] verts = _filter.mesh.vertices;
        Debug.Log("Verts:" + verts.Length);

        for(int i = 0; i < triangles.Length;) {
            getAngleForVert(verts[triangles[i]], verts[triangles[i + 1]], verts[triangles[i + 2]]);
            getAngleForVert(verts[triangles[i + 1]], verts[triangles[i]], verts[triangles[i + 2]]);
            getAngleForVert(verts[triangles[i + 2]], verts[triangles[i]], verts[triangles[i + 1]]);
            i += 3;
        }

        foreach (KeyValuePair<Vector3, float> entry in _angleDict) {
            if (entry.Value < 358) {
                Vector3 v = transform.TransformPoint(entry.Key);
                bool keep = true;
                foreach (Vector3 prevCandidate in _candidates) {
                    float dist = Vector3.Distance(prevCandidate, v);
                    if (dist < 0.5f) {
                        keep = false;
                        break;
                    }
                }
                if (keep) {
                    _candidates.Add(v);
                }
            }
        }

        _edgeList = new List<Vector3>();
        foreach (Vector3 v1 in _candidates) {
            Vector2 c1 = new Vector2(v1.x, v1.z);
            float closestDist = float.MaxValue;
            foreach (Vector3 v2 in _candidates) {
                Vector2 c2 = new Vector2(v2.x, v2.z);
                if (v1 == v2) {
                    continue;
                }
                
                float dist = Vector3.Distance(v1, v2);
                if (dist > renderer.bounds.size.z && dist > renderer.bounds.size.y && dist < closestDist && !_edgeList.Contains(v1) && !_edgeList.Contains(v2)) {
                    closestDist = dist;
                    _edgeList.Add(v1);
                    _edgeList.Add(v2);
                }

                if (dist < 1.1f && dist > 0.9f) {
                    if (!_neighbors.ContainsKey(v1)) {
                        _neighbors[v1] = new List<Vector3>();
                    }
                    _neighbors[v1].Add(v2);
                }
            }
        }

        Vector3 xDir = new Vector3(1, 0, 0);
        foreach (KeyValuePair<Vector3, List<Vector3>> entry in _neighbors) {
            Vector3 norm;
            if (entry.Key.z < entry.Value[0].z) {
                norm = Vector3.Cross(entry.Value[0] - entry.Key, xDir);
            } else {
                norm = Vector3.Cross(entry.Key - entry.Value[0], xDir);
            }
            _normalDict[entry.Key] = norm;

            Debug.Log(norm);
        }

        separateEdges();
        getSpawnNode();
    }

    /**
     * Goes through all of the implicit edges and converts them to actual
     * Edges for later use. Saves the edges into the level. Gets all neighbors
     * for each edge.
     */
    private void separateEdges() {
        List<Edge> newEdges = new List<Edge>();
        Vector3 frontPos = front.transform.position;
        for (int i = 0; i < _edgeList.Count;) {
            Edge e;
            if (Vector3.Distance(_edgeList[i], frontPos) < Vector3.Distance(_edgeList[i + 1], frontPos)) {
                e = new Edge(_edgeList[i], _edgeList[i + 1], _normalDict[_edgeList[i]]);
            } else {
                e = new Edge(_edgeList[i + 1], _edgeList[i], _normalDict[_edgeList[i + 1]]);
            }
            newEdges.Add(e);
            _edgeDict[e.Front] = e;

            i += 2;
        }

        foreach (KeyValuePair<Vector3, List<Vector3>> entry in _neighbors) {
            if (!_edgeDict.ContainsKey(entry.Key)) {
                continue;
            }
            Edge e = _edgeDict[entry.Key];
            foreach (Vector3 n in entry.Value) {
                e.addNeighbor(_edgeDict[n]);
            }
        }

        Level l = GetComponent<Level>();
        l.edges = newEdges;
    }

    /**
     * Add to a running total to check the angle, this is used to determine where an edge is
     */
    private void getAngleForVert(Vector3 targetVert, Vector3 o1, Vector3 o2) {
        Vector3 d1 = o1 - targetVert;
        Vector3 d2 = o2 - targetVert;
        if (!_angleDict.ContainsKey(targetVert)) {
            _angleDict[targetVert] = 0f;
        }
        _angleDict[targetVert] += Vector3.Angle(d1, d2);
    }

    /**
     * Quick way to get the spawn location for the ship
     * 
     */
    private void getSpawnNode() {
        float lowest = float.MaxValue;
        float closest = float.MaxValue;
        foreach (Vector3 v in _edgeList) {
            if (v.y < lowest) {
                _spawnNode = v;
                lowest = v.y;
                closest = Vector3.Distance(v, front.transform.position);
            }
            if (v.y == lowest && closest > Vector3.Distance(v,front.transform.position)) {
                _spawnNode = v;
                lowest = v.y;
                closest = Vector3.Distance(v, front.transform.position);
            }
        }
    }
	
    void Update () {
	
    }

    void OnDrawGizmos() {
        if (_candidates == null) { return; }
        foreach (Vector3 v in _candidates) {
            Gizmos.DrawSphere(v, 0.05f);
            Gizmos.color = Color.red;
            foreach (Vector3 n in _neighbors[v]) {
                Gizmos.DrawLine(n, v);
            }
        }

        bool flip = false;
        for (int i = 0; i < _edgeList.Count; ) {
            if (flip) {
                Gizmos.color = Color.green;
            } else {
                Gizmos.color = Color.blue;
            }
            flip = !flip;
            Gizmos.DrawSphere(_edgeList[i], 0.1f);

            Gizmos.DrawLine(_edgeList[i], _edgeList[i] + _normalDict[_edgeList[i]]);

            Gizmos.DrawSphere(_edgeList[i + 1], 0.1f);

            Gizmos.DrawLine(_edgeList[i+1], _edgeList[i+1] + _normalDict[_edgeList[i+1]]);

            Gizmos.DrawLine(_edgeList[i], _edgeList[i + 1]);
            i += 2;

        }

        Gizmos.color = Color.gray;
        Gizmos.DrawSphere(_spawnNode, 0.5f);
    }
}
