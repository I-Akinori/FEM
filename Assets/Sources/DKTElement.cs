using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DKTElement
{
    static int count = 0;
    int index;
    int[] vertices;
    Vector3[] basepos;
    Vector3[] baseang;
    float[,] eMat;
    float[,] bbMat;  // DKT：曲げ
    float[,] ebMat;  // 
    float[,] bmMat;  // CST：膜
    float[,] emMat;  // 
    float[,] kMat;
    float[,] t3;
    float[,] t6;
    float[,] t9;
    float[,] t18;
    float area;
    float poisson;
    float young;
    float thickness;

    public DKTElement()
    {
        index = count;
        vertices = new int[3];
        basepos = new Vector3[3];
        baseang = new Vector3[3];
        ebMat = new float[3, 3];
        bbMat = new float[3, 9];
        emMat = new float[3, 3];
        bmMat = new float[3, 6];
        kMat = new float[18, 18];
        t3 = new float[3, 3];
        t6 = new float[6, 6];
        t9 = new float[9, 9];
        t18 = new float[18, 18];
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
    public Vector3[] A0
    {
        set { baseang = value; }
        get { return baseang; }
    }
    public float[,] Eb
    {
        set { ebMat = value; }
        get { return ebMat; }
    }
    public float[,] Bb
    {
        set { bbMat = value; }
        get { return bbMat; }
    }
    public float[,] Em
    {
        set { emMat = value; }
        get { return emMat; }
    }
    public float[,] Bm
    {
        set { bmMat = value; }
        get { return bmMat; }
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
    public float[,] T6
    {
        set { t6 = value; }
        get { return t6; }
    }
    public float[,] T9
    {
        set { t9 = value; }
        get { return t9; }
    }
    public float[,] T18
    {
        set { t18 = value; }
        get { return t18; }
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
    public float h
    {
        set { thickness = value; }
        get { return thickness; }
    }
}
