using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FEM4 : MonoBehaviour
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
    public List<Vector3> g1;
    public List<int> S2;
    public List<Vector3> g2;

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
                if (R_Array[i, j] - R_Array[j, i] > 0.0001f) Debug.Log("R_Array is not symetry:" + R_Array[i, j] + " & " + R_Array[j, i]);
            }
        }

        SetVector();
        Solve();

        ///*
        // Debug.lineRenderer
        var lineRenderer = gameObject.AddComponent<LineRenderer>();
        var positions = new Vector3[] {
            HES.TList[150].HE.Vert.Pos,
            HES.TList[150].HE.Next.Vert.Pos,
            HES.TList[150].HE.Prev.Vert.Pos,
            HES.TList[150].HE.Vert.Pos
        };
        
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.positionCount = positions.Length;
        lineRenderer.startWidth = 0.01f;
        lineRenderer.endWidth = 0.01f;
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;
        for (int i = 0; i < HES.TList.Count; i++)
        {
            Vector3 V = HES.TList[i].HE.Next.Vert.Pos - HES.TList[i].HE.Vert.Pos;
            Vector3 U = HES.TList[i].HE.Prev.Vert.Pos - HES.TList[i].HE.Vert.Pos;
            if ((Vector3.Cross(V, U)).magnitude < 0.1f)
            {
                positions[0] = HES.TList[i].HE.Vert.Pos;
                positions[1] = HES.TList[i].HE.Next.Vert.Pos;
                positions[2] = HES.TList[i].HE.Prev.Vert.Pos;
                positions[3] = HES.TList[i].HE.Vert.Pos;

                Debug.Log("Small triangle: " + i);
                Debug.Log("Small triangle index: " + HES.TList[i].HE.Vert.Idx + ", " + HES.TList[i].HE.Next.Vert.Idx + ", " + HES.TList[i].HE.Prev.Vert.Idx);
                break;
            }

        }
        lineRenderer.SetPositions(positions);
        Solver.PrintVector(S1.ToArray());
        
        //*/
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
        Array = new float[HES.VList.Count * 3, HES.VList.Count * 3];
        Fvector = new float[HES.VList.Count * 3];
        F2vector = new float[HES.VList.Count * 3];

        for (int i = 0; i < HES.VList.Count * 3; i++)
        {
            Fvector[i] = 0;
            F2vector[i] = 0;
            for (int j = 0; j < HES.VList.Count * 3; j++)
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
                g1.Add(HES.VList[i].B_vector);
                count--;
            }
            else if (HES.VList[i].Bound == 2)
            {
                S2.Add(i);
                g2.Add(HES.VList[i].B_vector);
                //g2.Add(j == 0 ? 0.5f : -0.5f);
            }
        }
        R_Array = new float[count * 3, count * 3];
        R_Fvector = new float[count * 3];
        R_F2vector = new float[count * 3];
        R_index = new int[count];
        values = new float[count * 3];
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

                R_Array[3 * x + 0, 3 * y + 0] = Array[3 * i + 0, 3 * j + 0];        // x, y はS1境界のインデックスを飛ばしてナンバリングされたもの  (このループ上で空回りしているインデックス)         
                R_Array[3 * x + 0, 3 * y + 1] = Array[3 * i + 0, 3 * j + 1];
                R_Array[3 * x + 0, 3 * y + 2] = Array[3 * i + 0, 3 * j + 2];
                R_Array[3 * x + 1, 3 * y + 0] = Array[3 * i + 1, 3 * j + 0];
                R_Array[3 * x + 1, 3 * y + 1] = Array[3 * i + 1, 3 * j + 1];
                R_Array[3 * x + 1, 3 * y + 2] = Array[3 * i + 1, 3 * j + 2];
                R_Array[3 * x + 2, 3 * y + 0] = Array[3 * i + 2, 3 * j + 0];
                R_Array[3 * x + 2, 3 * y + 1] = Array[3 * i + 2, 3 * j + 1];
                R_Array[3 * x + 2, 3 * y + 2] = Array[3 * i + 2, 3 * j + 2];

                y++;
            }

            R_Fvector[3 * x + 0] = Fvector[3 * i + 0];
            R_Fvector[3 * x + 1] = Fvector[3 * i + 1];
            R_Fvector[3 * x + 2] = Fvector[3 * i + 2];

            R_F2vector[3 * x + 0] = F2vector[3 * i + 0];
            R_F2vector[3 * x + 1] = F2vector[3 * i + 1];
            R_F2vector[3 * x + 2] = F2vector[3 * i + 2];

            values[3 * x + 0] = 0f;
            values[3 * x + 1] = 0f;
            values[3 * x + 2] = 0f;

            R_index[x] = i;                         // x, y の番号の飛び方は一緒なので片方だけ記録すればOK
            x++;
        }

        Debug.Log("x: " + x);
        Debug.Log("V.count: " + HES.VList.Count);
        Debug.Log("S1: " + S1.Count);
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

            Vector3 Q1 = HES.VList[i1].Pos * 1f;
            Vector3 Q2 = HES.VList[i2].Pos * 1f;
            Vector3 Q3 = HES.VList[i3].Pos * 1f;

            Q2 -= Q1;
            Q3 -= Q1;
            Q1 *= 0;

            float t = Vector3.Dot(Q2, Q3.normalized);
            Vector2 E1 = new Vector2(Q3.x, Q3.y).normalized;                              // 変換後の基底ベクトル　z軸方向は無視 
            Vector2 E2 = new Vector2(E1.y, -E1.x);

            Vector2 P1 = new Vector2(0, 0);                                         // 数学的解説は右手座標系が多いので、三角形を反転させてそちらに合わせる
            Vector2 P3 = new Vector2(t, (Q2 - t * Q3.normalized).magnitude);        // 左手座標系で考えると、他の文献と正負が逆転してややこしい
            Vector2 P2 = new Vector2(Q3.magnitude, 0);

            
            float area = P3.y * P2.x * 0.5f;

            float a1 = 0.5f * (P2.x * P3.y - P3.x * P2.y) / area;  // 形状関数: L_i = a_i + b_i * x + c_i * y;
            float a2 = 0.5f * (P3.x * P1.y - P1.x * P3.y) / area;
            float a3 = 0.5f * (P1.x * P2.y - P2.x * P1.y) / area;

            float b1 = 0.5f * (P3.y - P2.y) / area;
            float b2 = 0.5f * (P1.y - P3.y) / area;
            float b3 = 0.5f * (P2.y - P1.y) / area;

            float c1 = 0.5f * (P2.x - P3.x) / area;
            float c2 = 0.5f * (P3.x - P1.x) / area;
            float c3 = 0.5f * (P1.x - P2.x) / area;
            ///*/
            ////
            Vector2 m1 = (P2 + P3) * 0.5f;
            Vector2 m2 = (P3 + P1) * 0.5f;
            Vector2 m3 = (P1 + P2) * 0.5f;
            float L11 = a1 + b1 * m1.x + c1 * m1.y; // 形状関数マトリクス: L_ij = a_i + b_i * m_j.x + c_i * m_j.y;
            float L12 = a1 + b1 * m2.x + c1 * m2.y;
            float L13 = a1 + b1 * m3.x + c1 * m3.y;
            float L21 = a2 + b2 * m1.x + c2 * m1.y;
            float L22 = a2 + b2 * m2.x + c2 * m2.y;
            float L23 = a2 + b2 * m3.x + c2 * m3.y;
            float L31 = a3 + b3 * m1.x + c3 * m1.y;
            float L32 = a3 + b3 * m2.x + c3 * m2.y;
            float L33 = a3 + b3 * m3.x + c3 * m3.y;

            /*
            float N11 = L11 * (2 * L11 - 1); // 内挿関数マトリクス: Nは6種類×点は3つ;
            float N12 = L12 * (2 * L12 - 1);
            float N13 = L13 * (2 * L13 - 1);
            float N21 = L21 * (2 * L21 - 1);
            float N22 = L22 * (2 * L22 - 1);
            float N23 = L23 * (2 * L23 - 1);
            float N31 = L31 * (2 * L31 - 1);
            float N32 = L32 * (2 * L32 - 1);
            float N33 = L33 * (2 * L33 - 1);
            float N41 = L21 * L31 * 4;
            float N42 = L22 * L32 * 4;
            float N43 = L23 * L33 * 4;
            float N51 = L31 * L11 * 4;
            float N52 = L32 * L12 * 4;
            float N53 = L33 * L13 * 4;
            float N61 = L11 * L21 * 4;
            float N62 = L12 * L22 * 4;
            float N63 = L13 * L23 * 4;
            */

            Vector2 V23 = P3 - P2;              // V_ij は「iからj」のベクトル
            Vector2 V31 = P1 - P3;
            Vector2 V12 = P2 - P1;
            float sqrl23 = V23.sqrMagnitude;
            float sqrl31 = V31.sqrMagnitude;
            float sqrl12 = V12.sqrMagnitude;

            float p1 = -6 * V23.x / sqrl23;
            float p2 = -6 * V31.x / sqrl31;
            float p3 = -6 * V12.x / sqrl12;
            float q1 =  3 * V23.x * V23.y / sqrl23;
            float q2 =  3 * V31.x * V31.y / sqrl31;
            float q3 =  3 * V12.x * V12.y / sqrl12;
            float r1 =  3 * V23.y * V23.y / sqrl23;
            float r2 =  3 * V31.y * V31.y / sqrl31;
            float r3 =  3 * V12.y * V12.y / sqrl12;
            float t1 = -6 * V23.y / sqrl23;
            float t2 = -6 * V31.y / sqrl31;
            float t3 = -6 * V12.y / sqrl12;

            /*
            float Ld21 = L2 - L1;
            float Ld31 = L3 - L1;
            float[] HxL2 = {
                - Ld21 * t3 + L3 * t2,
                + Ld21 * t3 + L3 * t1,
                - L3 * (t1 + t2),
                - 1 + Ld21 * r3 + L3 * r2,
                - Ld21 * q3 - L3 * q2,
                + 1 + Ld21 * r3 - L3 * r1,
                - Ld21 * q3 + L3 * q1,
                - L3 * (r1 - r2),
                + L3 * (q1 - q2)
            };
            float[] HxL3 = {
                + Ld31 * t2 - L2 * t3,
                + L2 * (t1 + t3),
                - Ld31 * t2 - L2 * t1,
                - 1 + Ld31 * r2 + L2 * r3,
                - Ld31 * q2 - L2 * q3,
                - L2 * (r1 - r3),
                + L2 * (q1 - q3),
                + 1 + Ld31 * r2 - L2 * r1,
                - Ld31 * q2 + L2 * q1
            };
            float[] HyL2 = {
                + Ld21 * p3 - L3 * p2,
                - Ld21 * p3 - L3 * p1,
                + L3 * (p1 + p2),
                - Ld21 * q3 - L3 * q2,
                + 2 - 6 * L1 - Ld21 * r3 - L3 * r2,
                - Ld21 * q3 + L3 * q1,
                - 2 + 6 * L2 - Ld21 * r3 + L3 * r1,
                + L3 * (q1 - q2),
                + L3 * (r1 - r2)
            };
            float[] HyL3 = {
                - Ld31 * p2 + L2 * p3,
                - L2 * (p1 + p3),
                + Ld31 * p2 + L2 * p1,
                - Ld31 * q2 - L2 * q3,
                + 2 - 6 * L1 - Ld31 * r2 - L2 * r3,
                + L2 * (q1 - q3),
                + L2 * (r1 - r3),
                - Ld31 * q2 + L2 * q1,
                - 2 + 6 * L3 - Ld31 * r2 + L2 * r1
            };*/

            float Ld211 = L21 - L11;
            float Ld311 = L31 - L11;
            float[] HxL21 = {
                - Ld211 * t3 + L31 * t2,
                + Ld211 * t3 + L31 * t1,
                - L31 * (t1 + t2),
                - 1 + Ld211 * r3 + L31 * r2,
                - Ld211 * q3 - L31 * q2,
                + 1 + Ld211 * r3 - L31 * r1,
                - Ld211 * q3 + L31 * q1,
                - L31 * (r1 - r2),
                + L31 * (q1 - q2)
            };
            float[] HxL31 = {
                + Ld311 * t2 - L21 * t3,
                + L21 * (t1 + t3),
                - Ld311 * t2 - L21 * t1,
                - 1 + Ld311 * r2 + L21 * r3,
                - Ld311 * q2 - L21 * q3,
                - L21 * (r1 - r3),
                + L21 * (q1 - q3),
                + 1 + Ld311 * r2 - L21 * r1,
                - Ld311 * q2 + L21 * q1
            };
            float[] HyL21 = {
                + Ld211 * p3 - L31 * p2,
                - Ld211 * p3 - L31 * p1,
                + L31 * (p1 + p2),
                - Ld211 * q3 - L31 * q2,
                + 2 - 6 * L11 - Ld211 * r3 - L31 * r2,
                - Ld211 * q3 + L31 * q1,
                - 2 + 6 * L21 - Ld211 * r3 + L31 * r1,
                + L31 * (q1 - q2),
                + L31 * (r1 - r2)
            };
            float[] HyL31 = {
                - Ld311 * p2 + L21 * p3,
                - L21 * (p1 + p3),
                + Ld311 * p2 + L21 * p1,
                - Ld311 * q2 - L21 * q3,
                + 2 - 6 * L11 - Ld311 * r2 - L21 * r3,
                + L21 * (q1 - q3),
                + L21 * (r1 - r3),
                - Ld311 * q2 + L21 * q1,
                - 2 + 6 * L31 - Ld311 * r2 + L21 * r1
            };

            float Ld212 = L22 - L12;
            float Ld312 = L32 - L12;
            float[] HxL22 = {
                - Ld212 * t3 + L32 * t2,
                + Ld212 * t3 + L32 * t1,
                - L32 * (t1 + t2),
                - 1 + Ld212 * r3 + L32 * r2,
                - Ld212 * q3 - L32 * q2,
                + 1 + Ld212 * r3 - L32 * r1,
                - Ld212 * q3 + L32 * q1,
                - L32 * (r1 - r2),
                + L32 * (q1 - q2)
            };
            float[] HxL32 = {
                + Ld312 * t2 - L22 * t3,
                + L22 * (t1 + t3),
                - Ld312 * t2 - L22 * t1,
                - 1 + Ld312 * r2 + L22 * r3,
                - Ld312 * q2 - L22 * q3,
                - L22 * (r1 - r3),
                + L22 * (q1 - q3),
                + 1 + Ld312 * r2 - L22 * r1,
                - Ld312 * q2 + L22 * q1
            };
            float[] HyL22 = {
                + Ld212 * p3 - L32 * p2,
                - Ld212 * p3 - L32 * p1,
                + L32 * (p1 + p2),
                - Ld212 * q3 - L32 * q2,
                + 2 - 6 * L12 - Ld212 * r3 - L32 * r2,
                - Ld212 * q3 + L32 * q1,
                - 2 + 6 * L22 - Ld212 * r3 + L32 * r1,
                + L32 * (q1 - q2),
                + L32 * (r1 - r2)
            };
            float[] HyL32 = {
                - Ld312 * p2 + L22 * p3,
                - L22 * (p1 + p3),
                + Ld312 * p2 + L22 * p1,
                - Ld312 * q2 - L22 * q3,
                + 2 - 6 * L12 - Ld312 * r2 - L22 * r3,
                + L22 * (q1 - q3),
                + L22 * (r1 - r3),
                - Ld312 * q2 + L22 * q1,
                - 2 + 6 * L32 - Ld312 * r2 + L22 * r1
            };

            float Ld213 = L23 - L13;
            float Ld313 = L33 - L13;
            float[] HxL23 = {
                - Ld213 * t3 + L33 * t2,
                + Ld213 * t3 + L33 * t1,
                - L33 * (t1 + t2),
                - 1 + Ld213 * r3 + L33 * r2,
                - Ld213 * q3 - L33 * q2,
                + 1 + Ld213 * r3 - L33 * r1,
                - Ld213 * q3 + L33 * q1,
                - L33 * (r1 - r2),
                + L33 * (q1 - q2)
            };
            float[] HxL33 = {
                + Ld313 * t2 - L23 * t3,
                + L23 * (t1 + t3),
                - Ld313 * t2 - L23 * t1,
                - 1 + Ld313 * r2 + L23 * r3,
                - Ld313 * q2 - L23 * q3,
                - L23 * (r1 - r3),
                + L23 * (q1 - q3),
                + 1 + Ld313 * r2 - L23 * r1,
                - Ld313 * q2 + L23 * q1
            };
            float[] HyL23 = {
                + Ld213 * p3 - L33 * p2,
                - Ld213 * p3 - L33 * p1,
                + L33 * (p1 + p2),
                - Ld213 * q3 - L33 * q2,
                + 2 - 6 * L13 - Ld213 * r3 - L33 * r2,
                - Ld213 * q3 + L33 * q1,
                - 2 + 6 * L23 - Ld213 * r3 + L33 * r1,
                + L33 * (q1 - q2),
                + L33 * (r1 - r2)
            };
            float[] HyL33 = {
                - Ld313 * p2 + L23 * p3,
                - L23 * (p1 + p3),
                + Ld313 * p2 + L23 * p1,
                - Ld313 * q2 - L23 * q3,
                + 2 - 6 * L13 - Ld313 * r2 - L23 * r3,
                + L23 * (q1 - q3),
                + L23 * (r1 - r3),
                - Ld313 * q2 + L23 * q1,
                - 2 + 6 * L33 - Ld313 * r2 + L23 * r1
            };

            float E = 20.0f;
            float thin = 0.005f;
            float nu = 0.0f;
            float D = E * thin * thin * thin / (12 * (1 - nu * nu));
            float[,] Db = { {D, D * nu, 0}, { D * nu, D, 0 }, { 0, 0, D * (1 - nu) / 2f}};


            float[,] tmpM = new float[3, 9];
            float[,] B = new float[3, 9];
            // 辺1の中点での値
            float[,] K = new float[9, 9];

            for (int j = 0; j < 9; j++) {
                B[0, j] = + b2 * HyL21[j] + b3 * HyL31[j];
                B[1, j] = - c2 * HxL21[j] - c3 * HxL31[j];
                B[2, j] = + c2 * HyL21[j] + c3 * HyL31[j] - b2 * HxL21[j] - b3 * HxL31[j];
            } 
            Solver.MatxMat(Db, B, tmpM);
            Solver.MatTxMat(B, tmpM, K);

            // 辺1の中点での値
            float[,] K2 = new float[9, 9];

            for (int j = 0; j < 9; j++)
            {
                B[0, j] = + b2 * HyL22[j] + b3 * HyL32[j];
                B[1, j] = - c2 * HxL22[j] - c3 * HxL32[j];
                B[2, j] = + c2 * HyL22[j] + c3 * HyL32[j] - b2 * HxL22[j] - b3 * HxL32[j];
            }
            Solver.MatxMat(Db, B, tmpM);
            Solver.MatTxMat(B, tmpM, K2);

            // 辺1の中点での値
            float[,] K3 = new float[9, 9];

            for (int j = 0; j < 9; j++)
            {
                B[0, j] = + b2 * HyL23[j] + b3 * HyL33[j];
                B[1, j] = - c2 * HxL23[j] - c3 * HxL33[j];
                B[2, j] = + c2 * HyL23[j] + c3 * HyL33[j] - b2 * HxL23[j] - b3 * HxL33[j];
            }
            Solver.MatxMat(Db, B, tmpM);
            Solver.MatTxMat(B, tmpM, K3);

            float[,] T = new float[9, 9];
            float[,] Kg = new float[9, 9];
            for (int j = 0; j < 9; j++)
            {
                for (int k = 0; k < 9; k++)
                {
                    if (j < 3 && k < 3)
                    {
                        if (j == k) T[j, k] = 1;
                        else T[j, k] = 0;
                    }
                    else if ((j - 3) / 2 == (k - 3) / 2 && j >= 3 && k >= 3)
                    {
                        if (j == k && j % 2 == 0) T[j, k] = E1.x;
                        if (j < k) T[j, k] = E2.x;
                        if (j > k) T[j, k] = E1.y;
                        if (j == k && j % 2 == 1) T[j, k] = E2.y;
                    }
                    else
                    {
                        T[j, k] = 0;
                    }
                }
            }

            for (int j = 0; j < 9; j++)
            {
                for (int k = 0; k < 9; k++)
                {
                    K[j, k] += K2[j, k] + K3[j, k];
                    K[j, k] *= area / 3;
                }
            }

            if (i == 150) Solver.PrintArray(T);
            Solver.MatTxMat(T, K, K2);
            Solver.MatxMat(K2, T, Kg);


            if (i == 150) Solver.PrintArray(Kg);

            for (int j = 0; j < 9; j++) // j, kについて，0 -> w, 1 -> θx, 2 -> θy  
            {
                for (int k = 0; k < 9; k++)
                {
                    int x = 0, y = 0;
                    switch (j) {
                        case 0:     x = i1 * 3 + 0;     break;
                        case 1:     x = i2 * 3 + 0;     break;
                        case 2:     x = i3 * 3 + 0;     break;
                        case 3:     x = i1 * 3 + 1;     break;
                        case 4:     x = i1 * 3 + 2;     break;
                        case 5:     x = i2 * 3 + 1;     break;
                        case 6:     x = i2 * 3 + 2;     break;
                        case 7:     x = i3 * 3 + 1;     break;
                        default:    x = i3 * 3 + 2;     break;
                    }
                    switch (k)
                    {
                        case 0:     y = i1 * 3 + 0;     break;
                        case 1:     y = i2 * 3 + 0;     break;
                        case 2:     y = i3 * 3 + 0;     break;
                        case 3:     y = i1 * 3 + 1;     break;
                        case 4:     y = i1 * 3 + 2;     break;
                        case 5:     y = i2 * 3 + 1;     break;
                        case 6:     y = i2 * 3 + 2;     break;
                        case 7:     y = i3 * 3 + 1;     break;
                        default:    y = i3 * 3 + 2;     break;
                    }

                    Array[x, y] += Kg[j, k];
                }
            }
            if (i == 15) {
                Solver.PrintArray(K);
                Debug.Log(E1.magnitude);
            }

            // 第二境界条件の処理，本来は角の要素について，二頂点がS2上にあってもそれを結ぶ辺が境界にならないがことがあるが，今回はないので無視する
            int x1 = S2.FindIndex(n => n == i1);
            int x2 = S2.FindIndex(n => n == i2);
            int x3 = S2.FindIndex(n => n == i3);

            if (x1 >= 0 && x2 >= 0)             // a,b,c の2, 3はi, x の3, 2にそれぞれ対応する！！！
            {
                float L = (P1 - P2).magnitude;
                F2vector[i1 * 3 + 0] += g2[x1].x * L * (a1 + b1 * (P1.x + P2.x) / 2 + c1 * (P1.y + P2.y) / 2);
                F2vector[i1 * 3 + 1] += g2[x1].y * L * (a1 + b1 * (P1.x + P2.x) / 2 + c1 * (P1.y + P2.y) / 2);
                F2vector[i1 * 3 + 2] += g2[x1].z * L * (a1 + b1 * (P1.x + P2.x) / 2 + c1 * (P1.y + P2.y) / 2);
                F2vector[i2 * 3 + 0] += g2[x2].x * L * (a3 + b3 * (P1.x + P2.x) / 2 + c3 * (P1.y + P2.y) / 2);
                F2vector[i2 * 3 + 1] += g2[x2].y * L * (a3 + b3 * (P1.x + P2.x) / 2 + c3 * (P1.y + P2.y) / 2);
                F2vector[i2 * 3 + 2] += g2[x2].z * L * (a3 + b3 * (P1.x + P2.x) / 2 + c3 * (P1.y + P2.y) / 2);
            }
            if (x2 >= 0 && x3 >= 0)
            {
                float L = (P2 - P3).magnitude;
                F2vector[i2 * 3 + 0] += g2[x2].x * L * (a3 + b3 * (P2.x + P3.x) / 2 + c3 * (P2.y + P3.y) / 2);
                F2vector[i2 * 3 + 1] += g2[x2].y * L * (a3 + b3 * (P2.x + P3.x) / 2 + c3 * (P2.y + P3.y) / 2);
                F2vector[i2 * 3 + 2] += g2[x2].z * L * (a3 + b3 * (P2.x + P3.x) / 2 + c3 * (P2.y + P3.y) / 2);
                F2vector[i3 * 3 + 0] += g2[x3].x * L * (a2 + b2 * (P2.x + P3.x) / 2 + c2 * (P2.y + P3.y) / 2);
                F2vector[i3 * 3 + 1] += g2[x3].y * L * (a2 + b2 * (P2.x + P3.x) / 2 + c2 * (P2.y + P3.y) / 2);
                F2vector[i3 * 3 + 2] += g2[x3].z * L * (a2 + b2 * (P2.x + P3.x) / 2 + c2 * (P2.y + P3.y) / 2);
            }
            if (x3 >= 0 && x1 >= 0)
            {
                float L = (P3 - P1).magnitude;
                F2vector[i3 * 3 + 0] += g2[x3].x * L * (a2 + b2 * (P3.x + P1.x) / 2 + c2 * (P3.y + P1.y) / 2);
                F2vector[i3 * 3 + 1] += g2[x3].y * L * (a2 + b2 * (P3.x + P1.x) / 2 + c2 * (P3.y + P1.y) / 2);
                F2vector[i3 * 3 + 2] += g2[x3].z * L * (a2 + b2 * (P3.x + P1.x) / 2 + c2 * (P3.y + P1.y) / 2);
                F2vector[i1 * 3 + 0] += g2[x1].x * L * (a1 + b1 * (P3.x + P1.x) / 2 + c1 * (P3.y + P1.y) / 2);
                F2vector[i1 * 3 + 1] += g2[x1].y * L * (a1 + b1 * (P3.x + P1.x) / 2 + c1 * (P3.y + P1.y) / 2);
                F2vector[i1 * 3 + 2] += g2[x1].z * L * (a1 + b1 * (P3.x + P1.x) / 2 + c1 * (P3.y + P1.y) / 2);
            }

            if (i1 == 38) {
                Debug.Log("test");
            }
            if (i2 == 38)
            {
                Debug.Log("test");
            }
            if (i3 == 38)
            {
                Debug.Log("test");
            }
        }
    }

    void SetVector()
    {
        for (int i = 0; i < R_Fvector.Length / 3; i++)
        {
            R_Fvector[3 * i + 0] = R_F2vector[3 * i + 0];
            R_Fvector[3 * i + 1] = R_F2vector[3 * i + 1];
            R_Fvector[3 * i + 2] = R_F2vector[3 * i + 2];

            for (int j = 0; j < S1.Count; j++)
            {
                R_Fvector[i * 3 + 0] -= Array[R_index[i] * 3 + 0, S1[j] * 3 + 0] * g1[j].x;
                R_Fvector[i * 3 + 0] -= Array[R_index[i] * 3 + 0, S1[j] * 3 + 1] * g1[j].y;
                R_Fvector[i * 3 + 0] -= Array[R_index[i] * 3 + 0, S1[j] * 3 + 2] * g1[j].z;
                R_Fvector[i * 3 + 1] -= Array[R_index[i] * 3 + 1, S1[j] * 3 + 0] * g1[j].x;
                R_Fvector[i * 3 + 1] -= Array[R_index[i] * 3 + 1, S1[j] * 3 + 1] * g1[j].y;
                R_Fvector[i * 3 + 1] -= Array[R_index[i] * 3 + 1, S1[j] * 3 + 2] * g1[j].z;
                R_Fvector[i * 3 + 2] -= Array[R_index[i] * 3 + 2, S1[j] * 3 + 0] * g1[j].x;
                R_Fvector[i * 3 + 2] -= Array[R_index[i] * 3 + 2, S1[j] * 3 + 1] * g1[j].y;
                R_Fvector[i * 3 + 2] -= Array[R_index[i] * 3 + 2, S1[j] * 3 + 2] * g1[j].z;

            }
        }
    }

    void UpdataBoundary()
    {
        for (int i = 0; i < g1.Count; i++)
        {
            Vector3 V = HES.VList[S1[i]].Pos;
            //g1[i] = y == 0 ? Mathf.Sin(0.1f + Time.time) * ratio * 0.4f + Mathf.Sin((x + y) * 0.5f + Time.time) * ratio * 0.6f : -2f;
            //g1[i] = new Vector3(0, 0, (1 + Mathf.Sin(0.1f + Time.time + V.x + V.y)) * ratio * 1f);
            g1[i] = new Vector3(V.x * 1f * Mathf.Sin(0.1f + Time.time), 0, 0);
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

        //float delta = 0.1f;
        //int oddpoint = 0;
        for (int i = 0; i < values.Length / 3; i++)
        {
            Vector3 deltaV = new Vector3(-values[i * 3 + 2] * values[i * 3 + 0], values[i * 3 + 1] * values[i * 3 + 0], values[i * 3 + 0]);
            Vector3 TestVec = new Vector3(values[i * 3 + 0], values[i * 3 + 1], values[i * 3 + 2]);
            vertices[R_index[i]] = vertices0[R_index[i]] + deltaV;
            uvs2[R_index[i]].x = Mathf.Clamp(TestVec.z * 1.3f + 0.0f, 0.01f, 1f - 0.01f);
            //uvs2[R_index[i]].x = Mathf.Sin(Time.time) * 0.5f * 0.5f;
            //if (deltaV.z > delta) { delta = deltaV.magnitude; oddpoint = i; }  
        }
        //Debug.Log("Oddpoint: " + R_index[oddpoint]);
        //Debug.Log(vertices[R_index[oddpoint]]);
        for (int i = 0; i < S1.Count; i++)
        {
            vertices[S1[i]] = vertices0[S1[i]] + new Vector3(-g1[i].z * g1[i].x, g1[i].y * g1[i].x, g1[i].x);
            uvs2[S1[i]].x = Mathf.Clamp(g1[i].x * 1.3f + 0.0f, 0.01f, 1f - 0.01f);
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

