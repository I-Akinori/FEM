using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FEM3 : MonoBehaviour
{
    //　頂点配列
    public Vector3[] vertices;
    public Vector3[] vertices0;
    public Vector3[] normals;
    //　UV配列
    public Vector2[] uvs;
    public Vector2[] uvs2;
    //　三角形の順番配列
    public int[] triangles;
    private Mesh mesh;
    //　メッシュ表示コンポーネント
    private MeshRenderer meshRenderer;
    //　メッシュに設定するマテリアル
    public Material material;
    public Material submat;
    private bool vis;
    private HEStructure HES = new HEStructure();

    public List<int> S1;
    public List<float> g1;
    public List<int> S2;
    public List<float> g2;

    private int size = 16;
    private int size2 = 24;
    private float ratio = 1f;
    private float scale = 1f;
    public float[,] Array;
    public float[,] R_Array;
    public float[] F2vector;            // g2の積分項
    public float[] R_F2vector;
    public float[] Fvector;
    public float[] R_Fvector;
    public float[] values;
    public int[] R_index;

    // Start is called before the first frame update
    void Start()
    {
        vis = false;
        HES.InitHES();
        Debug.Log(HES.VList.Count);
        InitMesh();
        InitArray();
        SetArray();
        Reduction();

        for (int i = 0; i < R_index.Length; i++) // 係数行列の初期化
        {
            for (int j = 0; j < R_index.Length; j++)
            {
                if (R_Array[i, j] != R_Array[j, i]) Debug.Log("R_Array is not symetry:" + R_Array[i, j] + " & " + R_Array[j, i]);
            }
        }

        SetVector();
        Solve();
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.V)) Visualization();
        UpdataBoundary();
        SetVector();
        Solve();
        CreateMesh(mesh, vertices, vis ? uvs2 : uvs, triangles);
    }

    void InitMesh()
    {
        vertices = new Vector3[HES.VList.Count];
        vertices0 = new Vector3[HES.VList.Count];
        normals = new Vector3[HES.VList.Count];
        uvs = new Vector2[HES.VList.Count];
        uvs2 = new Vector2[HES.VList.Count];
        triangles = new int[HES.TList.Count * 3];

        for (int i = 0; i < HES.VList.Count; i++) 
        {
            vertices[i] = HES.VList[i].Pos;
            vertices0[i] = HES.VList[i].Pos;
            normals[i] = HES.VList[i].Nor;
            uvs[i] = HES.VList[i].Tex;
            uvs2[i] = new Vector2(0.5f, 0.5f);
        }

        for (int i = 0; i < HES.TList.Count; i++) 
        {
            triangles[3 * i] = HES.TList[i].HE.Vert.Idx;
            triangles[3 * i + 1] = HES.TList[i].HE.Next.Vert.Idx;
            triangles[3 * i + 2] = HES.TList[i].HE.Prev.Vert.Idx;
        }

        gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        mesh = GetComponent<MeshFilter>().mesh;
        meshRenderer.material = material;
    }

    void InitArray()
    {
        Array = new float[HES.VList.Count, HES.VList.Count];
        Fvector = new float[HES.VList.Count];
        F2vector = new float[HES.VList.Count];

        for (int i = 0; i < HES.VList.Count; i++) 
        {
            Fvector[i] = 0;
            F2vector[i] = 0;
            for (int j = 0; j < HES.VList.Count; j++)
            {
                Array[i, j] = 0;

            }
        }

        int count = HES.VList.Count;

        for (int i = 0; i < HES.VList.Count; i++) // 頂点：Arrayの行または列に対応する
        {
            if (HES.VList[i].Bound == 1) // S1境界の設定
            {
                S1.Add(i);
                g1.Add(HES.VList[i].B_value);
                count--;
            }
            else if (HES.VList[i].Bound == 2)
            {
                S2.Add(i);
                g2.Add(HES.VList[i].B_value);
                //g2.Add(j == 0 ? 0.5f : -0.5f);
            }
        }
        R_Array = new float[count, count];
        R_Fvector = new float[count];
        R_F2vector = new float[count];
        R_index = new int[count];
        values = new float[count];
    }
    void Reduction()
    {

        int x = 0;
        int y;

        for (int i = 0; i < HES.VList.Count; i++)
        {
            if (S1.Contains(i)) continue;

            y = 0;
            for (int j = 0; j < HES.VList.Count; j++)
            {
                if (S1.Contains(j)) continue;

                R_Array[x, y] = Array[i, j];        // x, y はS1境界のインデックスを飛ばしてナンバリングされたもの  (このループ上で空回りしているインデックス)         
                y++;
            }

            R_Fvector[x] = Fvector[i];
            R_F2vector[x] = F2vector[i];
            values[x] = 0f;
            R_index[x] = i;                         // x, y の番号の飛び方は一緒なので片方だけ記録すればOK
            x++;
        }
    }
    void SetArray()
    {
        for (int i = 0; i < HES.TList.Count; i++) // Element
        {
            int i1 = HES.TList[i].HE.Vert.Idx;
            int i2 = HES.TList[i].HE.Next.Vert.Idx;
            int i3 = HES.TList[i].HE.Prev.Vert.Idx;

            //float v1 = values[i1];
            //float v2 = values[i2];
            //float v3 = values[i3];

            Vector3 q1 = HES.VList[i1].Pos;
            Vector3 q2 = HES.VList[i2].Pos;
            Vector3 q3 = HES.VList[i3].Pos;

            q2 -= q1;
            q3 -= q1;
            q1 *= 0;

            float t = Vector3.Dot(q2, q3.normalized);

            Vector2 p1 = new Vector2(0, 0);
            Vector2 p3 = new Vector2(t, (q2 - t * q3.normalized).magnitude);
            Vector2 p2 = new Vector2(q3.magnitude, 0);

            float area = p3.y * p2.x * 0.5f;

            float a1 = 0.5f * (p2.x * p3.y - p3.x * p2.y) / area;
            float a2 = 0.5f * (p3.x * p1.y - p1.x * p3.y) / area;
            float a3 = 0.5f * (p1.x * p2.y - p2.x * p1.y) / area;

            /*
            Vector2 p1 = new Vector2(0, 0);
            Vector2 p2 = new Vector2(t, (q2 - t * q3.normalized).magnitude);
            Vector2 p3 = new Vector2(q3.magnitude, 0);

            float area = p2.y * p3.x * 0.5f;

            float a1 = -0.5f * (p2.x * p3.y - p3.x * p2.y) / area;
            float a2 = -0.5f * (p3.x * p1.y - p1.x * p3.y) / area;
            float a3 = -0.5f * (p1.x * p2.y - p2.x * p1.y) / area;
            
             */


            float b1 = 0.5f * (p3.y - p2.y) / area;
            float b2 = 0.5f * (p1.y - p3.y) / area;
            float b3 = 0.5f * (p2.y - p1.y) / area;

            float c1 = 0.5f * (p2.x - p3.x) / area;
            float c2 = 0.5f * (p3.x - p1.x) / area;
            float c3 = 0.5f * (p1.x - p2.x) / area;
            

            Array[i1, i1] += (b1 * b1 + c1 * c1) * area;        // a,b,c の2, 3はi の3, 2にそれぞれ対応する！！！（左右反転を戻す）
            Array[i3, i3] += (b2 * b2 + c2 * c2) * area;
            Array[i2, i2] += (b3 * b3 + c3 * c3) * area;
            Array[i1, i3] += (b1 * b2 + c1 * c2) * area;
            Array[i3, i1] += (b1 * b2 + c1 * c2) * area;
            Array[i3, i2] += (b2 * b3 + c2 * c3) * area;
            Array[i2, i3] += (b2 * b3 + c2 * c3) * area;
            Array[i2, i1] += (b3 * b1 + c3 * c1) * area;
            Array[i1, i2] += (b3 * b1 + c3 * c1) * area;

            /*
            Debug.Log("11: " + (b1 * b1 + c1 * c1) * area);
            Debug.Log("22: " + (b2 * b2 + c2 * c2) * area);
            Debug.Log("33: " + (b3 * b3 + c3 * c3) * area);
            Debug.Log("33: " + (b3 * b3 + c3 * c3) * area);
            Debug.Log("12: " + (b1 * b2 + c1 * c2) * area);
            Debug.Log("21: " + (b1 * b2 + c1 * c2) * area);
            Debug.Log("23: " + (b2 * b3 + c2 * c3) * area);
            Debug.Log("32: " + (b2 * b3 + c2 * c3) * area);
            Debug.Log("31: " + (b3 * b1 + c3 * c1) * area);
            Debug.Log("13: " + (b3 * b1 + c3 * c1) * area);
            */

            // 第二境界条件の処理，本来は角の要素について，二頂点がS2上にあってもそれを結ぶ辺が境界にならないがことがあるが，今回はないので無視する
            int x1 = S2.FindIndex(n => n == i1);
            int x2 = S2.FindIndex(n => n == i2);
            int x3 = S2.FindIndex(n => n == i3);

            if (x1 >= 0 && x2 >= 0)             // a,b,c の2, 3はi, x の3, 2にそれぞれ対応する！！！
            {
                float L = (p1 - p2).magnitude;
                F2vector[i1] += g2[x1] * L * (a1 + b1 * (p1.x + p2.x) / 2 + c1 * (p1.y + p2.y) / 2);
                F2vector[i2] += g2[x2] * L * (a3 + b3 * (p1.x + p2.x) / 2 + c3 * (p1.y + p2.y) / 2);
            }
            if (x2 >= 0 && x3 >= 0)
            {
                float L = (p2 - p3).magnitude;
                F2vector[i2] += g2[x2] * L * (a3 + b3 * (p2.x + p3.x) / 2 + c3 * (p2.y + p3.y) / 2);
                F2vector[i3] += g2[x3] * L * (a2 + b2 * (p2.x + p3.x) / 2 + c2 * (p2.y + p3.y) / 2);
            }
            if (x3 >= 0 && x1 >= 0)
            {
                float L = (p3 - p1).magnitude;
                F2vector[i3] -= g2[x3] * L * (a2 + b2 * (p3.x + p1.x) / 2 + c2 * (p3.y + p1.y) / 2);
                F2vector[i1] += g2[x1] * L * (a1 + b1 * (p3.x + p1.x) / 2 + c1 * (p3.y + p1.y) / 2);
            }
        }
    }

    void SetVector()
    {
        for (int i = 0; i < R_Fvector.Length; i++)
        {
            R_Fvector[i] = R_F2vector[i];

            for (int j = 0; j < S1.Count; j++)
            {
                R_Fvector[i] -= Array[R_index[i], S1[j]] * g1[j];

            }
        }
    }

    void UpdataBoundary()
    {
        for (int i = 0; i < g1.Count; i++)
        {
            Vector3 V = HES.VList[S1[i]].Pos;
            //g1[i] = y == 0 ? Mathf.Sin(0.1f + Time.time) * ratio * 0.4f + Mathf.Sin((x + y) * 0.5f + Time.time) * ratio * 0.6f : -2f;
            g1[i] = Mathf.Sin(0.1f + Time.time + V.x + V.y) * ratio * 1f;
            //g1[i] = V.x > 0 ? Mathf.Sin(0.1f + Time.time) : -Mathf.Sin(0.1f + Time.time);// Mathf.Sin(0.1f + Time.time) * ratio * 0.1f;
        }
    }
    void Solve()
    {
        //Solver.PrintArray(R_Array);
        //Solver.PrintVector(R_Fvector);
        //Solver.PrintVector(values);
        Solver.ICCG(R_Array, R_Fvector, values);

        //Solver.PrintVector(R_Fvector);
        //Solver.PrintVector(values);

        float delta = 0.1f;
        for (int i = 0; i < values.Length; i++)
        {
            vertices[R_index[i]] = vertices0[R_index[i]] + normals[R_index[i]] * values[i] * delta;
            uvs2[R_index[i]].x = Mathf.Clamp(values[i] * 0.4f + 0.7f, 0.01f, 1f - 0.01f);
            //uvs2[R_index[i]].x = Mathf.Sin(Time.time) * 0.5f * 0.5f;
        }
        for (int i = 0; i < S1.Count; i++)
        {
            vertices[S1[i]] = vertices0[S1[i]] + normals[S1[i]] * g1[i] * delta;
            uvs2[S1[i]].x = Mathf.Clamp(g1[i] * 0.4f + 0.7f, 0.01f, 1f - 0.01f);
        }
    }
    void Visualization()
    {
        vis = !vis;
        if (vis)
        {
            meshRenderer.material = submat;
        }
        else
        {
            meshRenderer.material = material;
        }

    }
    void CreateMesh(Mesh mesh, Vector3[] vertices, Vector2[] uvs, int[] triangles)
    {
        //　最初にメッシュをクリアする
        mesh.Clear();
        //　頂点の設定
        mesh.vertices = vertices;
        //　テクスチャのUV座標設定
        mesh.uv = uvs;
        //　三角形メッシュの設定
        mesh.triangles = triangles;
        //　Boundsの再計算
        mesh.RecalculateBounds();
        //　NormalMapの再計算
        mesh.RecalculateNormals();
    }
}

