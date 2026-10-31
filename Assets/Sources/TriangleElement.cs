using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriangleElement
{
    static int count = 0;
    int index;
    int[] vertices;
    Vector3[] basepos;
    float[,] eMat;
    float[,] bMat;
    float[,] kMat;
    float[,] t3;
    float[,] t9;
    float area;
    float poisson;
    float young;

    public TriangleElement()
    {
        index = count;
        vertices = new int[3];
        basepos = new Vector3[3];
        eMat = new float[9, 9];
        bMat = new float[9, 9];
        kMat = new float[9, 9];
        t3 = new float[3, 3];
        t9 = new float[9, 9];
        count++;
    }
    public int Index
    {
        get { return index; }
    }
    public int[] V
    {
        set { vertices = value; }
        get { return vertices; }
    }
    public Vector3[] P0
    {
        set { basepos = value; }
        get { return basepos; }
    }
    public float[,] E
    {
        set { eMat = value; }
        get { return eMat; }
    }
    public float[,] B
    {
        set { bMat = value; }
        get { return bMat; }
    }
    public float[,] K
    {
        set { kMat = value; }
        get { return kMat; }
    }
    public float[,] T3
    {
        set { t3 = value; }
        get { return t3; }
    }
    public float[,] T9
    {
        set { t9 = value; }
        get { return t9; }
    }
    public float A
    {
        set { area = value; }
        get { return area; }
    }
    public float p
    {
        set { poisson = value; }
        get { return poisson; }
    }
    public float y
    {
        set { young = value; }
        get { return young; }
    }
}
