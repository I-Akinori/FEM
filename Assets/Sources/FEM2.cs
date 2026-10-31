using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FEM2 : MonoBehaviour
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
        InitMesh();
        InitArray();
        SetArray();
        Reduction();

        for (int i = 0; i < R_index.Length; i++) // 係数行列の初期化
        {
            for (int j = 0; j < R_index.Length; j++)
            {
                if (R_Array[i, j] != R_Array[j, i]) Debug.Log("R_Array is not symetry.");
            }
        }
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
        vertices = new Vector3[size * size2];
        vertices0 = new Vector3[size * size2];
        normals = new Vector3[size * size2];
        uvs = new Vector2[size * size2];
        uvs2 = new Vector2[size * size2];
        triangles = new int[(size - 1) * size2 * 6]; // attention! 
        float r = 3.0f;
        float del_theta = Mathf.PI * 2f / size2;

        for (int i = 0; i < size2; i++)
        {
            for (int j = 0; j < size; j++)
            {

                //vertices0[i * size + j] = new Vector3(j * 10f / size - 5f, 0, i * 10f / size - 5f);
                float rand1 = UnityEngine.Random.value * 0.3f;
                float rand2 = UnityEngine.Random.value * 0.31f;
                vertices0[i * size + j] = new Vector3((j + rand1) * 10f / size - 5f, - Mathf.Sin((i + rand2) * del_theta) * r, Mathf.Cos((i + rand2) * del_theta) * r);
                vertices[i * size + j] = new Vector3(j * 10f / size - 5f, -Mathf.Sin(i * del_theta) * r, Mathf.Cos(i * del_theta) * r);
                normals[i * size + j] = new Vector3(0, -Mathf.Sin(i * del_theta), Mathf.Cos(i * del_theta));

                uvs[i * size + j] = new Vector2((j + rand1) / size, (i + rand2) / size2);
                uvs2[i * size + j] = new Vector2(0.5f, 0.5f);

                if (i < size2 - 1 && j < size - 1)
                {
                    triangles[(i * (size - 1) + j) * 6] = i * size + j;
                    triangles[(i * (size - 1) + j) * 6 + 1] = (i + 1) * size + (j + 1);
                    triangles[(i * (size - 1) + j) * 6 + 2] = i * size + (j + 1);
                    triangles[(i * (size - 1) + j) * 6 + 3] = i * size + j;
                    triangles[(i * (size - 1) + j) * 6 + 4] = (i + 1) * size + j;
                    triangles[(i * (size - 1) + j) * 6 + 5] = (i + 1) * size + (j + 1);
                }
                if (i == size2 - 1 && j < size - 1)
                {
                    triangles[(i * (size - 1) + j) * 6] = i * size + j;
                    triangles[(i * (size - 1) + j) * 6 + 1] = (j + 1);
                    triangles[(i * (size - 1) + j) * 6 + 2] = i * size + (j + 1);
                    triangles[(i * (size - 1) + j) * 6 + 3] = i * size + j;
                    triangles[(i * (size - 1) + j) * 6 + 4] = j;
                    triangles[(i * (size - 1) + j) * 6 + 5] = (j + 1);
                }
            }
        }

        gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        mesh = GetComponent<MeshFilter>().mesh;
        meshRenderer.material = material;
    }

    void InitArray()
    {
        Array = new float[size * size2, size * size2];
        Fvector = new float[size * size2];
        F2vector = new float[size * size2];

        for (int i = 0; i < size * size2; i++) // 係数行列の初期化
        {
            Fvector[i] = 0;
            F2vector[i] = 0;
            for (int j = 0; j < size * size2; j++)
            {
                Array[i, j] = 0;

            }
        }

        int count = size * size2;
        for (int i = 0; i < size2; i++) // 頂点：Arrayの行または列に対応する
        {
            for (int j = 0; j < size; j++)
            {
                if (j == 0 || j == size - 1) // S1境界の設定
                {
                    S1.Add(i * size + j);
                    //g1.Add(Mathf.Sin((i + j) * 0.5f) * ratio);
                    g1.Add(j == 0 ? 1.0f : -1.0f);
                    count--;
                }
                else if (j == size - 1)
                {
                    S2.Add(i * size + j);
                    g2.Add(0);
                    //g2.Add(j == 0 ? 0.5f : -0.5f);
                }
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

        for (int i = 0; i < size * size2; i++)
        {
            if (S1.Contains(i)) continue;

            y = 0;
            for (int j = 0; j < size * size2; j++)
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
        for (int i = 0; i < (size - 1) * (size2) * 2; i++) // Element
        {
            int i1 = triangles[i * 3];
            int i2 = triangles[i * 3 + 1];
            int i3 = triangles[i * 3 + 2];

            //float v1 = values[i1];
            //float v2 = values[i2];
            //float v3 = values[i3];

            Vector3 q1 = vertices0[i1];
            Vector3 q2 = vertices0[i2];
            Vector3 q3 = vertices0[i3];

            Vector2 p1 = new Vector2(q1.x, q1.z);
            Vector2 p2 = new Vector2(q2.x, q2.z);
            Vector2 p3 = new Vector2(q3.x, q3.z);

            q2 -= q1;
            q3 -= q1;
            q1 *= 0;
            p1 *= 0;

            float t = Vector3.Dot(q2, q3.normalized);
            p3 = new Vector2(t, (q2 - t * q3.normalized).magnitude);
            p2 = new Vector2(q3.magnitude, 0);
            //float area = -0.5f * ((p2.x - p1.x) * (p3.y - p1.y) - (p2.y - p1.y) * (p3.x - p1.x));
            float area = p3.y * p2.x * 0.5f;
//            Debug.Log("area: " + area);

            float a1 = 0.5f * (p2.x * p3.y - p3.x * p2.y) / area;
            float a2 = 0.5f * (p3.x * p1.y - p1.x * p3.y) / area;
            float a3 = 0.5f * (p1.x * p2.y - p2.x * p1.y) / area;

            float b1 = 0.5f * (p3.y - p2.y) / area;
            float b2 = 0.5f * (p1.y - p3.y) / area;
            float b3 = 0.5f * (p2.y - p1.y) / area;

            float c1 = 0.5f * (p2.x - p3.x) / area;
            float c2 = 0.5f * (p3.x - p1.x) / area;
            float c3 = 0.5f * (p1.x - p2.x) / area;
            ///*/

            Array[i1, i1] += (b1 * b1 + c1 * c1) * area;
            Array[i2, i2] += (b2 * b2 + c2 * c2) * area;
            Array[i3, i3] += (b3 * b3 + c3 * c3) * area;
            Array[i1, i2] += (b1 * b2 + c1 * c2) * area;
            Array[i2, i1] += (b1 * b2 + c1 * c2) * area;
            Array[i2, i3] += (b2 * b3 + c2 * c3) * area;
            Array[i3, i2] += (b2 * b3 + c2 * c3) * area;
            Array[i3, i1] += (b3 * b1 + c3 * c1) * area;
            Array[i1, i3] += (b3 * b1 + c3 * c1) * area;

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

            if (x1 >= 0 && x2 >= 0)
            {
                float L = (p1 - p2).magnitude;
                F2vector[i1] += g2[x1] * L * (a1 + b1 * (p1.x + p2.x) / 2 + c1 * (p1.y + p2.y) / 2);
                F2vector[i2] += g2[x2] * L * (a2 + b2 * (p1.x + p2.x) / 2 + c2 * (p1.y + p2.y) / 2);
            }
            if (x2 >= 0 && x3 >= 0)
            {
                float L = (p2 - p3).magnitude;
                F2vector[i2] += g2[x2] * L * (a2 + b2 * (p2.x + p3.x) / 2 + c2 * (p2.y + p3.y) / 2);
                F2vector[i3] += g2[x3] * L * (a3 + b3 * (p2.x + p3.x) / 2 + c3 * (p2.y + p3.y) / 2);
            }
            if (x3 >= 0 && x1 >= 0)
            {
                float L = (p3 - p1).magnitude;
                F2vector[i3] -= g2[x3] * L * (a3 + b3 * (p3.x + p1.x) / 2 + c3 * (p3.y + p1.y) / 2);
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
            int x = S1[i] / size;
            int y = S1[i] % size;
            //g1[i] = Mathf.Sin((x + y) * 0.5f + Time.time) * ratio;
            //g1[i] = x == 0 ? Mathf.Sin((x + y) * 0.3f + Time.time) * ratio : Mathf.Sin((x + y) * 0.3f - Time.time) * ratio;
            //g1[i] = y == 0 ? 1.0f : -1.0f;
            g1[i] = y == 0 ? Mathf.Sin(0.1f + Time.time) * ratio * 0.4f + Mathf.Sin((x + y) * 0.5f + Time.time) * ratio * 0.6f : -2f ;
        }
    }
    void Solve()
    {
        Solver.ICCG(R_Array, R_Fvector, values);

        //Solver.PrintVector(R_Fvector);
        //Solver.PrintVector(values);

        for (int i = 0; i < values.Length; i++)
        {
            vertices[R_index[i]] = vertices0[R_index[i]] + normals[R_index[i]] * values[i];
            uvs2[R_index[i]].x = Mathf.Clamp(values[i] * 0.4f + 0.7f, 0.01f, 1f - 0.01f);
            //uvs2[R_index[i]].x = Mathf.Sin(Time.time) * 0.5f * 0.5f;
        }
        for (int i = 0; i < S1.Count; i++)
        {
            vertices[S1[i]] = vertices0[S1[i]] + normals[S1[i]] * g1[i];
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

