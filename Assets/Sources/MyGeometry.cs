using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyGeometry
{

    public static void Test()
    {
        //Debug.Log("TEST!!!");
    }
}

public class Point : MyGeometry
{
    GameObject obj = (GameObject)Resources.Load("PaintModel");
    private Vector2 pos;
    public Point()                          // 引数なしコンストラクタ
    {
        pos = new Vector2(0.0f, 0.0f);
    }
    public Point(float x)                   // 単引数コンストラクタ
    {
        pos = new Vector2(x, x);
    }
    public Point(float x, float y)          // ２引数コンストラクタ
    {
        pos = new Vector2(x, y);
    }
    public Point(Vector2 V)                 // ベクトル引数コンストラクタ
    {
        pos = V;
    }
    public void SetPosition(float x, float y)       // ２引数位置代入
    {
        pos = new Vector2(x, y);
    }
    public void SetPosition(Vector2 V)              // ベクトル引数位置代入
    {
        pos = V;
    }
    public Vector2 GetPosition()                    // 位置取得
    {
        return new Vector2(pos.x, pos.y);
    }
    public Vector3 Get3DPosition(float z)           // ３次元位置取得; 引数はゼット座標
    {
        return new Vector3(pos.x, pos.y, z);
    }

    public bool equals(Point P)                     // point引数との位置比較
    {
        return (pos.x == P.pos.x && pos.y == P.pos.y);
    }

    public float dist(Point P)                      // point引数との距離計算
    {
        return Mathf.Sqrt(Mathf.Pow(pos.x - P.pos.x, 2.0f) + Mathf.Pow(pos.y - P.pos.y, 2.0f)); 
    }
    public float dist(Vector2 P)                    // ベクトルとの距離計算
    {
        return Mathf.Sqrt(Mathf.Pow(pos.x - P.x, 2.0f) + Mathf.Pow(pos.y - P.y, 2.0f));
    }
}
public class Edge : MyGeometry                      // 稜線
{
    private Point[] Ends = new Point[2];            // 両端の頂点
    private int[] Indexes = new int[2];             // 両端のインデックス

    public Edge(Point P1, Point P2)                 // 2頂点引数コンストラクタ
    {
        Ends[0] = P1;
        Ends[1] = P2;
    }
    public Edge(int N1, int N2)                    // 2インデックス引数コンストラクタ
    {
        Indexes[0] = N1;
        Indexes[1] = N2;
    }
    public Edge(int N1, int N2, List<Point> PL)   // 2インデックス＋頂点リスト引数コンストラクタ
    {
        Indexes[0] = N1;
        Indexes[1] = N2;
        Ends[0] = PL[N1];
        Ends[1] = PL[N2];
    }
    public bool equals(Edge E)   // 2インデックス＋頂点リスト引数コンストラクタ
    {
        return (E.GetEnds(1).equals(Ends[0]) && E.GetEnds(2).equals(Ends[1])) || (E.GetEnds(1).equals(Ends[1]) && E.GetEnds(2).equals(Ends[0]));

    }

    public float length()                           // 長さ
    {
        return Ends[0].dist(Ends[1]);
    }

    public Point GetEnds(int i)                     // i (= 1, 2) 番目の頂点を取得
    {
        if (i == 2)
            return Ends[1];
        else
            return Ends[0];
    }

    public bool IsContain(Point P)
    {
        float x0 = Ends[0].GetPosition().x;
        float x1 = Ends[1].GetPosition().x;
        float y0 = Ends[0].GetPosition().y;
        float y1 = Ends[1].GetPosition().y;

        float vx = x1 - x0;
        float vy = y1 - y0;
        float px = P.GetPosition().x - x0;
        float py = P.GetPosition().y - y0;

        return vx * py == vy * px;
    }

} 

public class Triangle : MyGeometry
{
    protected Point[] Vertices = new Point[3];
    protected int[] Indexes = new int[3];

    public Triangle(Point P1, Point P2, Point P3)   // ３頂点引数コンストラクタ
    {
        Vertices[0] = P1;
        Vertices[1] = P2;
        Vertices[2] = P3;
    }

    public Triangle(int N1, int N2, int N3)   // 3インデックス引数コンストラクタ
    {
        Indexes[0] = N1;
        Indexes[1] = N2;
        Indexes[2] = N3;
    }
    public Triangle(int N1, int N2, int N3, List<Point> PL)   // 3インデックス＋頂点リスト引数コンストラクタ
    {
        Indexes[0] = N1;
        Indexes[1] = N2;
        Indexes[2] = N3;
        Vertices[0] = PL[N1];
        Vertices[1] = PL[N2];
        Vertices[2] = PL[N3];
    }


    public void SetVertex(int i, Point P)           // i（= 1,2,3）番頂点の頂点引数による点設定
    {
        if (i > 0 && i <= 3) Vertices[i - 1] = P;
    }
    public void SetVertex(int i, float x, float y)  // i（= 1,2,3）番頂点の座標引数による点設定
    {
        Point P = new Point(x, y);
        if (i > 0 && i <= 3) Vertices[i - 1] = P;
    }
    public void SetVertex(int i, Vector2 V)         // i（= 1,2,3）番頂点のベクトル引数による点設定
    {
        Point P = new Point(V);
        if (i > 0 && i <= 3) Vertices[i - 1] = P;
    }
    public Point GetVertex(int i)                   // i（= 1,2,3）番頂点の点取得
    {
        if (i > 0 && i <= 3) return Vertices[i - 1];
        else return new Point(0.0f);
    }

    public int GetIndex(int i)                   // i（= 1,2,3）番頂点のインデックス取得
    {
        if (i > 0 && i <= 3) return Indexes[i - 1];
        else return -1;
    }

    public Vector2 GetVertexPosition(int i)         // i（= 1,2,3）番頂点の位置ベクトル取得
    {
        if (i > 0 && i <= 3) return Vertices[i - 1].GetPosition();
        else return new Vector2(0.0f, 0.0f);
    }

    public Vector2[] GetVertexPositions()           // ３頂点の位置ベクトル配列取得
    {
        Vector2[] Positions = new Vector2[4];
        for (int i = 1; i < 4; i++)
            Positions[i] = Vertices[i - 1].GetPosition();
        return Positions;
    }

    public Vector3 GetVertex3DPosition(int i, float z)　// i（= 1,2,3）番頂点の３次元位置ベクトル取得; 引数はz座標
    {
        if (i > 0 && i <= 3) return Vertices[i - 1].Get3DPosition(z);
        else return new Vector3(0.0f, 0.0f, z);
    }

    public Vector3[] GetVertex3DPositions(float z)     // ３頂点の３次元位置ベクトル配列取得; 引数はz座標
    {
        Vector3[] Positions = new Vector3[4];
        for (int i = 1; i < 4; i++)
            Positions[i] = Vertices[i - 1].Get3DPosition(z);
        return Positions;
    }
    public int hashCode()
    {
        return 0;
    }
    public bool equals(Triangle T)                      // 三角形の比較
    {
        return (Vertices[0].equals(T.Vertices[0]) && Vertices[1].equals(T.Vertices[1]) && Vertices[2].equals(T.Vertices[2]) ||
                Vertices[0].equals(T.Vertices[0]) && Vertices[1].equals(T.Vertices[2]) && Vertices[2].equals(T.Vertices[1]) ||
                Vertices[0].equals(T.Vertices[1]) && Vertices[1].equals(T.Vertices[0]) && Vertices[2].equals(T.Vertices[2]) ||
                Vertices[0].equals(T.Vertices[1]) && Vertices[1].equals(T.Vertices[2]) && Vertices[2].equals(T.Vertices[0]) ||
                Vertices[0].equals(T.Vertices[2]) && Vertices[1].equals(T.Vertices[0]) && Vertices[2].equals(T.Vertices[1]) ||
                Vertices[0].equals(T.Vertices[2]) && Vertices[1].equals(T.Vertices[1]) && Vertices[2].equals(T.Vertices[0])
            );
    }
    public bool hasCommonVertices(Triangle T)           // 共有頂点の有無
    {
        return (Vertices[0].equals(T.Vertices[0]) || Vertices[1].equals(T.Vertices[0]) || Vertices[2].equals(T.Vertices[0]) ||
                Vertices[0].equals(T.Vertices[1]) || Vertices[1].equals(T.Vertices[1]) || Vertices[2].equals(T.Vertices[1]) ||
                Vertices[0].equals(T.Vertices[2]) || Vertices[1].equals(T.Vertices[2]) || Vertices[2].equals(T.Vertices[2])
            );
    }

    public Circle CircumscrivedCircle()
    {
        Point Center = new Point();
        float radius;

        float dx12 = Vertices[0].GetPosition().x - Vertices[1].GetPosition().x; 
        //float dx23 = Vertices[1].GetPosition().x - Vertices[2].GetPosition().x;
        float dx31 = Vertices[2].GetPosition().x - Vertices[0].GetPosition().x;
        float sx12 = Vertices[0].GetPosition().x + Vertices[1].GetPosition().x;
        float sx31 = Vertices[2].GetPosition().x + Vertices[0].GetPosition().x;

        float dy12 = Vertices[0].GetPosition().y - Vertices[1].GetPosition().y;
        //float dy23 = Vertices[1].GetPosition().y - Vertices[2].GetPosition().y;
        float dy31 = Vertices[2].GetPosition().y - Vertices[0].GetPosition().y;
        float sy12 = Vertices[0].GetPosition().y + Vertices[1].GetPosition().y;
        float sy31 = Vertices[2].GetPosition().y + Vertices[0].GetPosition().y;

        float c = 2 * (dy12 * dx31 - dx12 * dy31);
        float c1 = dx31 * sx31 + dy31 * sy31;
        float c2 = dx12 * sx12 + dy12 * sy12;
        Center.SetPosition((dy12 * c1 - dy31 * c2) / c, -(dx12 * c1 - dx31 * c2) / c);
        radius = Center.dist(Vertices[0]);

        return new Circle(Center, radius);

    }

    public bool IsContain(Point P)                      // ある点が内部に存在しているかどうか
    {
        float x0 = Vertices[0].GetPosition().x;
        float x1 = Vertices[1].GetPosition().x;
        float x2 = Vertices[2].GetPosition().x;
        float y0 = Vertices[0].GetPosition().y;
        float y1 = Vertices[1].GetPosition().y;
        float y2 = Vertices[2].GetPosition().y;

        float ax = x1 - x0;
        float bx = x2 - x0;
        float ay = y1 - y0;
        float by = y2 - y0;
        float px = P.GetPosition().x - x0;
        float py = P.GetPosition().y - y0;

        float c = ax * by - ay * bx;

        float s = (   by * px - bx * py) / c;
        float t = ( - ay * px + ax * py) / c;

        return s > 0 && t > 0 && s + t < 1; 
    }

    public bool CCircleIsContain(Point P)           // 外接円がある点を含む（円周上でも可）かどうか
    {
        return CircumscrivedCircle().IsContain(P);
    }

    public int IsHave(Edge E)                      // ある稜線を辺にもつかどうか。返り値は残りの頂点番号(1～3)ない場合は0。
    {
        for (int i = 0; i < 3; i++)
        {
            if (E.equals(new Edge(Vertices[i % 3], Vertices[(i + 1) % 3]))) return (i + 2) % 3 + 1;
        }
        return 0;
    }
}

public class Circle : MyGeometry
{
    private Point Center;
    private float Radius;
    public Circle(Point C, float r)
    {
        Center = C;
        Radius = r;
    }
    public Circle(Vector2 C, float r)
    {
        Center = new Point(C);
        Radius = r;
    }
    public Point GetCenter()
    {
        return Center;
    }

    public float GetRadius()
    {
        return Radius;
    }

    public bool IsContain(Point P)
    {
        return Center.dist(P) <= Radius;
    }
}