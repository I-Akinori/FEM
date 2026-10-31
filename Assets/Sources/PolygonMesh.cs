using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vertex
{
    static private int num = 0;
    private int idx;
    private Vector3 pos;
    private Vector3 nor;
    private Color col;
    private Vector2 tex;
    private HalfEdge he;
    private List<Vertex> nvs;       // Nearby VerticeS
    private float rust;
    private float dust;
    private float bond;
    private float liftingForce;
    private int peelScale;
    private int status;
    private PerlinNoise _noise;
    private Vector3 direction;  // ひび割れの方向 (status == 1 のときに参照)
    private int bound;
    private float b_value;
    private Vector3 b_vector;

    public PerlinNoise Noise
    {
        get
        {
            if (_noise == null)
            {
                _noise = new PerlinNoise(1000);
            }
            return _noise;
        }
    }
    public Vertex(Vector3 p)
    {
        idx = num; num++;
        pos = p;
        nor = new Vector3(0, 0, -1);
        col = new Color(1.0f, 1.0f, 1.0f);
        tex = new Vector2(0, 0);
        he = null;
        nvs = new List<Vertex>();
        rust = 0.0f;
        dust = 0.0f;
        liftingForce = 0.0f;
        bond = Noise.FBM(p * 0.01f) * 30 + 70;  // 標準 +40   // 正方格子 +60  //  teapot 80 // teapo4  
        peelScale = 0;
        status = 0;                             // 0: 初期状態,  1: 分離，　2: ひびの端点, 3: ひびの中継点
                                                // 4: サブ，中継点,  5: サブ，コーナー
        bound = 0;                              // 境界条件　境界でなければ0
        b_value = 0.0f;                         // 境界値
        b_vector = new Vector3(0, 0, 0);
    }
    public Vertex(Vertex Ver)
    {
        idx = num; num++;
        pos = Ver.Pos;
        nor = Ver.Nor;
        col = Ver.Col;
        tex = Ver.Tex;
        he = Ver.HE;
        nvs = new List<Vertex>();
        rust = Ver.Rust;
        dust = Ver.Dust;
        liftingForce = Ver.LiftingForce;
        bond = Ver.Bond;
        peelScale = Ver.PeelScale;
        status = Ver.Status;
        bound = 0;                              
        b_value = 0.0f;
        b_vector = new Vector3(0, 0, 0);
    }
    public Vertex(Vector3 p, Vector3 n)
    {
        idx = num; num++;
        pos = p;
        nor = n;
        col = new Color(1.0f, 1.0f, 1.0f);
        tex = new Vector2(0, 0);
        he = null;
        nvs = new List<Vertex>();
        rust = 0.0f;
        dust = 0.0f;
        liftingForce = 0.0f;
        bond = Noise.FBM(p * 0.01f) * 30 + 70;   // 標準 +40   // 正方格子 +60
        peelScale = 0;
        status = 0;
        bound = 0;
        b_value = 0.0f;
        b_vector = new Vector3(0, 0, 0);
    }
    public Vertex(Vector3 p, Vector3 n, Vector2 t)
    {
        idx = num; num++;
        pos = p;
        nor = n;
        col = new Color(1.0f, 1.0f, 1.0f);
        tex = t;
        he = null;
        nvs = new List<Vertex>();
        rust = 0.0f;
        dust = 0.0f;
        liftingForce = 0.0f;
        bond = Noise.FBM(p * 0.01f) * 50 + 30;   // 標準 +40   // 正方格子 +60
        peelScale = 0;                           // Plane 50 30  // bunny3 0.02 30 20
        status = 0;
        bound = 0;
        b_value = 0.0f;
        b_vector = new Vector3(0, 0, 0);
    }

    public void ReloadNearbyVertices()
    {
        nvs.Clear();
        HalfEdge tmpHE = he;

        do
        {
            if (tmpHE.Pair == null) // 繋がらなかったら逆回転
            {
                tmpHE = he;
                do
                {
                    nvs.Add(tmpHE.Prev.Vert);
                    if (tmpHE.Prev.Pair == null)
                        break;

                    tmpHE = tmpHE.Prev.Pair;
                } while (true);
                break;
            }
            nvs.Add(tmpHE.Pair.Vert);
            tmpHE = tmpHE.Pair.Next;
        } while (tmpHE != he);
    }
    public int Num
    {
        set { num = value; }
        get { return num; }
    }
    public int Idx
    {
        set { idx = value; }
        get { return idx; }
    }
    public Vector3 Pos
    {
        set { pos = value; }
        get { return pos; }
    }

    public Vector3 Nor
    {
        set { nor = value; }
        get { return nor; }
    }

    public Color Col
    {
        set { col = value; }
        get { return col; }
    }

    public Vector2 Tex
    {
        set { tex = value; }
        get { return tex; }
    }
    public HalfEdge HE
    {
        set { he = value; }
        get { return he; }
    }
    public List<Vertex> NVS
    {
        set { nvs = value; }
        get { return nvs; }
    }
    public float Rust
    {
        set { rust = value; }
        get { return rust; }
    }
    public float Dust
    {
        set { dust = value; }
        get { return dust; }
    }
    public float LiftingForce
    {
        set { liftingForce = value; }
        get { return liftingForce; }
    }
    public float Bond
    {
        set { bond = value; }
        get { return bond; }
    }
    public int PeelScale
    {
        set { peelScale = value; }
        get { return peelScale; }
    }
    public int Status
    {
        set { status = value; }
        get { return status; }
    }
    public Vector3 Direction
    {
        set { direction = value; }
        get { return direction; }
    }
    public int Bound
    {
        set { bound = value; }
        get { return bound; }
    }
    public float B_value
    {
        set { b_value = value; }
        get { return b_value; }
    }
    public Vector3 B_vector
    {
        set { b_vector = value; }
        get { return b_vector; }
    }
}

public class HalfEdge
{
    static private int num = 0;
    private int idx;
    private float liftingForce;
    private float bindingForce;
    private float curvature;
    private Vertex vert;
    private HalfEdge pair;
    private HalfEdge next;
    private HalfEdge prev;
    private TPolygon face;
    private bool connected;

    public HalfEdge(Vertex V)
    {
        idx = num; num++;
        liftingForce = 0.0f;
        bindingForce = 70.0f;
        curvature = 0.0f;
        vert = V;
        pair = null;
        next = null;
        prev = null;
        face = null;
        connected = true;
    }

    public int ReloadPair(List<HalfEdge> EList)
    {
        /*
        for (int i = 0; i < EList.Count; i++)
            if ((this.end1 == EList[i].End1 && this.end2 == EList[i].End2) || (this.end1 == EList[i].End2 && this.end2 == EList[i].End1))
            {
                if (this.oppositeVertex != EList[i].OppositeVertex)
                {
                    this.pair = i;
                    return i;
                }
            }
            */
        return -1;
    }
    public int ReloadIncluding(List<TPolygon> TList)
    {
        /*
        for (int i = 0; i < TList.Count; i++)
        {
            TPolygon TPi = TList[i];
            if ((this.end1 == TPi.V1 && this.end2 == TPi.V2 && this.oppositeVertex == TPi.V3) || (this.end1 == TPi.V2 && this.end2 == TPi.V1 && this.oppositeVertex == TPi.V3)
             || (this.end1 == TPi.V2 && this.end2 == TPi.V3 && this.oppositeVertex == TPi.V1) || (this.end1 == TPi.V3 && this.end2 == TPi.V2 && this.oppositeVertex == TPi.V1)
             || (this.end1 == TPi.V3 && this.end2 == TPi.V1 && this.oppositeVertex == TPi.V2) || (this.end1 == TPi.V1 && this.end2 == TPi.V3 && this.oppositeVertex == TPi.V2)
               )
            {
                this.including = i;
                return i;
            }
        }
        Debug.Log("Including Polygon does NOT exsit.");
        */
        return -1;
    }
    public int Num
    {
        set { num = value; }
        get { return num; }
    }
    public int Idx
    {
        set { idx = value; }
        get { return idx; }
    }
    public float LiftingForce
    {
        set { liftingForce = value; }
        get { return liftingForce; }
    }
    public float BindingForce
    {
        set { bindingForce = value; }
        get { return bindingForce; }
    }
    public float Curvature
    {
        set { curvature = value; }
        get { return curvature; }
    }
    public Vertex Vert
    {
        set { vert = value; }
        get { return vert; }
    }
    public TPolygon Face
    {
        set { face = value; }
        get { return face; }
    }
    public HalfEdge Pair
    {
        set { pair = value; }
        get { return pair; }
    }
    public HalfEdge Next
    {
        set { next = value; }
        get { return next; }
    }
    public HalfEdge Prev
    {
        set { prev = value; }
        get { return prev; }
    }
    public bool Connected
    {
        set { connected = value; }
        get { return connected; }
    }
}
public class TPolygon
{
    static private int num = 0;
    private int idx;
    private HalfEdge he;
    private float contractionForce;
    private Vector3 normal;
    private bool peeled;
    private int peelScale;
    private int relax;

    public TPolygon(HalfEdge H)
    {
        idx = num; num++;
        he = H;
        contractionForce = 100.0f;
        normal = new Vector3(0f, 0f, -1f);
        peeled = false;
        peelScale = 0;
        relax = 0;
    }

    public Vector3 ReloadNormal()
    {
        normal = Vector3.Cross(he.Vert.Pos - he.Next.Vert.Pos, he.Vert.Pos - he.Prev.Vert.Pos).normalized;
        return normal;
    }
    public int Num
    {
        set { num = value; }
        get { return num; }
    }
    public int Idx
    {
        set { idx = value; }
        get { return idx; }
    }
    public HalfEdge HE
    {
        set { he = value; }
        get { return he; }
    }
    public float ContractionForce
    {
        set { contractionForce = value; }
        get { return contractionForce; }
    }
    public Vector3 Normal
    {
        set { normal = value; }
        get { return normal; }
    }
    public bool Peeled
    {
        set { peeled = value; }
        get { return peeled; }
    }

    public int PeelScale
    {
        set { peelScale = value; }
        get { return peelScale; }
    }
    public int Relax
    {
        set { relax = value; }
        get { return relax; }
    }
}
public class PolygonMesh : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
