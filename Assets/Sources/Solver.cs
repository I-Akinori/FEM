using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Solver : MonoBehaviour
{
    static private System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
    static private float e = 0.001f;
    static private int iterate = 30;
    static void InitArray(float[,] A, float[] b, float[] x) {
        float[,] S = new float[b.Length, b.Length];
        int count = 0;

        for (int i = 0; i < S.GetLength(0); i++) {
            for (int j = 0; j < S.GetLength(1); j++)
            {
                S[i, j] = UnityEngine.Random.value * 2f - 1f;
            }
        }

        for (int i = 0; i < A.GetLength(0); i++)
        {
            for (int j = 0; j < A.GetLength(1); j++)
            {
                A[i, j] = 0f;
                for (int k = 0; k < A.GetLength(1); k++)
                {
                    A[i, j] += S[i, k] * S[j, k];
                }
                if (i != j && (A[i, j] > 1f || A[i, j] < -1f)) {A[i, j] = 0f; count++; }// sparse
            }
        }
        Debug.Log("Count: " + count);

        for (int i = 0; i < b.GetLength(0); i++)
        {
            b[i] = UnityEngine.Random.value * 10f;
            x[i] = UnityEngine.Random.value * 10f;
        }
    }
    public static void Decomposition(float[,] A, float[,] L, float[] d)
    {
        for (int i = 0; i < L.GetLength(0); i++)
        {
            for (int j = 0; j < L.GetLength(1); j++)
            {
                L[i, j] = 0;
            }
        }

        L[0, 0] = A[0, 0];
        d[0] = 1 / A[0, 0];

        for (int i = 1; i < A.GetLength(0); i++)
        {
            for (int j = 0; j <= i; j++)
            {
                if (Mathf.Abs(A[i, j]) < e) // Imcomplete
                {
                    continue;
                }

                float lld = A[i, j];

                for (int k = 0; k < j; k++)
                {
                    lld -= L[i, k] * d[k] * L[j, k];
                }
                L[i, j] = lld;
            }
            d[i] = 1.0f / L[i, i];  
        }
    }
    public static SPMatrix Decomposition(SPMatrix A, ref float[] d)
    {
        d = new float[A.Size1];
        SPMatrix L = new SPMatrix();

        L.Size1 = A.Size1;
        L.Size2 = A.Size2;
        L.A.Add(A.A[0]);
        L.JA.Add(A.JA[0]);
        d[0] = 1.0f / L.A[L.A.Count - 1];

        L.IA.Add(0);
        L.IA.Add(1);

        for (int i = 1; i < A.Size1; i++)
        {
            int count_modifier = 0;
            for (int j = A.IA[i]; j < A.IA[i + 1]; j++)
            {
                if (i < A.JA[j]) break;

                float lld = A.A[j];
                int loop_k = j - A.IA[i] - count_modifier;
                int j_index = A.JA[j];

                for (int k = L.IA[i]; k < L.IA[i] + loop_k; k++)
                {
                    for (int l = L.IA[j_index]; ; l++) {
                        if (j_index + 1 < L.IA.Count && l >= L.IA[j_index + 1]) break;
                        if (L.JA[k] == L.JA[l]) {
                            lld -= L.A[k] * d[L.JA[l]] * L.A[l];
                            break;
                        }
                    }
                }
                if (lld != 0)
                {
                    L.A.Add(lld);
                    L.JA.Add(A.JA[j]);
                }
                else {
                    count_modifier++;
                }
            }
            d[i] = 1.0f / L.A[L.A.Count - 1];
            L.IA.Add(L.A.Count);
        }
        return L;
    }

    static void CDecomposition(float[,] A, float[,] L, float[] d)
    {
        for (int i = 0; i < L.GetLength(0); i++)
        {
            for (int j = 0; j < L.GetLength(1); j++)
            {
                L[i, j] = 0;
            }
        }

        L[0, 0] = A[0, 0];
        d[0] = 1 / A[0, 0];

        for (int i = 1; i < A.GetLength(0); i++)
        {
            for (int j = 0; j <= i; j++)
            {
                float lld = A[i, j];

                for (int k = 0; k < j; k++)
                {
                    lld -= L[i, k] * d[k] * L[j, k];
                }
                L[i, j] = lld;
            }
            d[i] = 1.0f / L[i, i];
        }
    }

    static void Recomposition(float[,] A, float[,] L, float[] d)
    {
        float[,] test = new float[d.Length, d.Length];
        for (int i = 0; i < L.GetLength(0); i++)
        {
            for (int j = 0; j < L.GetLength(1); j++)
            {
                test[i, j] = d[i] * L[j, i];
            }
        }
        for (int i = 0; i < L.GetLength(0); i++)
        {
            for (int j = 0; j < L.GetLength(1); j++)
            {
                A[i, j] = 0f;
                for (int k = 0; k < L.GetLength(1); k++)
                {
                    A[i, j] += L[i, k] * test[k, j];
                }
            }
        }
    }
    public static float DotProduct(float[] a, float[] b)
    {
        float x = 0;
        for (int i = 0; i < a.GetLength(0); i++)
        {
            x += a[i] * b[i];
        }
        return x;
    }
    public static float[] SimpleProduct(float[] a, float[] b)
    {
        float[] x = new float[a.Length];
        for (int i = 0; i < a.Length; i++)
        {
            x[i] = a[i] * b[i];
        }
        return x;
    }
    public static void DyadProduct(float[] a, float[] b, float[,] M)
    {
        for (int i = 0; i < a.GetLength(0); i++)
        {
            for (int j = 0; j < b.GetLength(0); j++)
            {
                M[i, j] = a[i] * b[j];
            }
        }
    }

    public static float[,] identity3() {
        float[,] identity = new float[3, 3];
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                identity[i, j] = i == j ? 1 : 0;
        return identity;

    }
    public static void jacobi3(float[,] M, float[] r, float[,] v) {
        float value;
        float alpha, beta, gamma, cos, sin;
        float[,] P = identity3();
        float[,] Q;
        float[,] A = new float[3, 3];
        int p; int q;

        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                A[i, j] = M[i, j];
        int count = 0;
        do {
            p = 1; q = 2;
            value = Mathf.Abs(A[1, 2]);
            for (int i = 0; i < 3; i++)
                for (int j = i + 1; j < 3; j++)
                    if (value < Mathf.Abs(A[i, j]))
                    {
                        p = i; q = j;
                        value = Mathf.Abs(A[i, j]);
                    }
            if (value < 0.001f)
                break;
            alpha = (A[p, p] - A[q, q]) / 2;
            beta = - A[p, q];
            gamma = Mathf.Abs(alpha) / Mathf.Sqrt(alpha * alpha + beta * beta);
            cos = Mathf.Sqrt((1 + gamma) / 2); sin = Mathf.Sqrt((1 - gamma) / 2) * Mathf.Sign(alpha * beta);
            Q = identity3();
            Q[p, p] = cos; Q[q, q] = cos;
            Q[p, q] = sin; Q[q, p] = -sin;

            P = MatxMat(P, Q);
            A = MatTxMat(Q, A);
            A = MatxMat(A, Q);
            count++;
        } while (count < 100);

        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++) {
                v[i, j] = P[i, j];
        }
        r[0] = A[0, 0]; r[1] = A[1, 1]; r[2] = A[2, 2];
    }

    public static void MatxVec(float[,] M, float[] V, float[] x)
    {
        for (int i = 0; i < M.GetLength(0); i++)
        {
            x[i] = 0;
            for (int j = 0; j < M.GetLength(1); j++)
            {
                x[i] += M[i, j] * V[j];
            }
        }
    }
    public static float[] MatxVec(float[,] M, float[] V)
    {
        float[] x = new float[M.GetLength(0)];
        for (int i = 0; i < M.GetLength(0); i++)
        {
            x[i] = 0;
            for (int j = 0; j < M.GetLength(1); j++)
            {
                x[i] += M[i, j] * V[j];
            }
        }
        return x;
    }
    public static Vector3 MatxVec(float[,] M, Vector3 V)
    {
        float[] VArray = { V.x, V.y, V.z };

        VArray = MatxVec(M, VArray);
        return new Vector3(VArray[0], VArray[1], VArray[2]);
    }
    public static void MatTxVec(float[,] M, float[] V, float[] x)
    {
        for (int i = 0; i < M.GetLength(1); i++)
        {
            x[i] = 0;
            for (int j = 0; j < M.GetLength(0); j++)
            {
                x[i] += M[j, i] * V[j];
            }
        }
    }
    public static float[] MatTxVec(float[,] M, float[] V)
    {
        float[] x = new float[M.GetLength(1)];
        for (int i = 0; i < M.GetLength(1); i++)
        {
            x[i] = 0;
            for (int j = 0; j < M.GetLength(0); j++)
            {
                x[i] += M[j, i] * V[j];
            }
        }
        return x;
    }
    public static Vector3 MatTxVec(float[,] M, Vector3 V)
    {
        float[] VArray = { V.x, V.y, V.z };

        VArray = MatTxVec(M, VArray);
        return new Vector3(VArray[0], VArray[1], VArray[2]);
    }
    public static float[,] MatxSca(float[,] M, float v)
    {
        float[,] x = new float[M.GetLength(0), M.GetLength(1)];
        for (int i = 0; i < M.GetLength(1); i++)
        {
            for (int j = 0; j < M.GetLength(0); j++)
            {
                x[i, j] = M[i, j] * v;
            }
        }
        return x;
    }
    public static void MatTxColumnVec(float[,] M, float[,] V, int index, float[] x)
    {
        for (int i = 0; i < M.GetLength(1); i++)
        {
            x[i] = 0;
            for (int j = 0; j < M.GetLength(0); j++)
            {
                x[i] += M[j, i] * V[index, j];
            }
        }
    }
    public static float[] MatTxColumnVec(float[,] M, float[,] V, int index)
    {
        float[] x = new float[M.GetLength(1)];
        for (int i = 0; i < M.GetLength(1); i++)
        {
            x[i] = 0;
            for (int j = 0; j < M.GetLength(0); j++)
            {
                x[i] += M[j, i] * V[index, j];
            }
        }
        return x;
    }
    public static void MatxMat(float[,] A, float[,] B, float[,] X)
    {
        if (A.GetLength(1) != B.GetLength(0))
            Debug.Log("The product of these matrixes is not defined.");
        for (int i = 0; i < A.GetLength(0); i++)
        {
            for (int j = 0; j < B.GetLength(1); j++)
            {
                X[i, j] = 0;
                for (int k = 0; k < A.GetLength(1); k++)
                {
                    X[i, j] += A[i, k] * B[k, j];
                }
            }
        }
    }
    public static float[,] MatxMat(float[,] A, float[,] B)
    {
        float[,] X = new float[A.GetLength(0), B.GetLength(1)];
        if (A.GetLength(1) != B.GetLength(0))
            Debug.Log("The product of these matrixes is not defined.");
        for (int i = 0; i < A.GetLength(0); i++)
        {
            for (int j = 0; j < B.GetLength(1); j++)
            {
                X[i, j] = 0;
                for (int k = 0; k < A.GetLength(1); k++)
                {
                    X[i, j] += A[i, k] * B[k, j];
                }
            }
        }
        return X; 
    }
    public static void MatTxMat(float[,] A, float[,] B, float[,] X)
    {
        if (A.GetLength(0) != B.GetLength(0))
            Debug.Log("The product of these matrixes is not defined.");
        for (int i = 0; i < A.GetLength(1); i++)
        {
            for (int j = 0; j < B.GetLength(1); j++)
            {
                X[i, j] = 0;
                for (int k = 0; k < A.GetLength(0); k++)
                {
                    X[i, j] += A[k, i] * B[k, j];
                }
            }
        }
    }
    public static float[,] MatTxMat(float[,] A, float[,] B)
    {
        float[,] X = new float[A.GetLength(1), B.GetLength(1)];
        if (A.GetLength(0) != B.GetLength(0))
            Debug.Log("The product of these matrixes is not defined.");
        for (int i = 0; i < A.GetLength(1); i++)
        {
            for (int j = 0; j < B.GetLength(1); j++)
            {
                X[i, j] = 0;
                for (int k = 0; k < A.GetLength(0); k++)
                {
                    X[i, j] += A[k, i] * B[k, j];
                }
            }
        }
        return X;
    }
    public static float[,] AddMat(float[,] A, float[,] B)
    {
        float[,] X = new float[A.GetLength(0), A.GetLength(1)];
        for (int i = 0; i < A.GetLength(1); i++)
        {
            for (int j = 0; j < B.GetLength(1); j++)
            {
                X[i, j] = A[i, j] + B[i, j];
            }
        }
        return X;
    }
    public static float[,] Transpose(float[,] A)
    {
        float[,] At = new float[A.GetLength(1), A.GetLength(0)];
        for (int i = 0; i < A.GetLength(1); i++)
        {
            for (int j = 0; j < A.GetLength(0); j++)
            {
                At[i, j] = A[j, i];
            }
        }
        return At;
    }
    public static void FWSubstitution(float[,] L, float[] b, float[] x)
    {
        for (int i = 0; i < L.GetLength(0); i++)
        {
            x[i] = b[i];
            for (int j = 0; j < i; j++)
            {
                x[i] -= L[i, j] * x[j];
            }
            x[i] /= L[i, i];
        }
    }
    public static float[] FWSubstitution(SPMatrix L, float[] b)
    {
        float[] x = new float[L.Size1];
        for (int i = 0; i < L.Size1; i++)
        {
            x[i] = b[i];
            for (int j = L.IA[i]; j < L.IA[i + 1] - 1; j++)
            {
                x[i] -= L.A[j] * x[L.JA[j]];
            }
            x[i] /= L.A[L.IA[i + 1] - 1];
        }
        return x;
    }
    public static void BWSubstitution(float[,] U, float[] b, float[] x)
    {
        for (int i = U.GetLength(0) - 1; i > -1; i--)
        {
            x[i] = b[i];
            for (int j = U.GetLength(1) - 1; j > i; j--)
            {
                x[i] -= U[i, j] * x[j];
            }
            x[i] /= U[i, i];
        }
    }
    public static float[] BWSubstitution(SPMatrix U, float[] b)
    {
        float[] x = new float[U.Size1];
        for (int i = U.Size1 - 1; i > -1; i--)
        {
            x[i] = b[i];
            for (int j = U.IA[i + 1] - 1; j > U.IA[i]; j--)
            {
                x[i] -= U.A[j] * x[U.JA[j]];
            }
            x[i] /= U.A[U.IA[i]];
        }
        return x;
    }
    public static void ICRes(float[,] L, float[,] U, float[] b, float[] x)
    {
        float[] tmp = new float[b.Length];
        FWSubstitution(L, b, tmp);
        BWSubstitution(U, tmp, x);
    }
    public static float[] ICRes(SPMatrix L, SPMatrix U, float[] b)
    {
        float[] x = new float[b.Length];
        x = FWSubstitution(L, b);
        return BWSubstitution(U, x);
    }

    static public void ICCG(float[,] A, float[] b, float[] x)
    {
        float[] r = new float[x.Length];
        float[] p = new float[x.Length];
        float[] tmp = new float[x.Length];
        float[] y = new float[x.Length];
        float[] r2 = new float[x.Length];
        float[,] L = new float[x.Length, x.Length];
        float[] d = new float[x.Length];
        float[,] DLT = new float[x.Length, x.Length];

        for (int i = 0; i < r.GetLength(0); i++)
        {
            r[i] = b[i];

            for (int j = 0; j < A.GetLength(1); j++)
            {
                r[i] -= A[i, j] * x[j];
            }
        }
        
        sw.Start();
        Decomposition(A, L, d);

        sw.Stop();
        //Debug.Log("IC: " + sw.Elapsed + " / " + (100 * sw.Elapsed.TotalMilliseconds / sw.Elapsed.TotalMilliseconds) + " %");
        
        for (int i = 0; i < L.GetLength(0); i++)
        {
            for (int j = 0; j < L.GetLength(1); j++)
            {
                DLT[i, j] = L[j, i] * d[i];
            }
        }
        sw.Restart();
        ICRes(L, DLT, r, p);

        sw.Stop();
        //Debug.Log("ICRec: " + sw.Elapsed + " / " + (100 * sw.Elapsed.TotalMilliseconds / sw.Elapsed.TotalMilliseconds) + " %");

        float alpha;
        float beta;
        float rr0 = DotProduct(r, p);
        float rr1;

        sw.Restart();
        if (DotProduct(r, r) > e) // 最初から解として適切ならCG法を適用しない（0割を回避）
        for (int i = 0; i < iterate; i++)
        {
            MatxVec(A, p, y);

            alpha = rr0 / DotProduct(p, y);

            for (int j = 0; j < x.Length; j++)
            {
                x[j] += alpha * p[j];
                r[j] -= alpha * y[j];
            }
            if (DotProduct(r, r) < e) break;
            
            ICRes(L, DLT, r, r2);
            rr1 = DotProduct(r, r2);

            beta = rr1 / rr0;

            for (int j = 0; j < x.Length; j++)
            {
                p[j] = r2[j] + p[j] * beta;
            }
            rr0 = rr1;
        }
        sw.Stop();
        //Debug.Log("CG: " + sw.Elapsed + " / " + (100 * sw.Elapsed.TotalMilliseconds / sw.Elapsed.TotalMilliseconds) + " %");
    }

    static public float[] ICCG(SPMatrix A, float[] b)
    {
        int size = b.Length;
        float[] x = new float[size];
        float[] r = new float[size];
        float[] p = new float[size];
        float[] tmp = new float[size];
        float[] y = new float[size];
        float[] r2 = new float[size];
        float[] d = new float[size];
        SPMatrix L;
        SPMatrix DLT;
        for (int i = 0; i < r.GetLength(0); i++)
        {
            r[i] = b[i];

            for (int j = A.IA[i]; j < A.IA[i + 1]; j++)
            {
                r[i] -= A.A[j] * x[A.JA[j]];
            }
        }
        
        L = Decomposition(A, ref d);

        //Debug.Log("IC: " + sw.Elapsed + " / " + (100 * sw.Elapsed.TotalMilliseconds / sw.Elapsed.TotalMilliseconds) + " %");

        DLT = new SPMatrix(L);
        for (int i = 0; i < size; i++)
        {
            for (int j = L.IA[i]; j < L.IA[i + 1]; j++)
            {
                DLT.A[j] *= d[L.JA[j]];
            }
        }
        
        DLT = DLT.Transpose();
        sw.Restart();

        p = ICRes(L, DLT, r);

        sw.Stop();
        //Debug.Log("ICRec: " + sw.Elapsed + " / " + (100 * sw.Elapsed.TotalMilliseconds / sw.Elapsed.TotalMilliseconds) + " %");
        float alpha;
        float beta;
        float rr0 = DotProduct(r, p);
        float rr1;

        sw.Restart();
        if (DotProduct(r, r) > e) // 最初から解として適切ならCG法を適用しない（0割を回避）
            for (int i = 0; i < iterate; i++)
            {
                y = A.LinearConversion(p);

                alpha = rr0 / DotProduct(p, y);

                for (int j = 0; j < x.Length; j++)
                {
                    x[j] += alpha * p[j];
                    r[j] -= alpha * y[j];
                }
                if (DotProduct(r, r) < e) break;

                r2 = ICRes(L, DLT, r);
                rr1 = DotProduct(r, r2);

                beta = rr1 / rr0;

                for (int j = 0; j < x.Length; j++)
                {
                    p[j] = r2[j] + p[j] * beta;
                }
                rr0 = rr1;
            }
        sw.Stop();
        //Debug.Log("CG: " + sw.Elapsed + " / " + (100 * sw.Elapsed.TotalMilliseconds / sw.Elapsed.TotalMilliseconds) + " %");
        return x;
    }
    void CCDecomposition(float[,] A, float[] b, float[] x)
    {
        float[] y = new float[x.Length];
        float[,] L = new float[x.Length, x.Length];
        float[] d = new float[x.Length];

        CDecomposition(A, L, d);
        FWSubstitution(L, b, y);
  
        for (int i = L.GetLength(0) - 1; i > -1; i--)
        {
            x[i] = y[i];
            for (int j = L.GetLength(1) - 1; j > i; j--)
            {
                x[i] -= L[j, i] * d[i] * x[j];
            }
            x[i] /= L[i, i] * d[i];
        }
    }
    public static void PrintArray(float[,] A) {
        for (int i = 0; i < A.GetLength(0); i++)
        {
            string str = "|";
            for (int j = 0; j < A.GetLength(1); j++)
            {
                str += A[i, j];
                if (j < A.GetLength(1) - 1)
                    str += ", ";
                else str += "|";
            }
            Debug.Log(str);
        }
    }
    public static bool IsSymetric(float[,] A)
    {
        bool sym = true;
        for (int i = 0; i < A.GetLength(0); i++)
        {
            for (int j = i + 1; j < A.GetLength(1); j++)
            {
                if (A[i, j] != A[j, i])
                {
                    sym = false;
                    Debug.Log( "A["+ i + ", " + j + "] : " + A[i, j] + ", " + "A[" + j + ", " + i + "] : " + A[j, i]);
                    continue;
                }
            }
        }
        return sym;
    }
    public static void PrintVector(float[] V)
    {
        string str = "(";
        for (int i = 0; i < V.Length; i++)
        {
            str += V[i];
            if (i < V.Length - 1)
                str += ", ";
            else str += ")";
        }
        Debug.Log(str);
    }
    public static void PrintVector(int[] V)
    {
        string str = "(";
        for (int i = 0; i < V.Length; i++)
        {
            str += V[i];
            if (i < V.Length - 1)
                str += ", ";
            else str += ")";
        }
        Debug.Log(str);
    }
    public static void PrintList(List<float> L)
    {
        string str = "(";
        for (int i = 0; i < L.Count; i++)
        {
            str += L[i].ToString("F2");
            if (i < L.Count - 1)
                str += ", ";
            else str += ")";
        }
        Debug.Log(str);
    }
    public static void PrintList(List<int> L)
    {
        string str = "(";
        for (int i = 0; i < L.Count; i++)
        {
            str += L[i];
            if (i < L.Count - 1)
                str += ", ";
            else str += ")";
        }
        Debug.Log(str);
    }

    public static Quaternion transformRotMatToQuaternion(float[,] R)
    {   float[] elem = new float[4]; // 0:x, 1:y, 2:z, 3:w
        elem[0] =   R[0, 0] - R[1, 1] - R[2, 2] + 1.0f;
        elem[1] = - R[0, 0] + R[1, 1] - R[2, 2] + 1.0f;
        elem[2] = - R[0, 0] - R[1, 1] + R[2, 2] + 1.0f;
        elem[3] =   R[0, 0] + R[1, 1] + R[2, 2] + 1.0f;

        int biggestIndex = 0;
        for (int i = 1; i < 4; i++)
        {
            if (elem[i] > elem[biggestIndex])
                biggestIndex = i;
        }

        float[] q = new float[4];
        float v = Mathf.Sqrt(elem[biggestIndex]) * 0.5f;
        q[biggestIndex] = v;
        float mult = 0.25f / v;

        switch (biggestIndex)
        {
            case 0: // x
                q[1] = (R[0, 1] + R[1, 0]) * mult;
                q[2] = (R[2, 0] + R[0, 2]) * mult;
                q[3] = -(R[1, 2] - R[2, 1]) * mult;
                break;
            case 1: // y
                q[0] = (R[0, 1] + R[1, 0]) * mult;
                q[2] = (R[1, 2] + R[2, 1]) * mult;
                q[3] = -(R[2, 0] - R[0, 2]) * mult;
                break;
            case 2: // z
                q[0] = (R[2, 0] + R[0, 2]) * mult;
                q[1] = (R[1, 2] + R[2, 1]) * mult;
                q[3] = -(R[0, 1] - R[1, 0]) * mult;
                break;
            case 3: // w
                q[0] = -(R[1, 2] - R[2, 1]) * mult;
                q[1] = -(R[2, 0] - R[0, 2]) * mult;
                q[2] = -(R[0, 1] - R[1, 0]) * mult;
                break;
        }
        
        return new Quaternion(q[0], q[1], q[2], q[3]);
    }
}
