using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TensorFEM_SP : MonoBehaviour
{
    //　頂点配列
    private Vector3[] vertices;
    private Vector3[] vertices0;
    private Vector3[] normals;
    //　UV配列
    private Vector2[] uvs;
    private Vector2[] uvs2;
    private Color[] colors;

    //　三角形の順番配列
    private int[] triangles;
    private Mesh mesh;
    //　メッシュ表示コンポーネント
    private MeshRenderer meshRenderer;
    //　メッシュに設定するマテリアル
    public Material material;
    public Material submat;
    public Material binarymat;
    private int mode; // 0: photoelasticity, 1: Stress Visualization, 2: Force Visualizaiton, 3: Boundary
    private int polarizer;

    public List<int> S1;    // 第一境界条件
    public List<float> g1;  // 境界値
    public List<int> S1_conj;   // 縮約行列→もとの行列
    public List<int> S2;    // 第二境界条件
    public List<float> g2;  // 境界値
    private bool Direct;
    private Vector2 MousePos;//
    private bool Direct2;
    private Vector2 MousePos2;

    public float[] BF;
    public float[] TF;
    private int size = 16;
    private int size2 = 16;
    private int dim = 3;
    public float vis_scale = 0.05f;
    private float ratio = 1f;
    private float scale = 1f;
    SPMatrix Array;
    SPMatrix R_Array;
    public float[] Fvector;          // 全体荷重
    public float[] R_Fvector;        //
    public float[] values;           // (頂点数 - 境界)×次元数
    public float[] Displacement;           // 頂点数×次元数
    private Texture2D ColorMatchingFunction;

    private List<TriangleElement> elements;
    private GameObject sphereObj;
    private List<GameObject> spheres;
    private GameObject arrowObj;
    private List<GameObject> arrows;
    private GameObject textObj;
    private List<GameObject> texts;
    private List<GameObject> decos;


    // Start is called before the first frame update
    void Start()
    {
        sphereObj = (GameObject)Resources.Load("Sphere");
        arrowObj = (GameObject)Resources.Load("arrow");
        textObj = (GameObject)Resources.Load("Text3D");
        spheres = new List<GameObject>();
        arrows = new List<GameObject>();
        texts = new List<GameObject>();
        decos = new List<GameObject>();

        mode = 0;
        polarizer = 1;
        InitMesh();
        InitArray();
        InitElement();
        InitObject();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V)) Visualization(1);
        if (Input.GetKeyDown(KeyCode.F)) Visualization(2);
        if (Input.GetKeyDown(KeyCode.B)) Visualization(3);
        if (Input.GetKeyDown(KeyCode.C)) Polarizer(0);
        if (Input.GetKeyDown(KeyCode.L)) Polarizer(1);
        if (Input.GetKeyDown(KeyCode.I)) Polarizer(2);

        SetArray();
        Reduction();
        Solve();
        ComputeStress();
        CreateMesh(mesh, vertices, mode == 0 ? uvs : uvs2, triangles);
        Decoration();

        UpdateBoundary();
    }

    void InitMesh()
    {
        vertices = new Vector3[size * size2];
        vertices0 = new Vector3[size * size2];
        normals = new Vector3[size * size2];
        uvs = new Vector2[size * size2];
        uvs2 = new Vector2[size * size2];
        colors = new Color[size * size2];
        triangles = new int[(size - 1) * size2 * 6]; // attention! 
        //float r = 3.0f;
        //float del_theta = Mathf.PI * 2f / size2;

        for (int i = 0; i < size2; i++)
        {
            for (int j = 0; j < size; j++)
            {

                vertices0[i * size + j] = new Vector3(j * 2f / size, i * 2f / size2, 0);
                vertices[i * size + j] = new Vector3(j * 2f / size, i * 2f / size2, 0);
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
        meshRenderer.material = mode == 0 ? material : (mode == 3 ? binarymat : submat);
    }

    void InitArray()
    {
        int count = size * size2 * dim;
        Array = new SPMatrix(count);
        //Debug.Log("A Count: " + Array.A.Count);
        //Debug.Log("Max Index: " + Array.IA[Array.IA.Count - 1]);
        Fvector = new float[count];
        Displacement = new float[count];
        TF = new float[count * 2 * 3];
        BF = new float[count * 2];

        for (int i = 0; i < count; i++) // 係数行列の初期化
        {

            Fvector[i] = 0f;

            for (int j = 0; j < 2; j++)
            {
                BF[i * 2 + j] = 0;
            }

            for (int j = 0; j < 6; j++)
            {
                TF[i * 6 + j] = 0;
            }
            S1_conj.Add(i);
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
        //set_t_force((size - 1) * (size2 - 1) * 2 - 1, 2, 0, 0);
        for (int i = 0; i < size * size2 * 2; i++)
            set_b_force(i, 1, -2f);

        R_Fvector = new float[count];
        values = new float[count];
    }
    void InitElement() {
        elements = new List<TriangleElement>();
        for (int i = 0; i < (size - 1) * (size2 - 1) * 2; i++) // Element
        {
            TriangleElement TriE = new TriangleElement();
            elements.Add(TriE);
            TriE.V[0] = triangles[i * 3];
            TriE.V[1] = triangles[i * 3 + 1];
            TriE.V[2] = triangles[i * 3 + 2];

            Vector3[] q = new Vector3[3];
            q[0] = vertices0[TriE.V[0]];
            q[1] = vertices0[TriE.V[1]];
            q[2] = vertices0[TriE.V[2]];

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

            float poisson = 1f / 3f;
            float young = 200f;
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

            TriE.P0[0] = q[0]; TriE.P0[1] = q[1]; TriE.P0[2] = q[2];
            TriE.A = area;
            TriE.p = poisson;
            TriE.y = young;
            TriE.E = EMatrix;
            TriE.B = BMatrix;

            float[,] BEB = new float[9, 9];
            for (int j = 0; j < 9; j++)
            {
                for (int k = 0; k < 9; k++)
                {
                    BEB[j, k] = 0;
                    for (int m = 0; m < 6; m++)
                        for (int n = 0; n < 6; n++)
                            BEB[j, k] += TriE.B[m, j] * TriE.E[m, n] * TriE.B[n, k] * TriE.A;
                }
            }

            TriE.K = BEB;
        }

    }
    void InitObject() {
        for (int i = 0; i < (size - 1) * (size2 - 1) * 2; i++) // Element
        {
            GameObject sphereIns = Instantiate(sphereObj, new Vector3(0, 0, 0), Quaternion.identity);
            sphereIns.SetActive(mode == 1);
            spheres.Add(sphereIns);

            GameObject arrowIns = Instantiate(arrowObj, new Vector3(0, 0, 0), Quaternion.identity);
            arrowIns.SetActive(mode == 2);
            arrows.Add(arrowIns);
        }

        GameObject fulcrum1Obj = (GameObject)Resources.Load("fulcrum1");

        GameObject fulcrum1Ins = Instantiate(fulcrum1Obj, new Vector3(0, 0, 0), Quaternion.Euler(0, 90, 0));
        fulcrum1Ins.GetComponent<Transform>().localScale = new Vector3(1f, 1f, 1f) * 0.1f;
        decos.Add(fulcrum1Ins);
        fulcrum1Ins = Instantiate(fulcrum1Obj, new Vector3(0, 0, 0), Quaternion.Euler(90, 90, 0));
        fulcrum1Ins.GetComponent<Transform>().localScale = new Vector3(1f, 1f, 1f) * 0.1f;
        decos.Add(fulcrum1Ins);

        GameObject fulcrum2Obj = (GameObject)Resources.Load("fulcrum2");

        GameObject fulcrum2Ins = Instantiate(fulcrum2Obj, new Vector3(0, 0, 0), Quaternion.Euler(0, 90, 0));
        fulcrum2Ins.GetComponent<Transform>().localScale = new Vector3(1f, 1f, 1f) * 0.1f;
        decos.Add(fulcrum2Ins);

        GameObject PointObj = (GameObject)Resources.Load("Point");

        GameObject PointIns = Instantiate(PointObj, new Vector3(0, 0, 0), Quaternion.Euler(0, 0, 0));
        PointIns.GetComponent<Transform>().localScale = new Vector3(1f, 1f, 1f) * 0.1f;
        decos.Add(PointIns);
    }
    void set_s1_boundary(int node, int direction, float disp)
    {
        int index = S1.IndexOf(node * dim + direction);
        if (index < 0)
        {
            S1.Add(node * dim + direction); g1.Add(disp);
            S1_conj.Remove(node * dim + direction);
        }
        else {
            g1[index] = disp;
        }
    }
    void remove_s1_boundary(int node, int direction)
    {
        int index = S1.IndexOf(node * dim + direction);
        if (index >= 0)
        {
            S1.RemoveAt(index); g1.RemoveAt(index);
            for (int i = 0; i < S1_conj.Count; i++)
                if (node * dim + direction < S1_conj[i]) {
                    S1_conj.Insert(i, node * dim + direction);
                    break;
                }
        }
    }

    void set_b_force(int element, int direction, float force)
    {
        BF[element * 3 + direction] = force;
    }
    void set_t_force(int element, int edge, int direction, float force)
    {
        TF[element * 9 + edge * 3 + direction] = force;
    }
    void Reduction()
    {
        R_Array = new SPMatrix(Array);
        R_Array.RemoveRowCol(S1);
        R_Fvector = new float[S1_conj.Count];
        for (int i = 0; i < S1_conj.Count; i++)
            R_Fvector[i] = Fvector[S1_conj[i]];
    }
    void SetArray()
    {
        for (int i = 0; i < Fvector.Length; i++)
            Fvector[i] = 0;
        Array = new SPMatrix(size * size2 * dim);
        foreach (TriangleElement TriE in elements) // Element
        {
            int i = TriE.Index;
            for (int j = 0; j < 3; j++)
                for (int k = 0; k < 3; k++)
                    for (int m = 0; m < 3; m++)
                        for (int n = 0; n < 3; n++) // BEBを3×3に分割して要素のシフト (j, k) -> (index[j] -> index[k])
                        { Array.AddElement(TriE.V[j] * 3 + m, TriE.V[k] * 3 + n, TriE.K[j * 3 + m, k * 3 + n]); }

            // Vector /////////////////////////////////////////////////////////
            Vector2[] p = new Vector2[3];
            p[0] = new Vector2(TriE.P0[0].x, TriE.P0[0].y);
            p[1] = new Vector2(TriE.P0[1].x, TriE.P0[1].y);
            p[2] = new Vector2(TriE.P0[2].x, TriE.P0[2].y);

            

            if (BF[3 * i + 0] != 0 || BF[3 * i + 1] != 0 || BF[3 * i + 2] != 0)
            {
                for (int j = 0; j < 3; j++)     // 頂点数
                {
                    for (int k = 0; k < 3; k++) // 次元
                    {
                        Fvector[TriE.V[j] * 3 + k] += TriE.A / 3 * BF[3 * i + k]; // 荷重は頂点（j）は無関係
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
                    //float[] N = { N0, N0, 0, N1, N1, 0, N2, N2, 0 };
                    float[] V = { TF[i * 9 + j * 3 + 0], TF[i * 9 + j * 3 + 1], TF[i * 9 + j * 3 + 2] };
                    float[] F = Solver.MatTxVec(N, V);
                    //float[] F = Solver.SimpleProduct(N, V);

                    for (int k = 0; k < 3; k++) // 要素内インデックス
                    {
                        for (int m = 0; m < 3; m++) // 次元
                        {
                            Fvector[TriE.V[k] * 3 + m] += F[k * 3 + m];
                        }
                    }
                }
            }
        }
    }

    void UpdateBoundary()
    {
        //for (int i = 0; i < g1.Count; i++)
        //{
        //    int x = S1[i] / size;
        //    int y = S1[i] % size;
            //g1[i] = Mathf.Sin((x + y) * 0.5f + Time.time) * ratio;
            //g1[i] = x == 0 ? Mathf.Sin((x + y) * 0.3f + Time.time) * ratio : Mathf.Sin((x + y) * 0.3f - Time.time) * ratio;
            //g1[i] = y == 0 ? 1.0f : -1.0f;
            //g1[i] = new Vector3 (0, 0, y == 0 ? Mathf.Sin(0.1f + Time.time) * ratio * 0.4f + Mathf.Sin((x + y) * 0.5f + Time.time) * ratio * 0.6f : -2f);
        //}

        set_t_force((size - 1) * (size2 - 1) * 2 - 1, 2, 0, Mathf.Sin(Time.time * 2.0f) * 5f);
        set_t_force((size - 1) * (size2 - 1) * 2 - 1, 2, 1, Mathf.Cos(Time.time * 2.0f) * 5f);

        if (!Direct && Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1))
        {
            Direct = true;
            MousePos = Input.mousePosition;
        }
        if (Direct && Input.GetMouseButton(0) && !Input.GetMouseButtonDown(1))
        {
            Vector2 touchScreenPosition = Input.mousePosition;

            touchScreenPosition.x = Mathf.Clamp(touchScreenPosition.x, 0.0f, Screen.width);
            touchScreenPosition.y = Mathf.Clamp(touchScreenPosition.y, 0.0f, Screen.height);

            Camera gameCamera = Camera.main;
            Ray touchPointToRay = gameCamera.ScreenPointToRay(touchScreenPosition);
            Vector3 v;
            foreach (TriangleElement TriE in elements)
            {
                v = (vertices[TriE.V[0]] + vertices[TriE.V[1]] + vertices[TriE.V[2]]) / 3 - touchPointToRay.origin;
                if (Vector3.Dot(v, v) - Mathf.Pow(Vector3.Dot(v, touchPointToRay.direction), 2) / Vector3.Dot(touchPointToRay.direction, touchPointToRay.direction) < 0.01f)
                {
                    Vector2 Move = touchScreenPosition - MousePos;
                    set_b_force(TriE.Index, 0, BF[TriE.Index * 3 + 0] + Move.x * 0.5f);
                    set_b_force(TriE.Index, 1, BF[TriE.Index * 3 + 1] + Move.y * 0.5f);
                }
            }
            MousePos = touchScreenPosition;
        }
        if (Direct && Input.GetMouseButtonUp(0) && !Input.GetMouseButtonDown(1))
        {
            Direct = false;
        }


        if (Input.GetMouseButton(1) && !Input.GetMouseButtonDown(0))
        {
            Vector2 touchScreenPosition = Input.mousePosition;

            touchScreenPosition.x = Mathf.Clamp(touchScreenPosition.x, 0.0f, Screen.width);
            touchScreenPosition.y = Mathf.Clamp(touchScreenPosition.y, 0.0f, Screen.height);

            Camera gameCamera = Camera.main;
            Ray touchPointToRay = gameCamera.ScreenPointToRay(touchScreenPosition);
            Vector3 v;
            foreach (TriangleElement TriE in elements)
            {
                v = (vertices[TriE.V[0]] + vertices[TriE.V[1]] + vertices[TriE.V[2]]) / 3 - touchPointToRay.origin;
                if (Vector3.Dot(v, v) - Mathf.Pow(Vector3.Dot(v, touchPointToRay.direction), 2) / Vector3.Dot(touchPointToRay.direction, touchPointToRay.direction) < 0.05f)
                {
                    set_b_force(TriE.Index, 0, 0);
                    set_b_force(TriE.Index, 1, -2f);
                }
            }
        }

        if (Input.GetMouseButton(2))
        {
            Vector2 touchScreenPosition = Input.mousePosition;

            touchScreenPosition.x = Mathf.Clamp(touchScreenPosition.x, 0.0f, Screen.width);
            touchScreenPosition.y = Mathf.Clamp(touchScreenPosition.y, 0.0f, Screen.height);

            Camera gameCamera = Camera.main;
            Ray touchPointToRay = gameCamera.ScreenPointToRay(touchScreenPosition);
            Vector3 v;
            for (int i = 0; i < vertices.Length; i++)
            {
                v = vertices[i] - touchPointToRay.origin;
                if (Vector3.Dot(v, v) - Mathf.Pow(Vector3.Dot(v, touchPointToRay.direction), 2) / Vector3.Dot(touchPointToRay.direction, touchPointToRay.direction) < 0.01f)
                {
                    Vector3 u = vertices[i] - vertices0[i];
                    //set_s1_boundary(i, 0, u.x);
                    set_s1_boundary(i, 0, 0);
                    //set_s1_boundary(i, 1, u.y);
                    set_s1_boundary(i, 1, 0);
                }
            }
        }

        if (Input.GetMouseButton(0) && Input.GetMouseButton(1))
        {
            Vector2 touchScreenPosition = Input.mousePosition;

            touchScreenPosition.x = Mathf.Clamp(touchScreenPosition.x, 0.0f, Screen.width);
            touchScreenPosition.y = Mathf.Clamp(touchScreenPosition.y, 0.0f, Screen.height);

            Camera gameCamera = Camera.main;
            Ray touchPointToRay = gameCamera.ScreenPointToRay(touchScreenPosition);
            Vector3 v;
            for (int i = 0; i < vertices.Length; i++)
            {
                v = vertices[i] - touchPointToRay.origin;
                if (Vector3.Dot(v, v) - Mathf.Pow(Vector3.Dot(v, touchPointToRay.direction), 2) / Vector3.Dot(touchPointToRay.direction, touchPointToRay.direction) < 0.05f)
                {
                    if (i != 0 && i != size - 1 && i != size * (size2 - 1)) {
                        remove_s1_boundary(i, 0);
                        remove_s1_boundary(i, 1);
                    }
                }
            }
        }
    }
    void Solve()
    {
        //Solver.PrintVector(R_Fvector);
        //R_Array.PrintMatrix();
        //Solver.PrintList(R_Array.A);
        //Solver.PrintList(R_Array.IA);
        //Solver.PrintList(R_Array.JA);
        values = Solver.ICCG(R_Array, R_Fvector);
        //Solver.PrintVector(values);
        
        //float exg = 1f;

        for (int i = 0; i < vertices.Length; i++)
            vertices[i] = vertices0[i];

        for (int i = 0; i < S1_conj.Count; i++)
        {
            Displacement[S1_conj[i]] = values[i];
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

    void ComputeStress()
    {
        float[] diffs = new float[vertices.Length];
        float[] tilts = new float[vertices.Length];
        Vector3[] force = new Vector3[vertices.Length];
        int[] count = new int[vertices.Length];// 頂点ごとの平均を取るためのカウンタ
        for (int i = 0; i < vertices.Length; i++) {
            diffs[i] = 0;
            tilts[i] = 0;
            force[i] = new Vector3(0, 0, 0);
            count[i] = 0;
        }
        foreach (TriangleElement TriE in elements) // Element
        {
            int i = TriE.Index;
            float[] E_Dis = { Displacement[TriE.V[0] * 3 + 0], Displacement[TriE.V[0] * 3 + 1], Displacement[TriE.V[0] * 3 + 2],
                                Displacement[TriE.V[1] * 3 + 0], Displacement[TriE.V[1] * 3 + 1], Displacement[TriE.V[1] * 3 + 2],
                                Displacement[TriE.V[2] * 3 + 0], Displacement[TriE.V[2] * 3 + 1], Displacement[TriE.V[2] * 3 + 2]};
            float[] E_Strain = Solver.MatxVec(TriE.B, E_Dis);
            float[] E_Stress = Solver.MatxVec(TriE.E, E_Strain);
            float[] eigenVal = new float[dim];
            float[,] eigenVec = new float[dim, dim];
            Solver.jacobi3(VectoSymMat(E_Stress), eigenVal, eigenVec);

            //GameObject sphereIns = Instantiate(sphereObj, (q[0] + q[1] + q[2]) / 3, Solver.transformRotMatToQuaternion(eigenVec));
            Vector3 u = new Vector3(E_Dis[0] + E_Dis[3] + E_Dis[6],
                                    E_Dis[1] + E_Dis[4] + E_Dis[7],
                                    E_Dis[2] + E_Dis[5] + E_Dis[8]) / 3;
            spheres[i].GetComponent<Transform>().position = (TriE.P0[0] + TriE.P0[1] + TriE.P0[2]) / 3 + u;
            spheres[i].GetComponent<Transform>().rotation = Solver.transformRotMatToQuaternion(eigenVec);
            spheres[i].GetComponent<Transform>().localScale = new Vector3(eigenVal[0] + 4f, eigenVal[1] + 4f, eigenVal[2] + 4f) * 0.15f * vis_scale;

            float diff = Mathf.Max(eigenVal[0], eigenVal[1], eigenVal[2]) - Mathf.Min(eigenVal[0], eigenVal[1], eigenVal[2]);
            diffs[TriE.V[0]] += diff; diffs[TriE.V[1]] += diff; diffs[TriE.V[2]] += diff;
            float tilt = 4 * Mathf.Pow(eigenVec[0, 0] * eigenVec[0, 1], 2);
            tilts[TriE.V[0]] += tilt;
            tilts[TriE.V[1]] += tilt;
            tilts[TriE.V[2]] += tilt;
            count[TriE.V[0]]++; count[TriE.V[1]]++; count[TriE.V[2]]++;

            Vector3 EBF = new Vector3(BF[i * 3 + 0], BF[i * 3 + 1], BF[i * 3 + 2]);
            for (int j = 0; j < 3; j++) {
                force[TriE.V[j]] += EBF / 3;
                //force[TriE.V[j]] += new Vector3(TF[i * 9 + j * 3 + 0], TF[i * 9 + j * 3 + 1], TF[i * 9 + j * 3 + 2]) / 2;
            }
            arrows[i].GetComponent<Transform>().position = (TriE.P0[0] + TriE.P0[1] + TriE.P0[2]) / 3 + u;
            arrows[i].GetComponent<Transform>().up = EBF.normalized;
            arrows[i].GetComponent<Transform>().localScale = new Vector3(30f, EBF.magnitude / 20f, 30f) * 0.2f * vis_scale;

        }
        for (int i = 0; i < vertices.Length; i++)
        {
            if (mode == 0)
            {
                float r = polarizer == 2 ? 1 : diffs[i] / count[i] * 0.2f;
                float g = polarizer == 0 ? 0.5f : tilts[i] / count[i];
                float b = 0;
                colors[i] = new Color(r, g, b, 1);
            }
            else if (mode == 1)
            {
                uvs2[i].x = Mathf.Clamp(diffs[i] / count[i] * 0.1f, 0.01f, 1f - 0.01f); // diffが10程度の値をとるので×0.1で補正
            }
            else if (mode == 2)
            {
                uvs2[i].x = Mathf.Clamp(force[i].magnitude * 0.01f, 0.01f, 1f - 0.01f);
            }
            else {
                uvs2[i].x = 0.99f;
            }
        }
        if (mode == 3) {
            for (int i = 0; i < S1.Count; i++) {
                if (S1[i] % dim == 2)
                    continue;
                uvs2[S1[i] / dim].x = 0.01f;
            }
        }


        ColorMatchingFunction = new Texture2D(
            40,
            1,
            TextureFormat.RGBA32,
            false);

        Color[] colorArray = new Color[40];
        colorArray[0] = new Color(0.00003f, - 0.00001f,    0.00117f);    // 380
        //colorArray[0] = new Color(0.00005f, - 0.00002f,    0.00189f);    // 385
        colorArray[1] = new Color(0.00010f, - 0.00004f,    0.00359f);    // 390
        //colorArray[0] = new Color(0.00017f, - 0.00007f,    0.00647f);    // 395

        colorArray[2] = new Color(0.00030f, - 0.00014f,    0.01214f);    // 400
        //colorArray[0] = new Color(0.00047f, - 0.00022f,    0.01969f);    // 405
        colorArray[3] = new Color(0.00084f, - 0.00041f,    0.03707f);    // 410
        //colorArray[0] = new Color(0.00139f, - 0.00070f,    0.06637f);    // 415
        colorArray[4] = new Color(0.00211f, - 0.00110f,    0.11541f);    // 420
        //colorArray[0] = new Color(0.00266f, - 0.00143f,    0.18575f);    // 425
        colorArray[5] = new Color(0.00218f, - 0.00119f,    0.24769f);    // 430
        //colorArray[0] = new Color(0.00036f, - 0.00021f,    0.29012f);    // 435
        colorArray[6] = new Color(-0.0026f, 0.00149f, 0.31228f);    // 440
        //colorArray[0] = new Color(-0.0067f, 0.00379f, 0.31860f);    // 445

        colorArray[7] = new Color(-0.0121f, 0.00678f, 0.31670f);    // 450
        //colorArray[0] = new Color(-0.0187f, 0.01046f, 0.31166f);    // 455
        colorArray[8] = new Color(-0.0260f, 0.01485f, 0.29821f);    // 460
        //colorArray[0] = new Color(-0.0332f, 0.01977f, 0.27295f);    // 465
        colorArray[9] = new Color(-0.0393f, 0.02538f, 0.22991f);    // 470
        //colorArray[0] = new Color(-0.0447f, 0.03183f, 0.18592f);    // 475
        colorArray[10] = new Color(-0.0493f, 0.03914f, 0.14494f);    // 480
        //colorArray[0] = new Color(-0.0536f, 0.04713f, 0.10968f);    // 485
        colorArray[11] = new Color(-0.0581f, 0.05689f, 0.08257f);    // 490
        //colorArray[0] = new Color(-0.0641f, 0.06948f, 0.06246f);    // 495

        colorArray[12] = new Color(-0.0713f, 0.08536f, 0.04776f);    // 500
        //colorArray[0] = new Color(-0.0812f, 0.10593f, 0.03688f);    // 505
        colorArray[13] = new Color(-0.0890f, 0.12860f, 0.02698f);    // 510
        //colorArray[0] = new Color(-0.0935f, 0.15262f, 0.01842f);    // 515
        colorArray[14] = new Color(-0.0926f, 0.17468f, 0.01221f);    // 520
        //colorArray[0] = new Color(-0.0847f, 0.19113f, 0.00830f);    // 525
        colorArray[15] = new Color(-0.0847f, 0.19113f, 0.00830f);    // 530
        //colorArray[0] = new Color(-0.0710f, 0.20317f, 0.00549f);    // 535
        colorArray[16] = new Color(-0.0315f, 0.21466f, 0.00146f);    // 540
        //colorArray[0] = new Color(-0.0061f, 0.21487f, 0.00023f);    // 545

        colorArray[17] = new Color(0.02279f, 0.21178f, -0.00058f);    // 550
        //colorArray[0] = new Color(0.05514f, 0.20588f, -0.00105f);
        colorArray[18] = new Color(0.09060f, 0.19702f, -0.00130f);
        //colorArray[0] = new Color(0.12840f,	0.18522f,	-0.00138f);
        colorArray[19] = new Color(0.16768f,	0.17087f,	-0.00135f);
        //colorArray[0] = new Color(0.20715f,	0.15429f,	-0.00123f);
        colorArray[20] = new Color(0.24562f,	0.13610f,	-0.00108f);
        //colorArray[0] = new Color(0.27989f,	0.11686f,	-0.00093f);
        colorArray[21] = new Color(0.30928f,	0.09754f,	-0.00079f);
        //colorArray[0] = new Color(0.33184f,	0.07909f,	-0.00063f);

        colorArray[22] = new Color(0.34429f,	0.06246f,	-0.00049f); // 600
        //colorArray[0] = new Color(0.34756f,	0.04776f,	-0.00038f);
        colorArray[23] = new Color(0.33971f,	0.03557f,	-0.00030f);
        //colorArray[0] = new Color(0.32265f,	0.02583f,	-0.00022f);
        colorArray[24] = new Color(0.29708f,	0.01828f,	-0.00015f);
        //colorArray[0] = new Color(0.26348f,	0.01253f,	-0.00011f);
        colorArray[25] = new Color(0.22677f,	0.00833f,	-0.00008f);
        //colorArray[0] = new Color(0.19233f,	0.00537f,	-0.00005f);
        colorArray[26] = new Color(0.15968f,	0.00334f,	-0.00003f);
        //colorArray[0] = new Color(0.12905f,	0.00199f,	-0.00002f);

        colorArray[27] = new Color(0.10167f,	0.00116f,	-0.00001f); // 650
        //colorArray[0] = new Color(0.07857f,	0.00066f,	-0.00001f);
        colorArray[28] = new Color(0.05932f,	0.00037f,	0.00000f);
        //colorArray[0] = new Color(0.04366f,	0.00021f,	0.00000f);
        colorArray[29] = new Color(0.03149f,	0.00011f,	0.00000f);
        //colorArray[0] = new Color(0.02294f,	0.00006f,	0.00000f);
        colorArray[30] = new Color(0.01687f,	0.00003f,	0.00000f);
        //colorArray[0] = new Color(0.01187f,	0.00001f,	0.00000f);
        colorArray[31] = new Color(0.00819f,	0.00000f,	0.00000f);
        //colorArray[0] = new Color(0.00572f,	0.00000f,	0.00000f);

        colorArray[32] = new Color(0.00410f,	0.00000f,	0.00000f);  // 700
        //colorArray[0] = new Color(0.00291f,	0.00000f,	0.00000f);
        colorArray[33] = new Color(0.00210f,	0.00000f,	0.00000f);
        //colorArray[0] = new Color(0.00148f,	0.00000f,	0.00000f);
        colorArray[34] = new Color(0.00105f,	0.00000f,	0.00000f);
        //colorArray[0] = new Color(0.00074f,	0.00000f,	0.00000f);
        colorArray[35] = new Color(0.00052f,	0.00000f,	0.00000f);
        //colorArray[0] = new Color(0.00036f,	0.00000f,	0.00000f);
        colorArray[36] = new Color(0.00025f,	0.00000f,	0.00000f);
        //colorArray[0] = new Color(0.00017f,	0.00000f,	0.00000f);
        colorArray[37] = new Color(0.00012f,	0.00000f,	0.00000f);
        //colorArray[0] = new Color(0.00008f,	0.00000f,	0.00000f);
        colorArray[38] = new Color(0.00006f,	0.00000f,	0.00000f);
        //colorArray[0] = new Color(0.00004f,	0.00000f,	0.00000f);
        colorArray[39] = new Color(0.00003f,	0.00000f,	0.00000f);
        //colorArray[0] = new Color(0.00001f,	0.00000f,	0.00000f);
        //colorArray[0] = new Color(0.00000f,	0.00000f,	0.00000f);

        ColorMatchingFunction.SetPixels(colorArray);
        ColorMatchingFunction.Apply();
        meshRenderer.material.SetTexture("_CMF", ColorMatchingFunction);
    }

    float[,] VectoSymMat(float[] vec)
    {

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
    void Visualization(int key)
    {
        if (key == mode)
        {
            mode = 0;
            meshRenderer.material = material;
            foreach (GameObject sphere in spheres)
            {
                sphere.SetActive(false);
            }
            foreach (GameObject arrow in arrows)
            {
                arrow.SetActive(false);
            }
        }
        else if (key == 1)
        {
            mode = 1;
            meshRenderer.material = submat;
            foreach (GameObject sphere in spheres)
            {
                sphere.SetActive(true);
            }
            foreach (GameObject arrow in arrows)
            {
                arrow.SetActive(false);
            }
        }
        else if (key == 2)
        {
            mode = 2;
            meshRenderer.material = submat;
            foreach (GameObject sphere in spheres)
            {
                sphere.SetActive(false);
            }
            foreach (GameObject arrow in arrows)
            {
                arrow.SetActive(true);
            }
        }
        else {
            mode = 3;
            meshRenderer.material = binarymat;
            foreach (GameObject sphere in spheres)
            {
                sphere.SetActive(false);
            }
            foreach (GameObject arrow in arrows)
            {
                arrow.SetActive(false);
            }
        }
    }
    void Polarizer(int key)
    {
        polarizer = key;
    }
    void CreateMesh(Mesh mesh, Vector3[] vertices, Vector2[] uvs, int[] triangles)
    {
        //　最初にメッシュをクリアする
        mesh.Clear();
        //　頂点の設定
        mesh.vertices = vertices;
        //　テクスチャのUV座標設定
        mesh.uv = mode == 1 ? uvs : uvs2;
        //　三角形メッシュの設定
        mesh.triangles = triangles;

        mesh.colors = colors;
        //　Boundsの再計算
        mesh.RecalculateBounds();
        //　NormalMapの再計算
        mesh.RecalculateNormals();
    }
    void Decoration()
    {
        decos[0].GetComponent<Transform>().position = vertices[size - 1] - new Vector3(0, 0.1f, 0);
        decos[1].GetComponent<Transform>().position = vertices[size * (size2 - 1)] - new Vector3(0.1f, 0, 0);
        decos[2].GetComponent<Transform>().position = vertices[0] - new Vector3(0, 0.1f, 0);
        decos[3].GetComponent<Transform>().position = vertices[vertices.Length - 1];
    }
}

