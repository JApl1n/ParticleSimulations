using UnityEngine;

public class ParticleSim2D : MonoBehaviour
{
    [Header("References")]
    public ComputeShader computeShader;
    public Shader renderShader;

    [Header ("Material Settings")]
    public float maxSpeed;

    [Header("Particle Settings")]
    public int particleCount = 100;
    public float spawnRadius = 5f;
    public float particleRadius = 1f;
    [Range(4,64)]
    public int particleResolution = 32;

    [Header ("Obstacle Settings")]
    public Vector2 boundsSize;
    public Vector2 obstacleSize;
	public Vector2 obstacleCentre;

    GraphicsBuffer particleBuffer;
    GraphicsBuffer velocityBuffer;
    GraphicsBuffer commandBuffer;

    GraphicsBuffer.IndirectDrawIndexedArgs[] commandData;
    const int commandCount = 1;

    int kernelHandle;
    Bounds bounds;

    struct ParticleData { public Vector3 position; }
    struct VelocityData { public Vector3 velocity; }

    private Mesh particleMesh;
    private Material material;

    void Start()
    {
        material = new Material(renderShader);

        particleMesh = MakeCircleMesh(particleResolution, particleRadius*0.25f);
        spawnRadius *= 0.25f;

        // Allocate GPU buffers
        particleBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, particleCount, sizeof(float) * 3);
        velocityBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, particleCount, sizeof(float) * 3);

        Vector3[] positions = new Vector3[particleCount];
        Vector3[] velocities = new Vector3[particleCount];

        for (int i = 0; i < particleCount; i++)
        {
            Vector2 rand = Random.insideUnitCircle * spawnRadius;
            positions[i] = new Vector3(rand.x, rand.y, 0);
            velocities[i] = new Vector3(Random.Range(-5f,5f), 0, 0);
        }

        particleBuffer.SetData(positions);
        velocityBuffer.SetData(velocities);

        // Setup compute shader, pass through values
        kernelHandle = computeShader.FindKernel("CSMain");
        computeShader.SetInt("_particleCount", particleCount);
        computeShader.SetVector("_boundsSize", boundsSize);
        computeShader.SetVector("_obstacleSize", obstacleSize);
        computeShader.SetVector("_obstacleCentre", obstacleCentre);


        computeShader.SetBuffer(kernelHandle, "_particlePositions", particleBuffer);
        computeShader.SetBuffer(kernelHandle, "_particleVelocities", velocityBuffer);

        // Setup indirect command buffer
        commandBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, commandCount, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[commandCount];

        commandData[0].indexCountPerInstance = particleMesh.GetIndexCount(0);
        commandData[0].instanceCount = (uint)particleCount;
        commandData[0].startIndex = particleMesh.GetIndexStart(0);
        commandData[0].baseVertexIndex = particleMesh.GetBaseVertex(0);
        commandData[0].startInstance = 0;
        commandBuffer.SetData(commandData);

  
        // Rendering setup
        material.SetBuffer("_particlePositions", particleBuffer);
        material.SetBuffer("_particleVelocities", velocityBuffer);
        material.SetFloat("_maxSpeed", maxSpeed);

        bounds = new Bounds(Vector3.zero, Vector3.one * 100);  // Set to a very large size
    }

    void Update()
    {
        // Evolve compute shader
        computeShader.SetFloat("_deltaTime", Time.deltaTime);
        computeShader.Dispatch(kernelHandle, 1, 1, 1);  // Split work among work groups here

        // Render particles
        RenderParams rp = new RenderParams(material);
        rp.worldBounds = bounds;
        Graphics.RenderMeshIndirect(rp, particleMesh, commandBuffer, commandCount);
    }

    void OnDestroy()
    {
        particleBuffer?.Release();
        velocityBuffer?.Release();
        commandBuffer?.Release();
    }


    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 1, 0, 0.5f);
        Gizmos.DrawWireCube(Vector2.zero, boundsSize);
        Gizmos.DrawWireCube(obstacleCentre, obstacleSize);
    }


    Mesh MakeCircleMesh(int segments, float radius)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[segments + 1];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;

            int tri = i * 3;
            triangles[tri] = 0;
            triangles[tri + 1] = (i + 2 > segments) ? 1 : i + 2;
            triangles[tri + 2] = i + 1;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }
}
