using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Hoge0 : MonoBehaviour
{
    public ComputeShader shader;
    //　頂点配列
    public Vector3[] vertices;
    //　UV配列
    public Vector2[] uvs;
    //　三角形の順番配列
    public int[] triangles;
    private Mesh mesh;
    //　メッシュ表示コンポーネント
    private MeshRenderer meshRenderer;
    //　メッシュに設定するマテリアル
    public Material material;

    private int size = 128;
    private int divide = 4; // 256 / 8 = "32"

    //int p = 32; //並列数
    //int N = 262144; // = 2^18
    float timestep;
    float velocity = 0.5f;

    public float[] host_u_pre;
    public float[] host_u_now;
    public float[] host_u_nex;

    public ComputeBuffer u_pre;
    public ComputeBuffer u_now;
    public ComputeBuffer u_nex;

    // Start is called before the first frame update
    void Start()
    {
        FixParameters();
        InitBuffer();
        InitMesh();
    }

    // Update is called once per frame
    void Update()
    {
        CreateMesh(mesh, vertices, uvs, triangles);
        Simulate();
    }
    void FixParameters()
    {
        timestep = Time.deltaTime;
    }
    void InitBuffer()
    {
        host_u_pre = new float[size * size];
        host_u_now = new float[size * size];
        host_u_nex = new float[size * size];

        u_pre = new ComputeBuffer(host_u_pre.Length, sizeof(float));
        u_now = new ComputeBuffer(host_u_now.Length, sizeof(float));
        u_nex = new ComputeBuffer(host_u_nex.Length, sizeof(float));

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                host_u_pre[i * size + j] = 0f;
                host_u_now[i * size + j] = 0f;
                host_u_nex[i * size + j] = 0f;
            }
        }

        int x = 20;
        int y = 20;
        float z = 3f;
        int range = 5;

        for (int i = - range; i < range; i++)
        {
            for (int j = - range; j < range; j++)
            {
                host_u_pre[(size / 2 + y + i) * size + size / 2 + x + j] = z / (i == 0 && j == 0 ? 1 : Mathf.Sqrt(i * i + j * j)) ;
                host_u_now[(size / 2 + y + i) * size + size / 2 + x + j] = z / (i == 0 && j == 0 ? 1 : Mathf.Sqrt(i * i + j * j)) ;
            }
        }

        x = -20;
        y = -20;
        z = 0.5f;
        range = 5;
        for (int i = -range; i < range; i++)
        {
            for (int j = -range; j < range; j++)
            {
                host_u_pre[(size / 2 + y + i) * size + size / 2 + x + j] = z / (i == 0 && j == 0 ? 1 : Mathf.Sqrt(i * i + j * j));
                host_u_now[(size / 2 + y + i) * size + size / 2 + x + j] = z / (i == 0 && j == 0 ? 1 : Mathf.Sqrt(i * i + j * j));
            }
        }

        /*
        host_u_pre[(size2 / 2 + 1) * size2 + size2 / 2] = 0.001f;
        host_u_now[(size2 / 2 + 1) * size2 + size2 / 2] = 0.001f;
        host_u_pre[(size2 / 2) * size2 + size2 / 2 + 1] = 0.001f;
        host_u_now[(size2 / 2) * size2 + size2 / 2 + 1] = 0.001f;
        host_u_pre[(size2 / 2 + 1) * size2 + size2 / 2 + 1] = 0.001f;
        host_u_now[(size2 / 2 + 1) * size2 + size2 / 2 + 1] = 0.001f;
        */

        u_pre.SetData(host_u_pre);
        u_now.SetData(host_u_now);
    }

    void InitMesh() {
        vertices = new Vector3[size * size];
        uvs = new Vector2[size * size];
        triangles = new int[(size - 1) * (size - 1) * 6];

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                vertices[i * size + j] = new Vector3(j * 10f / size - 5f, 0, i * 10f / size - 5f);
                uvs[i * size + j] = new Vector2((float)j / size, (float)i / size );

                if (i < size - 1 && j < size - 1) {
                    triangles[(i * (size - 1) + j) * 6] = i * size + j;
                    triangles[(i * (size - 1) + j) * 6 + 1] = (i + 1) * size + (j + 1);
                    triangles[(i * (size - 1) + j) * 6 + 2] = i * size + (j + 1);
                    triangles[(i * (size - 1) + j) * 6 + 3] = i * size + j;
                    triangles[(i * (size - 1) + j) * 6 + 4] = (i + 1) * size + j;
                    triangles[(i * (size - 1) + j) * 6 + 5] = (i + 1) * size + (j + 1);
                }
            }
        }

        gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        mesh = GetComponent<MeshFilter>().mesh;
        meshRenderer.material = material;
    }

    void Simulate() {
        int k = shader.FindKernel("CSMain");

        // host to device
        u_pre.SetData(host_u_pre);
        u_now.SetData(host_u_now);

        //引数をセット
        shader.SetInt("_Size", size);
        shader.SetInt("_Div", divide);
        shader.SetFloat("_Velocity", velocity);
        shader.SetFloat("_StepT", timestep);
        shader.SetBuffer(k, "u_pre", u_pre);
        shader.SetBuffer(k, "u_now", u_now);
        shader.SetBuffer(k, "u_nex", u_nex);

        //GPUで計算
        shader.Dispatch(k, 4, 4, 1);

        // device to host
        u_nex.GetData(host_u_nex);

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                
                vertices[i * size + j].y = host_u_now[i * size + j];

            }
        }

        for (int i = 0; i < size * size; i++) {
            host_u_pre[i] = host_u_now[i];
            host_u_now[i] = host_u_nex[i];
        }

        Debug.Log(host_u_now[(size / 2) * size + size / 2]);
    }

    private void OnDestroy()
    {
        //解放
        u_pre.Release();
        u_now.Release();
        u_nex.Release();
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

    int Gettime() {
        return DateTime.Now.Millisecond + DateTime.Now.Second * 1000 + DateTime.Now.Minute * 60 * 1000 + DateTime.Now.Hour * 60 * 60 * 1000;    
    }
}
