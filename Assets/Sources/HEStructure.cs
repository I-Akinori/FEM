using System;
using System.Collections.Generic;
using UnityEngine;

public class HEStructure : MonoBehaviour
{
    private Transform _camTransform;
    private float timeOut = 0.1f;
    private int count = 0;
    private bool firstloop = true;
    public static System.Diagnostics.Stopwatch sw0 = new System.Diagnostics.Stopwatch();
    private System.Diagnostics.Stopwatch sw1 = new System.Diagnostics.Stopwatch();
    private System.Diagnostics.Stopwatch sw2 = new System.Diagnostics.Stopwatch();
    private System.Diagnostics.Stopwatch sw3 = new System.Diagnostics.Stopwatch();
    private System.Diagnostics.Stopwatch sw4 = new System.Diagnostics.Stopwatch();
    private System.Diagnostics.Stopwatch sw5 = new System.Diagnostics.Stopwatch();
    private System.Diagnostics.Stopwatch sw6 = new System.Diagnostics.Stopwatch();
    private System.Diagnostics.Stopwatch sw7 = new System.Diagnostics.Stopwatch();
    private float scale = 1f;

    private bool _simulating = true;
    private float _peelangle = Mathf.PI * 2;

    [SerializeField]
    private uint _seed = 1000;

    private Mesh _mesh;

    // (1) 頂点座標（この配列のインデックスが頂点インデックス）
    private List<Vertex> _vertices = new List<Vertex>();

    private Vector3[] _positions = new Vector3[]{                                                                    // position, normal, triangles, uvsは配列　ほかはリスト
    
    };

    // (2) ポリゴンを形成する頂点インデックスを順番に指定する
    private List<TPolygon> _tpolygons = new List<TPolygon>();
    private List<HalfEdge> _halfedges = new List<HalfEdge>();

    private int[] _triangles = new int[] { 0, 1, 2 };

    // (3) 法線
    private Vector3[] _normals = new Vector3[]{
        new Vector3(0, 0, -1),
        new Vector3(0, 0, -1),
        new Vector3(0, 0, -1)
    };

    // uv座標
    private Vector2[] _uvs = new Vector2[]{
        new Vector2(0, 0),
        new Vector2(0, 1),
        new Vector2(1, 0),
    };

    // uv座標
    private Color[] _colors = new Color[]{
        Color.white,
        Color.blue,
        Color.red,
    };

    private float Dist2BetweenPointAndLine(Vector3 P, Vector3 Q, Vector3 R)
    {
        Vector3 u = P - Q;
        Vector3 v = R - Q;
        float uu = Vector3.Dot(u, u);
        float vv = Vector3.Dot(v, v);
        float uv = Vector3.Dot(u, v);
        return (vv - uv * uv / uu);
    }

    public List<Vertex> VList
    {
        set { _vertices = value; }
        get { return _vertices; }
    }
    public List<TPolygon> TList
    {
        set { _tpolygons = value; }
        get { return _tpolygons; }
    }
    public List<HalfEdge> HEList
    {
        set { _halfedges = value; }
        get { return _halfedges; }
    }

    // ##############################################################################################################################################################
    // ##############################################################################################################################################################
    public void InitHES()
    {
        sw0.Start();
        _camTransform = GameObject.Find("Main Camera").transform;
        // 各リストの初期化

        _mesh = new Mesh();

        // Objファイル読込み start

        for (int i = 0; i < LoadText.Positions.Count; i++)
        {
            _vertices.Add(new Vertex(LoadText.Positions[i] * scale, LoadText.VNormals[i], LoadText.ArrangedUVs[i]));
            _vertices[i].Tex = new Vector2(_vertices[i].Pos.x, _vertices[i].Pos.y) * scale * 0.1f + new Vector2(0.5f, 0.5f); // objのテクスチャ座標が気に食わないとき用
        }
        for (int i = 0; i < LoadText.Polygons.Count; i++)
        {
            _halfedges.Add(new HalfEdge(_vertices[LoadText.Polygons[i].x]));
            _halfedges.Add(new HalfEdge(_vertices[LoadText.Polygons[i].y]));
            _halfedges.Add(new HalfEdge(_vertices[LoadText.Polygons[i].z]));

            _halfedges[_halfedges.Count - 3].Next = _halfedges[_halfedges.Count - 2];
            _halfedges[_halfedges.Count - 2].Prev = _halfedges[_halfedges.Count - 3];
            _halfedges[_halfedges.Count - 2].Next = _halfedges[_halfedges.Count - 1];
            _halfedges[_halfedges.Count - 1].Prev = _halfedges[_halfedges.Count - 2];
            _halfedges[_halfedges.Count - 1].Next = _halfedges[_halfedges.Count - 3];
            _halfedges[_halfedges.Count - 3].Prev = _halfedges[_halfedges.Count - 1];

            _tpolygons.Add(new TPolygon(_halfedges[_halfedges.Count - 3]));
            _halfedges[_halfedges.Count - 3].Face = _tpolygons[_tpolygons.Count - 1];
            _halfedges[_halfedges.Count - 2].Face = _tpolygons[_tpolygons.Count - 1];
            _halfedges[_halfedges.Count - 1].Face = _tpolygons[_tpolygons.Count - 1];
        }


        // Objファイル読込み end

        /*
        for (int i = _tpolygons.Count - 1; i >= 0; i--)
        {

            TPolygon TP = _tpolygons[i];

            if (TP.V1 < 3 || TP.V2 < 3 || TP.V3 < 3)        // 初期３点を含むポリゴンの削除 ドロネー分割のみ必要
            {
                // _tpolygons.RemoveAt(i);
            }
        }
        Debug.Log("After Count: " + _tpolygons.Count);
        */

        SetHalfEdge();
        SetPairHalfEdge();

        for (int i = 0; i < _halfedges.Count; i++)
        {
            HalfEdge HEi = _halfedges[i];

            if (HEi.Pair == null) // ペアがないので境界
            {
                HEi.Vert.Bound = 1;
                HEi.Vert.B_value = HEi.Vert.Pos.y * (- 0.2f); //Mathf.Sin(HEi.Vert.Pos.y);
                HEi.Vert.B_vector = new Vector3((HEi.Vert.Pos.x) * 1.2f, 0, 0);
            }
            else
            {
                bool convex = Vector3.Dot(HEi.Prev.Vert.Nor - HEi.Pair.Prev.Vert.Nor, HEi.Prev.Vert.Pos - HEi.Pair.Prev.Vert.Pos) > 0;
                Vector3 mid = (HEi.Vert.Pos + HEi.Next.Vert.Pos) / 2.0f;
                //convex = Vector3.Dot(_vertices[HEi.OppositeVertex].Pos + _vertices[_halfedges[HEi.Pair].OppositeVertex].Pos - 2.0f * mid, _vertices[HEi.End1].Nor + _vertices[HEi.End2].Nor) < 0;

                float rad = Vector3.Dot(HEi.Prev.Vert.Nor.normalized, HEi.Pair.Prev.Vert.Nor.normalized);
                float d = Mathf.Clamp(rad, -1.0f, 1.0f);
                HEi.Curvature = (convex ? Mathf.PI + Mathf.Acos(d) : Mathf.PI - Mathf.Acos(d)) - _peelangle;
                HEi.LiftingForce = 0.12f * HEi.Face.ContractionForce * Dist2BetweenPointAndLine(HEi.Prev.Vert.Pos, HEi.Vert.Pos, HEi.Next.Vert.Pos)
                //    * (convex ? 10 : 0);
                * HEi.Curvature;

            }
        }

        /*
        for (int i = 0; i < VList.Count; i++)
        {
            if (VList[i].Pos.x < -1.9) {
                VList[i].Bound = 1;     // 平曲面用のテスト
                VList[i].B_value = -1;   //
            }
            if (VList[i].Pos.x > 1.2)
            {
                VList[i].Bound = 1;     // 平曲面用のテスト
                VList[i].B_value = 1;   //
            }

        }*/
        

        foreach (Vertex V in _vertices)
            V.ReloadNearbyVertices();
        for (int i = 0; i < _tpolygons.Count; i++)
            _tpolygons[i].ReloadNormal();

        sw0.Stop();
    }

    private void SetHalfEdge() // 頂点ごとの半稜線のセッティング
    {
        Debug.Log("Set Vertex's HalfEdge.");
        for (int i = 0; i < _vertices.Count; i++)
        {
            for (int j = 0; j < _halfedges.Count; j++)
            {
                if (_vertices[i] == _halfedges[j].Vert)
                {
                    _vertices[i].HE = _halfedges[j];
                    break;
                }
            }
            if (_vertices[i].HE == null) Debug.Log("Error2: " + i);
        }
    }
    private void SetPairHalfEdge() // 半稜線のペアセッティング
    {
        Debug.Log("Set Pair.");
        for (int i = 0; i < _halfedges.Count; i++)
        {
            if (_halfedges[i].Pair != null) continue;
            for (int j = 0; j < _halfedges.Count; j++)
            {
                if (j == i) continue;
                if (_halfedges[i].Next.Vert == _halfedges[j].Vert && _halfedges[j].Next.Vert == _halfedges[i].Vert)
                {
                    _halfedges[i].Pair = _halfedges[j];
                    _halfedges[j].Pair = _halfedges[i];
                    break;
                }
            }
        }
    }
    private void ReloadVertexIndex()
    {
        for (int i = 0; i < _vertices.Count; i++)
        {
            _vertices[i].Idx = i;
        }
    }
    private void ReloadHalfEdgeIndex()
    {
        for (int i = 0; i < _halfedges.Count; i++)
        {
            _halfedges[i].Idx = i;
        }
    }
    private void ReloadFaceIndex()
    {
        for (int i = 0; i < _tpolygons.Count; i++)
        {
            _tpolygons[i].Idx = i;
        }
    }
    // ##############################################################################################################################################################
    // ##############################################################################################################################################################
    private void Update()
    {/*
        sw1.Start();

            Array.Resize(ref _triangles, _tpolygons.Count * 3);

            for (int i = 0; i < _tpolygons.Count; i++)      // ポリゴンの最終処理：ポリゴン座標をメッシュに代入
            {
                
                 _triangles[i * 3] = _tpolygons[i].HE.Vert.Idx;
                 _triangles[i * 3 + 1] = _tpolygons[i].HE.Next.Vert.Idx;
                 _triangles[i * 3 + 2] = _tpolygons[i].HE.Prev.Vert.Idx;
                
            }

            Array.Resize(ref _positions, _vertices.Count);
            Array.Resize(ref _normals, _vertices.Count);
            Array.Resize(ref _uvs, _vertices.Count);
            Array.Resize(ref _colors, _vertices.Count);

            // 位置計算
            for (int i = 0; i < _vertices.Count && _simulating; i++)
                _positions[i] = _vertices[i].Pos;

            // 法線計算
            int[] Nface = new int[_vertices.Count];
            Vector3[] NormalSum = new Vector3[_vertices.Count];
            for (int i = 0; i < _vertices.Count; i++) Nface[i] = 0;
            for (int i = 0; i < _tpolygons.Count; i++)
            {
                TPolygon TP = _tpolygons[i];
                NormalSum[TP.HE.Vert.Idx] += TP.Normal; Nface[TP.HE.Vert.Idx]++;
                NormalSum[TP.HE.Next.Vert.Idx] += TP.Normal; Nface[TP.HE.Next.Vert.Idx]++;
                NormalSum[TP.HE.Prev.Vert.Idx] += TP.Normal; Nface[TP.HE.Prev.Vert.Idx]++;
            }
            for (int i = 0; i < _vertices.Count; i++)
            {
                // _vertices[i].Nor = NormalSum[i] / Nface[i];
                _normals[i] = _vertices[i].Nor;

            }

            // 収縮力計算 （デバッグ用）
            // Debug.Log("Contract: " + _tpolygons[100].ContractionForce);
            int[] Cface = new int[_vertices.Count];
            float[] ContractSum = new float[_vertices.Count];
            for (int i = 0; i < _vertices.Count; i++) Cface[i] = 0;
            for (int i = 0; i < 0 * _tpolygons.Count; i++)
            {
                TPolygon TP = _tpolygons[i];
                ContractSum[TP.HE.Vert.Idx] += TP.ContractionForce; Cface[TP.HE.Vert.Idx]++;
                ContractSum[TP.HE.Next.Vert.Idx] += TP.ContractionForce; Cface[TP.HE.Next.Vert.Idx]++;
                ContractSum[TP.HE.Prev.Vert.Idx] += TP.ContractionForce; Cface[TP.HE.Prev.Vert.Idx]++;
            }
            for (int i = 0; i < _vertices.Count; i++)
            {
                //_vertices[i].Rust = ContractSum[i] / Cface[i];

            }

            Array.Resize(ref _uvs, _positions.Length);

            for (int i = 0; i < _positions.Length; i++)
                    _uvs[i] = _vertices[i].Tex;

            sw7.Stop();

            sw6.Start();
            for (int i = 0; i < _vertices.Count; i++)
            {
                // 錆
                _colors[i] = new Color(132.0f / 255.0f, 51.0f / 255.0f, 0.0f, 1.0f) + new Color(123.0f / 255.0f, 204.0f / 255.0f, 1.0f, 0.0f) * Mathf.Exp(-(_vertices[i].Rust > 10.0f ? _vertices[i].Rust - 10.0f : 0.0f) / 100.0f);
                // 塵
                _colors[i] = _colors[i] * (0.0f + 1.0f * Mathf.Exp(-(_vertices[i].Dust) / 100.0f));
                
                // _colors[i] = _vertices[i].Col;
                //_colors[i] = Color.white;
            }
            sw6.Stop();

            count++;

        sw1.Stop();

        if (firstloop)
        {
            Debug.Log("Start: " + sw0.Elapsed);
            Debug.Log("Total: " + sw1.Elapsed + " / " + (100 * sw1.Elapsed.TotalMilliseconds / sw1.Elapsed.TotalMilliseconds) + " %");
            Debug.Log("Dust: " + sw2.Elapsed + " / " + (100 * sw2.Elapsed.TotalMilliseconds / sw1.Elapsed.TotalMilliseconds) + " %");
            Debug.Log("Separate: " + sw3.Elapsed + " / " + (100 * sw3.Elapsed.TotalMilliseconds / sw1.Elapsed.TotalMilliseconds) + " %");
            Debug.Log("Rust: " + sw4.Elapsed + " / " + (100 * sw4.Elapsed.TotalMilliseconds / sw1.Elapsed.TotalMilliseconds) + " %");
            Debug.Log("Deform: " + sw5.Elapsed + " / " + (100 * sw5.Elapsed.TotalMilliseconds / sw1.Elapsed.TotalMilliseconds) + " %");
            Debug.Log("Color: " + sw6.Elapsed + " / " + (100 * sw6.Elapsed.TotalMilliseconds / sw1.Elapsed.TotalMilliseconds) + " %");
            Debug.Log("Mesh: " + sw7.Elapsed + " / " + (100 * sw7.Elapsed.TotalMilliseconds / sw1.Elapsed.TotalMilliseconds) + " %");
            Debug.Log("Other: " + (100f - 100 * (sw2.Elapsed.TotalMilliseconds + sw3.Elapsed.TotalMilliseconds + sw4.Elapsed.TotalMilliseconds +
                                                    sw5.Elapsed.TotalMilliseconds + sw6.Elapsed.TotalMilliseconds + sw7.Elapsed.TotalMilliseconds) / sw1.Elapsed.TotalMilliseconds) + " %");
            Debug.Log("Vertices: " + _vertices.Count);
            firstloop = false;
        }



        // (4) Meshに頂点情報を代入
        _mesh.vertices = _positions;
        _mesh.triangles = _triangles;
        _mesh.normals = _normals;
        _mesh.uv = _uvs;
        _mesh.colors = _colors;

        _mesh.RecalculateBounds();*/


        // (5) 描画

        //Graphics.DrawMesh(_mesh, Vector3.zero, Quaternion.identity, _material, 0);
    }
}