using UnityEngine;
using Unity.Mathematics;

public class ParticleSpawner2D : MonoBehaviour
{

    [Header("Particle Spawning Settings")]
    public int particleCount = 100;
    public float spawnRadius = 5f;
    

    [Header ("particle Mesh Settings")]
    public float particleRadius = 1f;
    [Range(4,64)]
    public int segments = 32;


    public InitialParticleData GetInitialData() 
    {     
        float3[] allPos = new float3[particleCount];
        float3[] allVel = new float3[particleCount];

        for (int i = 0; i < particleCount; i++)
        {
            Vector2 rand = UnityEngine.Random.insideUnitCircle * spawnRadius;
            allPos[i] = new float3(rand.x, rand.y, 0);
            allVel[i] = new float3(UnityEngine.Random.Range(-1f,1f), 0, 0);
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
