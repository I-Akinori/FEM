using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class ParallelSample : MonoBehaviour
{
    static private System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
    // Start is called before the first frame update
    void Start()
    {
        int size = 1000 * 1000 * 1000;
        float[] Array = new float[size];
        sw.Start();
        for (int i = 0; i < size; i++) {
            Array[i] = i;
        }
        sw.Stop();
        Debug.Log("Single: " + sw.Elapsed + " / " + (100 * sw.Elapsed.TotalMilliseconds / sw.Elapsed.TotalMilliseconds) + " %");

        sw.Restart();

        Parallel.For(0, size, i =>
       {
           Array[i] = i;
       });
        sw.Stop();
        Debug.Log("Parallel: " + sw.Elapsed + " / " + (100 * sw.Elapsed.TotalMilliseconds / sw.Elapsed.TotalMilliseconds) + " %");

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
