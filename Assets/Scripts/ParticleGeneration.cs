using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ParticleGeneration : MonoBehaviour
{
    public GameObject particlePrefab;
    public SpawnType spawnType;
    public int particlesPerRow;
    public int numOfRows;
    public float spawnRate;
    public Vector2 spawnPosition;
    [Range(1.0f, 3.0f)]
    public float density;
    public Vector2 initialVelocity;

    public Vector2 localParticleSize;
    float particleRadius;
    int particleCount = 0;

    private void OnValidate()
    {
        SimulationController simController = GetComponent<SimulationController>();
        particleRadius = simController.particleSize;
        localParticleSize = new Vector2(
            1 / transform.localScale.x,
            1 / transform.localScale.y
            ) * particleRadius;
        EditorApplication.delayCall += () =>
        {
            //Generate();
        };
    }

    public List<Particle> Generate()
    {
        List<Particle> particles = new List<Particle>();
        if (spawnType == SpawnType.AllAtOnce)
        {
            if (particlePrefab == null || numOfRows <= 0 || particlesPerRow <= 0)
            {
                Debug.LogWarning("Invalid configuration in ParticleGeneration");
                return null;
            }


            ClearGeneratedParticles(particles);

            GenerateParticlesAll(particles);
        }
        if (spawnType == SpawnType.OneByOne)
        {
            ClearGeneratedParticles(particles);
            particles.Add(GenerateParticle());
            
        }
        return particles;
        
    }

    void ClearGeneratedParticles(List<Particle> particles)
    {
        // Destroy all child GameObjects
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
        particles.Clear();
    }

    public List<Particle> GenerateParticlesAll(List<Particle> particles)
    {
        for (int i = 0; i < numOfRows; i++)
        {
            for (int j = 0; j < particlesPerRow; j++)
            {
                float jitterX = Random.Range(-(density - 1) / 2, (density - 1) / 2) / 10;
                float jitterY = Random.Range(-(density - 1) / 2, (density - 1) / 2) / 10;

                Vector3 localPosition = new Vector3(
                    (j * particleRadius * density + spawnPosition.x + jitterX) / transform.localScale.x,
                    (i * particleRadius * density + spawnPosition.y + jitterY) / transform.localScale.y,
                    0);

                GameObject newParticleObject = Instantiate(particlePrefab, transform);
                newParticleObject.transform.localPosition = localPosition;
                newParticleObject.name = $"Particle ({i}, {j})";
                particleCount++;

                Particle newParticle = newParticleObject.GetComponent<Particle>();
                newParticle.Initialize(localParticleSize, initialVelocity);
                particles.Add(newParticle);
            }
        }
        return particles;
    }

    public Particle GenerateParticle()
    {
        GameObject newParticleObject = Instantiate(particlePrefab, transform);
        newParticleObject.transform.position = spawnPosition;
        newParticleObject.name = $"Particle ({particleCount})";
        particleCount++;

        Particle newParticle = newParticleObject.GetComponent<Particle>();
        newParticle.Initialize(localParticleSize, initialVelocity);
        return newParticle;
    }
}
