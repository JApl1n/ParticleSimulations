using UnityEngine;
using System;

public class ParticleSim2D : MonoBehaviour
{
    [Header("References")]
    public ComputeShader computeShader;
    public Shader renderShader;

    [Header ("Material Settings")]
    public float maxSpeed;

    [Header ("Obstacle Settings")]
    public Vector2 boundsSize;
    public Vector2 obstacleSize;
	public Vector2 obstacleCentre;

    [Header ("Physics Settings")]
    public float collisionDamping = 0.9f;
    public float gravity = -9.8f;
    public float particleRadius = 0.1f;
    private float sigma = 1.0f;  // Distance to sign swap of LJ potential (in terms of particle radius)
    public float epsilon = 1.0f;  // Strength of LJ potential well 
    public float forceCutoff = 4.0f;  // Distance away from particle to check in terms of radius
    // This bool allows the user to choose between a force repulsion between particles and 
    // exclusion principle
    public bool forceCollisions = true;

    [Header ("Script References")]
    public ParticleSpawner2D particleSpawner2D;

    GraphicsBuffer particleBuffer;
    GraphicsBuffer velocityBuffer;
    GraphicsBuffer commandBuffer;

    GraphicsBuffer.IndirectDrawIndexedArgs[] commandData;
    const int commandCount = 1;

    int kernelHandle;
    Bounds bounds;


    ParticleSpawner2D.InitialParticleData initialData;

    private Mesh particleMesh;
    private Material material;

    private int particleCount;

    void Start()
    {
        InitializeSystem();
        
    }

    void Update()
    {
        // Evolve compute shader
        computeShader.SetFloat("_deltaTime", Time.deltaTime);
        // Run compute shader
        // division splits work into groups of 256
        computeShader.Dispatch(kernelHandle, Mathf.CeilToInt(particleCount / 256f), 1, 1);  

        // Render particles
        RenderParams rp = new RenderParams(material);
        rp.worldBounds = bounds;
        Graphics.RenderMeshIndirect(rp, particleMesh, commandBuffer, commandCount);
    }

    void InitializeSystem() 
    {
        material = new Material(renderShader);
        particleMesh = particleSpawner2D.MakeCircleMesh();
        initialData = particleSpawner2D.GetInitialData();
        particleCount = initialData.velocities.Length;
        sigma = particleRadius / (float)Math.Pow(2, 1.0/6.0);
        forceCutoff *= particleRadius;

        // Allocate GPU buffers
        particleBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, particleCount, sizeof(float) * 3);
        velocityBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, particleCount, sizeof(float) * 3);

        particleBuffer.SetData(initialData.positions);
        velocityBuffer.SetData(initialData.velocities);

        // Setup compute shader, pass through values
        kernelHandle = computeShader.FindKernel("CSMain");
        computeShader.SetInt("_particleCount", particleCount);
        computeShader.SetFloat("_particleRadius", particleRadius);
        computeShader.SetFloat("_collisionDamping", collisionDamping);
        computeShader.SetBool("_forceCollisions", forceCollisions);
        computeShader.SetFloat("_epsilon", epsilon);
        computeShader.SetFloat("_sigma", sigma);
        computeShader.SetFloat("_forceCutoff", forceCutoff);
        computeShader.SetFloat("_gravity", gravity);
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

}
