using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class tensorFEM : MonoBehaviour
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

    public List<int> S1;    // 第一境界条件
    public List<float> g1;  // 境界値
    public List<int> S2;    // 第二境界条件
    public List<float> g2;  // 境界値
    public List<int> TF_index;    // 表面力（要素数×辺の数×次元数）
    public List<float> TF_value;  //
    public List<int> BF_index;    // 物体力（要素数×次元数）
    public List<float> BF_value;  //
    
    public float[] BF;
    public float[] TF;
    private int size = 3;
    private int size2 = 3;
    private int dim = 3;
    public float vis_scale = 0.05f;
    private float ratio = 1f;
    private float scale = 1f;
    public float[,] Array;
    public float[,] R_Array;
    public float[] Fvector;          // 全体荷重
    public float[] R_Fvector;        //
    public float[] values;           // (頂点数 - 境界)×次元数
    public float[] Displacement;           // 頂点数×次元数
    public int[] R_index;
    private GameObject sphereObj;
    private List<GameObject> spheres;
    private GameObject textObj;
    private List<GameObject> texts;
    private List<GameObject> decos;


    // Start is called before the first frame update
    void Start()
    {
        sphereObj = (GameObject)Resources.Load("Sphere");
        textObj = (GameObject)Resources.Load("Text3D");
        spheres = new List<GameObject>();
        texts = new List<GameObject>();
        decos = new List<GameObject>();

        //vis = false;
        vis = true;
        InitMesh();
        InitArray();
        //SetArray();
        //Reduction();

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

        SetArray();
        Reduction();
        Solve();
        ComputeStress();
        CreateMesh(mesh, vertices, vis ? uvs2 : uvs, triangles);
        Decoration();
    }

    void InitMesh()
    {
        vertices = new Vector3[size * size2];
        vertices0 = new Vector3[size * size2];
        normals = new Vector3[size * size2];
        uvs = new Vector2[size * size2];
        uvs2 = new Vector2[size * size2];
        triangles = new int[(size - 1) * size2 * 6]; // attention! 
        //float r = 3.0f;
        //float del_theta = Mathf.PI * 2f / size2;

        for (int i = 0; i < size2; i++)
        {
            for (int j = 0; j < size; j++)
            {

                vertices0[i * size + j] = new Vector3(j * 2f / size, i * 2f / size, 0);
                vertices[i * size + j] = new Vector3(j * 2f / size, i * 2f / size, 0);
                normals[i * size + j] = new Vector3(0, 0, 1);

                uvs[i * size + j] = new Vector2(j * 1f / size, i * 1f / size2);
                uvs2[i * size + j] = new Vector2(j / size, i / size2);

                if (i < size2 - 1 && j < size - 1)
                {
                    /*
                    triangles[(i * (size - 1) + j) * 6] = i * size + j;
                    triangles[(i * (size - 1) + j) * 6 + 1] = (i + 1) * size + (j + 1);
                    triangles[(i * (size - 1) + j) * 6 + 2] = (i + 1) * size + j;
                    triangles[(i * (size - 1) + j) * 6 + 3] = (i + 1) * size + (j + 1);
                    triangles[(i * (size - 1) + j) * 6 + 4] = i * size + j;
                    triangles[(i * (size - 1) + j) * 6 + 5] = i * size + (j + 1);
                    */
                    triangles[(i * (size - 1) + j) * 6] = i * size + j;
                    triangles[(i * (size - 1) + j) * 6 + 1] = (i + 1) * size + j;
                    triangles[(i * (size - 1) + j) * 6 + 2] = (i + 1) * size + (j + 1);
                    triangles[(i * (size - 1) + j) * 6 + 3] = (i + 1) * size + (j + 1);
                    triangles[(i * (size - 1) + j) * 6 + 4] = i * size + (j + 1);
                    triangles[(i * (size - 1) + j) * 6 + 5] = i * size + j;
                }
                if (false && i == size2 - 1 && j < size - 1)
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
        //meshRenderer.material = material;
        meshRenderer.material = submat;
    }

    void InitArray()
    {
        int count = size * size2 * dim;
        Array = new float[count, count];
        Fvector = new float[count];
        Displacement = new float[count];
        TF = new float[count * 2 * 3];
        BF = new float[count * 2];

        for (int i = 0; i < count; i++) // 係数行列の初期化
        {
            for (int j = 0; j < count; j++)
            {
                Array[i, j] = 0;

            }

            Fvector[i] = 0f;

            for (int j = 0; j < 2; j++)
            {
                BF[i * 2 + j] = 0;
            }

            for (int j = 0; j < 6; j++) {
                TF[i * 6 + j] = 0;
            }
        }
        /*
        S1.Add(0 * dim + 0); g1.Add(0);
        S1.Add(0 * dim + 1); g1.Add(0);
        S1.Add(0 * dim + 2); g1.Add(0);

        S1.Add(1 * dim + 1); g1.Add(0);
        S1.Add(1 * dim + 2); g1.Add(0);

        S1.Add(2 * dim + 0); g1.Add(0);
        S1.Add(2 * dim + 2); g1.Add(0);

        S1.Add(3 * dim + 2); g1.Add(0);
        */

        for (int i = 0; i < size * size2; i++)
            set_s1_boundary(i, 2, 0);

        set_s1_boundary(0, 0, 0);
        set_s1_boundary(0, 1, 0);
        set_s1_boundary(size - 1, 1, 0);
        set_s1_boundary(size * (size2 - 1), 0, 0);

        count -= S1.Count;

        // 外力の設定
        set_t_force((size - 1) * (size2 - 1) * 2 - 1, 2, 0, 0);
        for (int i = 0; i < size * size2; i++)
            set_b_force(i, 1, -5);

        R_Array = new float[count, count];
        R_Fvector = new float[count];
        R_index = new int[count];
        values = new float[count];
    }
    void set_s1_boundary(int node, int direction, float force)
    {
        S1.Add(node * dim + direction); g1.Add(force);
    }
    void set_b_force(int element, int direction, float force) {
        BF[element * 3 + direction] = force;
    }
    void set_t_force(int element, int edge, int direction, float force)
    {
        TF[element * 9 + edge * 3 + direction] = force;
    }
    void Reduction()
    {

        int x = 0;
        int y;

        for (int i = 0; i < size * size2 * dim; i++)
        {
            if (S1.Contains(i)) continue;

            y = 0;
            for (int j = 0; j < size * size2 * dim; j++)
            {
                if (S1.Contains(j)) continue;
                
                R_Array[x, y] = Array[i, j];        // x, y はS1境界のインデックスを飛ばしてナンバリングされたもの  (このループ上で空回りしているインデックス)         
                y++;
            }

            R_Fvector[x] = Fvector[i];
            values[x] = 0f;
            R_index[x] = i;                         // x, y の番号の飛び方は一緒なので片方だけ記録すればOK
            x++;
        }
    }
    void SetArray()
    {
        for (int i = 0; i < (size - 1) * (size2 - 1) * 2; i++) // Element
        {
            // Matrix /////////////////////////////////////////////////////////
            int[] index = new int[3];
            index[0] = triangles[i * 3];
            index[1] = triangles[i * 3 + 1];
            index[2] = triangles[i * 3 + 2];

            Vector3[] q = new Vector3[3];
            q[0] = vertices0[index[0]];
            q[1] = vertices0[index[1]];
            q[2] = vertices0[index[2]];

            Vector2[] p = new Vector2[3];
            p[0] = new Vector2(q[0].x, q[0].y);
            p[1] = new Vector2(q[1].x, q[1].y);
            p[2] = new Vector2(q[2].x, q[2].y);

            p[1] -= p[0];
            p[2] -= p[0];
            q[0] *= 0;
            p[0] *= 0;

            float area = Mathf.Abs(p[1].x * p[2].y - p[1].y * p[2].x) * 0.5f;

            float[] a = new float[3];
            a[0] = 0.5f * (p[1].x * p[2].y - p[2].x * p[1].y) / area;
            a[1] = 0.5f * (p[2].x * p[0].y - p[0].x * p[2].y) / area;
            a[2] = 0.5f * (p[0].x * p[1].y - p[1].x * p[0].y) / area;
            
            float[] b = new float[3];
            b[0] = 0.5f * (p[2].y - p[1].y) / area;
            b[1] = 0.5f * (p[0].y - p[2].y) / area;
            b[2] = 0.5f * (p[1].y - p[0].y) / area;

            float[] c = new float[3];
            c[0] = 0.5f * (p[1].x - p[2].x) / area;
            c[1] = 0.5f * (p[2].x - p[0].x) / area;
            c[2] = 0.5f * (p[0].x - p[1].x) / area;
            ///*/
            ///

            float poisson = 1f / 3f;
            float young = 100f;
            float C = young / (1f - poisson * poisson);
            float[,] EMatrix = { {       C * 1, C * poisson,  0,                     0, 0, 0 },
                                 { C * poisson,       C * 1,  0,                     0, 0, 0 },
                                 {          0,            0,  0,                     0, 0, 0 },
                                 {          0,            0,  0, C * (1 - poisson) / 2, 0, 0 },
                                 {          0,            0,  0,                     0, 0, 0 },
                                 {          0,            0,  0,                     0, 0, 0 }};

            float[,] BMatrix = { { b[0],    0, 0, b[1],    0, 0, b[2],    0, 0},
                                 {    0, c[0], 0,    0, c[1], 0,    0, c[2], 0},
                                 {    0,    0, 0,    0,    0, 0,    0,    0, 0},
                                 { c[0], b[0], 0, c[1], b[1], 0, c[2], b[2], 0},
                                 {    0,    0, 0,    0,    0, 0,    0,    0, 0},
                                 {    0,    0, 0,    0,    0, 0,    0,    0, 0}, };

            float[,] BEB = new float[9, 9];

            for (int j = 0; j < 9; j++) {
                for (int k = 0; k < 9; k++) {
                    BEB[j, k] = 0;
                    for (int m = 0; m < 6; m++)
                        for (int n = 0; n < 6; n++)
                            BEB[j, k] += BMatrix[m, j] * EMatrix[m, n] * BMatrix[n, k] * area;
                }
            }

            for (int j = 0; j < 3; j++)
                for (int k = 0; k < 3; k++)
                    for (int m = 0; m < 3; m++)
                        for (int n = 0; n < 3; n++) // BEBを3×3に分割して要素のシフト (j, k) -> (index[j] -> index[k])
                            Array[index[j] * 3 + m, index[k] * 3 + n] += BEB[j * 3 + m, k * 3 + n];

            // Vector /////////////////////////////////////////////////////////
            if (BF[3 * i + 0] != 0 || BF[3 * i + 1] != 0 || BF[3 * i + 2] != 0)
            {
                for (int j = 0; j < 3; j++)     // 頂点数
                {
                    for (int k = 0; k < 3; k++) // 次元
                    {
                        Fvector[index[j] * 3 + k] += area / 3 * BF[3 * i + k]; // 荷重は頂点（j）は無関係
                    }
                }
            }

            for (int j = 0; j < 3; j++)     // エッジ -> 向かい合う頂点の番号
            {
                if (TF[i * 9 + j * 3 + 0] != 0 || TF[i * 9 + j * 3 + 1] != 0 || TF[i * 9 + j * 3 + 2] != 0)
                {
                    float L = (p[(j + 1) % 3] - p[(j + 2) % 3]).magnitude;
                    float N0 = j != 0 ? L / 2 : 0;
                    float N1 = j != 1 ? L / 2 : 0;
                    float N2 = j != 2 ? L / 2 : 0;
                    float[,] N = { { N0,  0,  0,  N1,  0, 0, N2,  0, 0},
                                   {  0, N0,  0,   0, N1, 0,  0, N2, 0},
                                   {  0,  0,  0,   0,  0, 0,  0,  0, 0},};
                    float[] V = { TF[i * 9 + j * 3 + 0], TF[i * 9 + j * 3 + 1], TF[i * 9 + j * 3 + 2] };
                    float[] F = Solver.MatTxVec(N, V);

                    for (int k = 0; k < 3; k++) // 要素内インデックス
                    {
                        for (int m = 0; m < 3; m++) // 次元
                        {
                            Fvector[index[k] * 3 + m] += F[k * 3 + m];
                        }
                    }
                }
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
            //g1[i] = new Vector3 (0, 0, y == 0 ? Mathf.Sin(0.1f + Time.time) * ratio * 0.4f + Mathf.Sin((x + y) * 0.5f + Time.time) * ratio * 0.6f : -2f);
        }

        set_t_force((size - 1) * (size2 - 1) * 2 - 1, 2, 0, Mathf.Sin(Time.time * 2.0f) * 100f);
        set_t_force((size - 1) * (size2 - 1) * 2 - 1, 2, 1, Mathf.Cos(Time.time * 2.0f) * 100f);

    }
    void Solve()
    {
        Solver.PrintVector(R_Fvector);
        Solver.PrintArray(R_Array);
        Solver.ICCG(R_Array, R_Fvector, values);
        Solver.PrintVector(values);

        //float exg = 1f;

        for (int i = 0; i < vertices.Length; i++)
            vertices[i] = vertices0[i];

        for (int i = 0; i < values.Length; i++) {
            Displacement[R_index[i]] = values[i];
        }
        for (int i = 0; i < S1.Count; i++)
        {
            Displacement[S1[i]] = g1[i];
        }

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = vertices0[i] + new Vector3(Displacement[i * 3], Displacement[i * 3 + 1], Displacement[i * 3 + 2]);
        }
    }

    void ComputeStress() {
        float[] diffs = new float[vertices.Length]; 
        int[] count = new int[vertices.Length];// 頂点ごとの平均を取るためのカウンタ
        for (int i = 0; i < vertices.Length; i++)
            count[i] = 0;

        foreach (GameObject S in spheres)
            Destroy(S);
        spheres.Clear();
        
        for (int i = 0; i < (size - 1) * (size2 - 1) * 2; i++) // Element
        {
            // Matrix /////////////////////////////////////////////////////////
            int[] index = new int[3];
            index[0] = triangles[i * 3];
            index[1] = triangles[i * 3 + 1];
            index[2] = triangles[i * 3 + 2];

            Vector3[] q = new Vector3[3];
            q[0] = vertices[index[0]];
            q[1] = vertices[index[1]];
            q[2] = vertices[index[2]];

            Vector2[] p = new Vector2[3];
            p[0] = new Vector2(q[0].x, q[0].y);
            p[1] = new Vector2(q[1].x, q[1].y);
            p[2] = new Vector2(q[2].x, q[2].y);

            p[1] -= p[0];
            p[2] -= p[0];
            p[0] *= 0;

            float area = Mathf.Abs(p[1].x * p[2].y - p[1].y * p[2].x) * 0.5f;

            float[] a = new float[3];
            a[0] = 0.5f * (p[1].x * p[2].y - p[2].x * p[1].y) / area;
            a[1] = 0.5f * (p[2].x * p[0].y - p[0].x * p[2].y) / area;
            a[2] = 0.5f * (p[0].x * p[1].y - p[1].x * p[0].y) / area;

            float[] b = new float[3];
            b[0] = 0.5f * (p[2].y - p[1].y) / area;
            b[1] = 0.5f * (p[0].y - p[2].y) / area;
            b[2] = 0.5f * (p[1].y - p[0].y) / area;

            float[] c = new float[3];
            c[0] = 0.5f * (p[1].x - p[2].x) / area;
            c[1] = 0.5f * (p[2].x - p[0].x) / area;
            c[2] = 0.5f * (p[0].x - p[1].x) / area;
            ///*/
            ///

            float poisson = 1f / 3f;
            float young = 100f;
            float C = young / (1f - poisson * poisson);
            float[,] EMatrix = { {       C * 1, C * poisson,  0,                     0, 0, 0 },
                                 { C * poisson,       C * 1,  0,                     0, 0, 0 },
                                 {          0,            0,  0,                     0, 0, 0 },
                                 {          0,            0,  0, C * (1 - poisson) / 2, 0, 0 },
                                 {          0,            0,  0,                     0, 0, 0 },
                                 {          0,            0,  0,                     0, 0, 0 }};

            float[,] BMatrix = { { b[0],    0, 0, b[1],    0, 0, b[2],    0, 0},
                                 {    0, c[0], 0,    0, c[1], 0,    0, c[2], 0},
                                 {    0,    0, 0,    0,    0, 0,    0,    0, 0},
                                 { c[0], b[0], 0, c[1], b[1], 0, c[2], b[2], 0},
                                 {    0,    0, 0,    0,    0, 0,    0,    0, 0},
                                 {    0,    0, 0,    0,    0, 0,    0,    0, 0}, };
            float[] E_Dis = { Displacement[index[0] * 3 + 0], Displacement[index[0] * 3 + 1], Displacement[index[0] * 3 + 2],
                                Displacement[index[1] * 3 + 0], Displacement[index[1] * 3 + 1], Displacement[index[1] * 3 + 2],
                                Displacement[index[2] * 3 + 0], Displacement[index[2] * 3 + 1], Displacement[index[2] * 3 + 2]};
            float[] E_Strain = Solver.MatxVec(BMatrix, E_Dis);
            float[] E_Stress = Solver.MatxVec(EMatrix, E_Strain);
            float[] eigenVal = new float[dim];
            float[,] eigenVec = new float[dim, dim];
            Solver.jacobi3(VectoSymMat(E_Stress), eigenVal, eigenVec);

            GameObject sphereIns = Instantiate(sphereObj, (q[0] + q[1] + q[2]) / 3, Solver.transformRotMatToQuaternion(eigenVec));
            sphereIns.GetComponent<Transform>().localScale = new Vector3(eigenVal[0] + 2f, eigenVal[1] + 2f, eigenVal[2] + 2f) * 0.25f * vis_scale;
            spheres.Add(sphereIns);
            //GameObject textIns = Instantiate(textObj, (q[0] + q[1] + q[2]) / 3 + new Vector3(0, 0.15f * (i % 4 < 2 ? -1 : 1), -0.1f), Quaternion.identity);
            //textIns.GetComponent<TextMesh>().text = "(" + eigenVec[0, 0].ToString("f3") + ", " + eigenVec[1, 0].ToString("f3") + ", " + eigenVec[2, 0].ToString("f3") + ")";
            //texts.Add(textIns);

            float diff = Mathf.Max(eigenVal[0], eigenVal[1], eigenVal[2]) - Mathf.Min(eigenVal[0], eigenVal[1], eigenVal[2]);
            diffs[index[0]] = diffs[index[1]] = diffs[index[2]] = diff;
            count[index[0]]++; count[index[0]]++; count[index[0]]++;
        }
        for (int i = 0; i < vertices.Length; i++)
            uvs2[i].x = Mathf.Clamp(diffs[i] / count[i], 0.01f, 1f - 0.01f); ;

    }

    float[,] VectoSymMat(float[] vec) {

        float[,] mat = new float[dim, dim];
        mat[0, 0] = vec[0];
        mat[1, 1] = vec[1];
        mat[2, 2] = vec[2];
        mat[0, 1] = vec[3];
        mat[1, 0] = vec[3];
        mat[1, 2] = vec[4];
        mat[2, 1] = vec[4];
        mat[2, 0] = vec[5];
        mat[0, 2] = vec[5];
        return mat;

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
        mesh.uv = vis ? uvs2 : uvs;
        //　三角形メッシュの設定
        mesh.triangles = triangles;
        //　Boundsの再計算
        mesh.RecalculateBounds();
        //　NormalMapの再計算
        mesh.RecalculateNormals();
    }
    void Decoration() {
        foreach (GameObject S in decos)
            Destroy(S);
        decos.Clear();

        GameObject fulcrum1Obj = (GameObject)Resources.Load("fulcrum1");

        GameObject fulcrum1Ins = Instantiate(fulcrum1Obj, vertices[size - 1] - new Vector3(0, 0.1f, 0), Quaternion.Euler(0, 90, 0));
        fulcrum1Ins.GetComponent<Transform>().localScale = new Vector3(1f, 1f, 1f) * 0.1f;
        decos.Add(fulcrum1Ins);
        fulcrum1Ins = Instantiate(fulcrum1Obj, vertices[size * (size2 - 1)] - new Vector3(0.1f, 0, 0), Quaternion.Euler(90, 90, 0));
        fulcrum1Ins.GetComponent<Transform>().localScale = new Vector3(1f, 1f, 1f) * 0.1f;
        decos.Add(fulcrum1Ins);

        GameObject fulcrum2Obj = (GameObject)Resources.Load("fulcrum2");

        GameObject fulcrum2Ins = Instantiate(fulcrum2Obj, vertices[0] - new Vector3(0, 0.1f, 0), Quaternion.Euler(0, 90, 0));
        fulcrum2Ins.GetComponent<Transform>().localScale = new Vector3(1f, 1f, 1f) * 0.1f;
        decos.Add(fulcrum2Ins);

        GameObject PointObj = (GameObject)Resources.Load("Point");

        GameObject PointIns = Instantiate(PointObj, vertices[vertices.Length - 1], Quaternion.Euler(0, 0, 0));
        PointIns.GetComponent<Transform>().localScale = new Vector3(1f, 1f, 1f) * 0.1f;
        decos.Add(PointIns);
        //GameObject fulcrum2Obj = (GameObject)Resources.Load("fulcrum2");

    }
}

