using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class SPMatrix
{
    static private System.Diagnostics.Stopwatch sw1 = new System.Diagnostics.Stopwatch();
    static private System.Diagnostics.Stopwatch sw2 = new System.Diagnostics.Stopwatch();

    private int size1;
    private int size2;
    private int length;
    private List<float> values;
    private List<int> row;
    private List<int> column;
    public SPMatrix(float[,] array) {
        values = new List<float>();
        row    = new List<int>();
        column = new List<int>();
        size1 = array.GetLength(0);
        size2 = array.GetLength(1);
        length = 0;

        for (int i = 0; i < array.GetLength(0); i++) {
            bool first_element = true;
            for (int j = 0; j < array.GetLength(1); j++) {
                if (array[i, j] != 0) {
                    values.Add(array[i, j]);
                    column.Add(j);
                    if (first_element) {
                        row.Add(length);
                        first_element = false;
                    }
                    length++;
                }
            }
            if (first_element)
                row.Add(length);
        }
        row.Add(length);
    }
    public SPMatrix(SPMatrix SPM)
    {
        values = new List<float>(SPM.A);
        row = new List<int>(SPM.IA);
        column = new List<int>(SPM.JA);
        size1 = SPM.Size1;
        size2 = SPM.Size2;
    }
    public SPMatrix(int d)
    {
        values = new List<float>();
        row = new List<int>();
        column = new List<int>();
        size1 = d;
        size2 = d;
        for (int i = 0; i < d; i++)
            row.Add(0);
        row.Add(0);
    }
    public SPMatrix()
    {
        values = new List<float>();
        row = new List<int>();
        column = new List<int>();
        size1 = 0;
        size2 = 0;
    }

    public SPMatrix Transpose() {
        int v_length = values.Count;
        Item[] Items = new Item[v_length];
        int count = 0;
        for (int i = 0; i < values.Count; i++) {
            if (i >= IA[count + 1])
            {
                count++;
            }
            Items[i] = new Item(A[i], count, JA[i]);
        }
        Array.Sort(Items);

        SPMatrix T = new SPMatrix();
        T.Size1 = size2;
        T.Size2 = size1;
        int tmp = -1;
        for (int i = 0; i < values.Count; i++)
        {
            T.A.Add(Items[i].Val);
            T.JA.Add(Items[i].Row);
            if (Items[i].Col != tmp) {
                tmp = Items[i].Col;
                T.IA.Add(i);
            }
        }
        T.IA.Add(values.Count);

        return T;
    }

    public void SetElement(int m, int n, float v)
    {
        int index = -1;
        for (int i = row[m]; i < row[m + 1]; i++)
            if (column[i] == n) { index = i; break; }

        if (v == 0)
        {
            if (index >= 0)
            {
                values.RemoveAt(index);
                column.RemoveAt(index);
                for (int i = m + 1; i < size1 + 1; i++)
                    row[i] -= 1;
            }
        }
        else {
            if (index >= 0)
            {
                values[index] = v;
            }
            else {
                index = row[m + 1];
                for (int i = row[m]; i < row[m + 1]; i++)
                    if (n < column[i]) { index = i; break; }

                values.Insert(index, v);
                column.Insert(index, n);
                for (int i = m + 1; i < size1 + 1; i++)
                    row[i] += 1;
            }
        }
    }
    public void AddElement(int m, int n, float v) {
        int index = -1;

        if (v != 0)
        {
            for (int i = row[m]; i < row[m + 1]; i++)
                if (column[i] == n) { index = i; break; }

            if (index >= 0)
            {
                values[index] += v;
                if (values[index] == 0) {
                    values.RemoveAt(index);
                    column.RemoveAt(index);
                    for (int i = m + 1; i < size1 + 1; i++)
                        row[i] -= 1;
                }
            }
            else
            {
                index = row[m + 1];
                for (int i = row[m]; i < row[m + 1]; i++)
                    if (n < column[i]) { index = i; break; }

                values.Insert(index, v);
                column.Insert(index, n);
                for (int i = m + 1; i < size1 + 1; i++)
                    row[i] += 1;
            }
        }
    }
    public void SubElement(int m, int n, float v) {
        AddElement(m, n, -v);
    }
    public float GetElement(int m, int n) {
        for (int i = row[m]; i < row[m + 1]; i++)
            if (column.Count > i && column[i] == n) return values[i];
        return 0;
    }
    public void RemoveRowCol(int m) {           // 対称行列のm行とm列を除く
        List<int> idxList = new List<int>();

        int delta = 0;
        int idx1 = IA[m];
        int idx2 = IA[m + 1];
        for (int i = IA[m]; i < IA[m + 1]; i++)
        {
            idxList.Add(JA[i]);
        }
        int count = 0;
        for (int i = 0; i < IA.Count; i++)
        {
            IA[i] -= delta;
            if (count < idxList.Count && i == idxList[count]) {
                if (i == m) 
                    delta += idx2 - idx1;
                else
                    delta += 1;
                count++;
            } else if (i == m) {
                delta += (idx2 - idx1);
            };
        }
        for (int i = idx2 - 1; i >= idx1; i--)
        {
            A.RemoveAt(i);
            JA.RemoveAt(i);
        }
        IA.RemoveAt(m);
        
        for (int i = A.Count - 1; i >= 0; i--)
        {
            if (JA[i] > m) JA[i]--;
            else if (JA[i] == m) {
                A.RemoveAt(i);
                JA.RemoveAt(i);
            }
        }
        Size1--;
        Size2--;
    }
    public void RemoveRowCol(int[] m)
    {
        Array.Sort(m);
        for (int i = m.Length - 1; i >= 0; i--) {
            RemoveRowCol(m[i]);
        }
    }
    public void RemoveRowCol(List<int> m)
    {
        m.Sort((a, b) => b - a);
        for (int i = 0; i < m.Count; i++)
        {
            RemoveRowCol(m[i]);
        }
    }
    public bool IsZero(int m, int n) {
        for (int i = row[m]; i < row[m + 1]; i++)
            if (column[i] == n) return false;
        return true;
    }
    public bool IsSymetric() {
        bool sym = true;
        for (int i = 0; i < size1; i++)
            for (int j = i; j < size2; j++)
                if (GetElement(i, j) != GetElement(j, i)) {
                    sym = false;
                    Debug.Log(i + ", " +  j);
                    continue;
                }
        return sym;
    }
    public void ForceSymetric()
    {
        for (int i = 0; i < size1; i++)
            for (int j = i; j < size2; j++) {
                if (GetElement(i, j) != GetElement(j, i))
                {
                    SetElement(j, i, GetElement(i, j));
                }
                if (GetElement(i, j) < 0.0001f) {
                    SetElement(i, j, 0);
                    SetElement(j, i, 0);
                }

            }
    }
    public float[] LinearConversion(float[] x)
    {
        if (x.Length != size2)
        {
            Debug.LogError("Cannnot define the product.");
            return null;
        }
        float[] y = new float[size1];
        for (int i = 0; i < size1; i++) {
            y[i] = 0;
            for (int j = row[i]; j < row[i + 1]; j++) {
                y[i] += values[j] * x[column[j]];
            }
        }
        return y;
    }
    public float[] LinearConversionT(float[] x)
    {
        if (x.Length != size1)
        {
            Debug.LogError("Cannnot define the product.");
            return null;
        }
        float[] y = new float[size2];
        for (int i = 0; i < size2; i++) {
            y[i] = 0; 
        }
        for (int i = 0; i < size1; i++)
        {
            for (int j = row[i]; j < row[i + 1]; j++)
            {
                y[column[j]] += values[j] * x[i];
            }
        }
        return y;
    }
    virtual public void PrintMatrix()
    {
        for (int i = 0; i < size1; i++)
        {
            string str = "|";
            for (int j = 0; j < size2; j++)
            {
                str += GetElement(i, j);
                if (j < size2 - 1)
                    str += ", ";
                else str += "|";
            }
            Debug.Log(str);
        }
    }

    public float[,] SymMatrix(int size, float p) {
        float[,] M = new float[size, size];

        for (int i = 0; i < size; i++)
        {
            M[i, i] = UnityEngine.Random.value * 2f - 1f;
            for (int j = i + 1; j < size; j++)
            {
                if (UnityEngine.Random.value < p - 1f / size)
                {
                    M[i, j] = UnityEngine.Random.value * 2f - 1f;
                }
                else
                {
                    M[i, j] = 0;
                }
                M[j, i] = M[i, j];
            }
        }
        return M;
    }

    /*
    private void Start()
    {
        int[] sizes = {64, 128, 256, 512, 1024, 2048, 4096, 8192};
        float[] ratio = { 0.025f, 0.05f, 0.1f, 0.2f, 0.4f };
        float[,] M;
        SPMatrix spM;
        float[] d;

        for (int i = 0; i < 5; i++) {
            int size = sizes[4];
            float p = ratio[i];
            float[,] L1 = new float[size, size];
            SPMatrix L2 = new SPMatrix(size);

            float[] c = new float[size];
            float[] solution = new float[size];
            sw1.Reset();
            sw2.Reset();

            for (int j = 0; j < 20; j++) {
                M = SymMatrix(size, p);
                spM = new SPMatrix(M);

                for (int k = 0; k < size; k++)
                {
                    c[j] = UnityEngine.Random.value;
                    solution[i] = 0;
                }

                sw1.Start();
                Solver.ICCG(M, c, solution);
                sw1.Stop();
                sw2.Start();
                Solver.ICCG(spM, c);
                sw2.Stop();
            }
            Debug.Log("Size:" + size);
            Debug.Log("  Mat: " + sw1.Elapsed);
            Debug.Log("  CRS: " + sw2.Elapsed);
        }
        
        Debug.Log("finish");
    }*/
    public int Size1
    {
        set { size1 = value; }
        get { return size1; }
    }
    public int Size2
    {
        set { size2 = value; }
        get { return size2; }
    }
    public int Length
    {
        set { length = value; }
        get { return length; }
    }
    public List<float> A {
        set { values = value; }
        get { return values; }
    }
    public List<int> IA
    {
        set { row = value; }
        get { return row; }
    }
    public List<int> JA
    {
        set { column = value; }
        get { return column; }
    }
}

public class Item: IComparable {
    float val;
    int row;
    int col;
    public Item(float v, int r, int c) {
        val = v;
        row = r;
        col = c;
    }
    public int CompareTo(object obj)
   {
        int i = col -((Item)obj).col;
       return i != 0 ? i: row - ((Item)obj).row;
   }

    public float Val
    {
        set { val = value; }
        get { return val; }
    }
    public int Row
    {
        set { row = value; }
        get { return row; }
    }
    public int Col
    {
        set { col = value; }
        get { return col; }
    }
}