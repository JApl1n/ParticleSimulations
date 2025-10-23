using UnityEngine;
using Unity.Mathematics;
using System;

public class ParticleSpawner2D : MonoBehaviour
{

    [Header("Particle Spawning Settings")]
    public int particleCount = 100;
    public Vector2 spawnCentre;
    

    [Header ("particle Mesh Settings")]
    public float particleRadius = 1f;
    [Range(4,64)]
    public int segments = 32;


    public InitialParticleData GetInitialData() 
    {     
        float3[] allPos = new float3[particleCount];
        float3[] allVel = new float3[particleCount];

        int root = (int)Math.Ceiling(Mathf.Sqrt(particleCount));
        int total = 0;

        for (int i = 0; i < root; i++)
        {
            for (int j = 0; j < root; j++) 
            {
                if (total>=particleCount) {continue;} else {
                    allPos[total] = new float3(spawnCentre.x + j*2.5f*particleRadius, spawnCentre.y + i*2.5f*particleRadius, 0);
                    allVel[total] = new float3(i, j, 0);
                    total++;
                }
            }
        }

        InitialParticleData data = new()
		{
			positions = allPos,
			velocities = allVel,
		};

        return data;
    }

    public struct InitialParticleData
	{
		public float3[] positions;
		public float3[] velocities;

		public InitialParticleData(int size)
		{
			positions = new float3[size];
			velocities = new float3[size];
		}
	}


    public Mesh MakeCircleMesh()
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[segments + 1];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * particleRadius;

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
