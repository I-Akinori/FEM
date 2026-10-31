using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadText : MonoBehaviour
{
    private Material _material;

    private Mesh _mesh;

    // (1) 頂点座標（この配列のインデックスが頂点インデックス）
    private Vector3[] _positions = new Vector3[]{
        new Vector3(1, 0, 0),
        new Vector3(-1, 0, 0),
        new Vector3(0, 1, 0)
    };

    // (2) ポリゴンを形成する頂点インデックスを順番に指定する
    private int[] _triangles = new int[] {0, 1, 2};

    // (3) 法線
    private Vector3[] _normals = new Vector3[]{
        new Vector3(0, 0, -1),
        new Vector3(0, 0, -1),
        new Vector3(0, 0, -1)
    };

    static public List<Vector3> Positions = new List<Vector3>();
    static public List<Vector3Int> Polygons = new List<Vector3Int>();
    static public List<Vector2> IntactUVs = new List<Vector2>();        // objファイルの順番通りのテクスチャ座標リスト
    static public List<Vector2> ArrangedUVs = new List<Vector2>();      // 頂点リストと番号が対応しているテクスチャ座標リスト
    static public List<int> IndexList = new List<int>();
    static public List<Vector3> PNormals = new List<Vector3>();
    static public List<Vector3> VNormals = new List<Vector3>();

    public string[] textMessage; //テキストの加工前の一行を入れる変数
    public string[,] textWords; //テキストの複数列を入れる2次元は配列 

    private int rowLength; //テキスト内の行数を取得する変数
    private int columnLength; //テキスト内の列数を取得する変数

    private void Awake()
    {
        TextAsset textasset = new TextAsset(); //テキストファイルのデータを取得するインスタンスを作成
        textasset = Resources.Load("plane163", typeof(TextAsset)) as TextAsset; //Resourcesフォルダから対象テキストを取得
        //textasset = Resources.Load("teapot4", typeof(TextAsset)) as TextAsset;
        
        string TextLines = textasset.text; //テキスト全体をstring型で入れる変数を用意して入れる

        //Splitで一行づつを代入した1次配列を作成
        textMessage = TextLines.Split('\n'); //

        //行数と列数を取得
        rowLength = textMessage.Length;
        columnLength = 4;

        //2次配列を定義
        textWords = new string[rowLength, columnLength];

        for (int i = 0; i < rowLength; i++)
        {
            columnLength = textMessage[0].Split(' ').Length;

            string[] tempWords = textMessage[i].Split(' '); //textMessageをカンマごとに分けたものを一時的にtempWordsに代入

            switch (tempWords[0]) {
                // objファイルは "キーワード" + "データ" という構成になっている
                // まずはキーワードを読取り，そのあとのデータが何なのか判断する
                // 現在は v: 頂点，vt: uv座標，f: 面 を読取り，他は無視する
                // 法線に関しては 外積によって面の法線ベクトルを計算 → 周囲の面の法線ベクトルの平均をとって頂点の法線ベクトルを計算 という流れで求める
                // 普通ないとは思うが，上から順に処理できないような形式 (fがvより先にかかれるとか) には対応していない

                case "v": // vのあとには "x座標" "y座標" "z座標" が続く
                          //mountain //Positions.Add(0.03f * new Vector3(float.Parse(tempWords[1]), float.Parse(tempWords[2]) - 150f , float.Parse(tempWords[3])));
                          //Positions.Add(2.50f * new Vector3(- float.Parse(tempWords[1]), float.Parse(tempWords[2]) - 2.0f, - float.Parse(tempWords[3])));
                    Positions.Add(new Vector3(float.Parse(tempWords[1]), float.Parse(tempWords[2]), float.Parse(tempWords[3])));
                    ArrangedUVs.Add(new Vector2(0, 0)); // Positionsにあわせてサイズを大きくしている
                    break;

                case "vt": // vtのあとには "u座標" "v座標" が続く vの番号とvtの番号は対応していない (同じ番号のものが同じ頂点を指さない) ので注意！
                    IntactUVs.Add(new Vector2(float.Parse(tempWords[1]), float.Parse(tempWords[2])));
                    break;

                case "f": // fのあとには 各頂点が参照すべき番号 (1から始まっていることに注意) が続く Unityに合わせているため3頂点の場合のみ対応
                    Vector3 V1, V2, V3;
                    if (tempWords[1].Split('/').Length == 1 )   // 頂点番号のみが3つ並んでいる場合
                    {
                        Polygons.Add(new Vector3Int(int.Parse(tempWords[1]) - 1, int.Parse(tempWords[2]) - 1, int.Parse(tempWords[3]) - 1));
                        V1 = Positions[int.Parse(tempWords[1]) - 1];
                        V2 = Positions[int.Parse(tempWords[2]) - 1];
                        V3 = Positions[int.Parse(tempWords[3]) - 1];
                    } else
                    {                                           // 頂点番号/テクスチャ座標番号/法線番号 が3組並ぶ場合
                        // 頂点番号のみを取り出す: A/B/C  D/E/F  G/H/I  → A,D,G を取得
                        Polygons.Add(new Vector3Int(int.Parse(tempWords[1].Split('/')[0]) - 1, int.Parse(tempWords[2].Split('/')[0]) - 1, int.Parse(tempWords[3].Split('/')[0]) - 1));
                        V1 = Positions[int.Parse(tempWords[1].Split('/')[0]) - 1];
                        V2 = Positions[int.Parse(tempWords[2].Split('/')[0]) - 1];
                        V3 = Positions[int.Parse(tempWords[3].Split('/')[0]) - 1];

                        // テクスチャ座標番号のみを取り出す: A/B/C  D/E/F  G/H/I  → B,E,H を取得
                        ArrangedUVs[int.Parse(tempWords[1].Split('/')[0]) - 1] = IntactUVs[int.Parse(tempWords[1].Split('/')[1]) - 1];
                        ArrangedUVs[int.Parse(tempWords[2].Split('/')[0]) - 1] = IntactUVs[int.Parse(tempWords[2].Split('/')[1]) - 1];
                        ArrangedUVs[int.Parse(tempWords[3].Split('/')[0]) - 1] = IntactUVs[int.Parse(tempWords[3].Split('/')[1]) - 1];
                    }

                    PNormals.Add(Vector3.Normalize(Vector3.Cross(V2 - V1, V3 - V2)));
                    break;
            }
            
        }

        for (int i = 0; i < Polygons.Count; i++)
        {
            IndexList.Add(Polygons[i].x);
            IndexList.Add(Polygons[i].y);
            IndexList.Add(Polygons[i].z);
        }

        //法線計算
        for (int i = 0; i < Positions.Count; i++)
        {
            int count = 0;
            Vector3 N = new Vector3(0, 0, 0);

            for (int j = 0; j < Polygons.Count; j++)
            {
                if (i == Polygons[j].x || i == Polygons[j].y || i == Polygons[j].z)
                {
                    N += PNormals[j];
                    count++;
                }
            }
            VNormals.Add(N / count);
        }

        // メッシュの計算

        _mesh = new Mesh();
        
        // (4) Meshに頂点情報を代入
        Debug.Log(Positions.Count);
        Debug.Log(IndexList.Count);
        Debug.Log(VNormals.Count);

        _mesh.vertices = Positions.ToArray();
        _mesh.uv       = ArrangedUVs.ToArray();
        _mesh.triangles = IndexList.ToArray();
        _mesh.normals = VNormals.ToArray();

        _mesh.RecalculateBounds();
    }


    private void Update()
    {
        // (5) 描画
        //Graphics.DrawMesh(_mesh, Vector3.zero, Quaternion.identity, _material, 0);
    }
}
