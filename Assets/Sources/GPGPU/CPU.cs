using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter))]
public class CPU : MonoBehaviour
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    struct DisplaceVertex
    {
        public Vector3 position;
        public Vector3 normal;
    }

    [SerializeField] ComputeShader compute;
    Mesh originalMesh, displacedMesh;
    GraphicsBuffer originalIndexBuffer;
    GraphicsBuffer originalVertexBuffer;
    GraphicsBuffer displacedVertexBuffer;

    void Awake()
    {
        var meshFilter = GetComponent<MeshFilter>();
        originalMesh = CreateMesh(meshFilter.sharedMesh);
        displacedMesh = CreateMesh(meshFilter.sharedMesh);
        originalVertexBuffer = originalMesh.GetVertexBuffer(0);
        originalIndexBuffer = originalMesh.GetIndexBuffer();
        displacedVertexBuffer = displacedMesh.GetVertexBuffer(0);
        meshFilter.mesh = displacedMesh;
    }

    Mesh CreateMesh(Mesh original)
    {
        var originalTriangles = original.triangles;
        var originalPositions = original.vertices;
        var originalNormals = original.normals;

        var mesh = new Mesh();
        mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
        mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        var pDesc = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3);
        var nDesc = new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3);
        mesh.SetVertexBufferParams(original.vertexCount, pDesc, nDesc);
        mesh.SetIndexBufferParams(originalTriangles.Length, IndexFormat.UInt32);
        mesh.SetSubMesh(0, new SubMeshDescriptor(0, originalTriangles.Length), MeshUpdateFlags.DontRecalculateBounds);
        mesh.SetIndexBufferData(originalTriangles, 0, 0, originalTriangles.Length);
        var vertices = new DisplaceVertex[original.vertexCount];
        for (var i = 0; i < original.vertexCount; ++i)
        {
            vertices[i].position = originalPositions[i];
            vertices[i].normal = originalNormals[i];
        }
        mesh.SetVertexBufferData(vertices, 0, 0, original.vertexCount);
        mesh.bounds = original.bounds;
        return mesh;
    }

    void OnDestroy()
    {
        originalVertexBuffer?.Dispose();
        originalVertexBuffer = null;
        originalIndexBuffer?.Dispose();
        originalIndexBuffer = null;
        displacedVertexBuffer?.Dispose();
        displacedVertexBuffer = null;
        Destroy(originalMesh);
        originalMesh = null;
        Destroy(displacedMesh);
        displacedMesh = null;
    }

    void Update()
    {
        DisplaceMesh();
    }

    void DisplaceMesh()
    {
        compute.SetFloat("Time", Time.time);
        compute.SetBuffer(0, "OriginalIndices", originalIndexBuffer);
        compute.SetBuffer(0, "OriginalVertices", originalVertexBuffer);
        compute.SetBuffer(0, "DisplacedVertices", displacedVertexBuffer);
        DispatchThreads(0, originalMesh.triangles.Length / 3);
    }

    void DispatchThreads(int kernel, int count)
    {
        uint x, y, z;
        compute.GetKernelThreadGroupSizes(kernel, out x, out y, out z);
        var groups = (count + (int)x - 1) / (int)x;
        compute.Dispatch(kernel, groups, 1, 1);
    }
}