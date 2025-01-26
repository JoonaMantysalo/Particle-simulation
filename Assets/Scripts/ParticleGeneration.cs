using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEditor;
using UnityEngine;
using static UnityEngine.ParticleSystem;

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

    int particleCount = 0;

    private void OnValidate()
    {
        SimulationController simController = GetComponent<SimulationController>(); 
        simController.particles = Generate(simController.particleSize);
    }

    public List<Particle> Generate(float particleSize)
    {
        List<Particle> particles = new List<Particle>();
        if (spawnType == SpawnType.AllAtOnce)
        {
            if (particlePrefab == null || numOfRows <= 0 || particlesPerRow <= 0)
            {
                Debug.LogWarning("Invalid configuration in ParticleGeneration");
                return null;
            }

            // Defer the cleanup and generation process to avoid conflicts
            EditorApplication.delayCall += () =>
            {
                if (this == null) return;

                ClearGeneratedParticles(particles);

                GenerateParticlesAll(particles, particleSize);
            };
        }
        if (spawnType == SpawnType.OneByOne)
        {
            // Defer the cleanup and generation process to avoid conflicts
            EditorApplication.delayCall += () =>
            {
                if (this == null) return;

                ClearGeneratedParticles(particles);
                GenerateParticle(particles, particleSize);
            };
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

    public List<Particle> GenerateParticlesAll(List<Particle> particles, float particleSize)
    {
        for (int i = 0; i < numOfRows; i++)
        {
            for (int j = 0; j < particlesPerRow; j++)
            {
                float jitterX = Random.Range(-(density - 1) / 2, (density - 1) / 2) / 10;
                float jitterY = Random.Range(-(density - 1) / 2, (density - 1) / 2) / 10;

                Vector3 localPosition = new Vector3(
                    (j * particleSize * density + spawnPosition.x + jitterX) / transform.localScale.x,
                    (i * particleSize * density + spawnPosition.y + jitterY) / transform.localScale.y,
                    0);

                GameObject newParticleObject = Instantiate(particlePrefab, transform);
                newParticleObject.transform.localPosition = localPosition;
                newParticleObject.name = $"Particle ({i}, {j})";

                Vector3 localSize = new Vector3(
                    1 / transform.localScale.x,
                    1 / transform.localScale.y,
                    1 / transform.localScale.z
                );
                Particle newParticle = newParticleObject.GetComponent<Particle>();
                newParticle.Initialize(localSize * particleSize, initialVelocity);
                particles.Add(newParticle);
            }
        }
        return particles;
    }

    public void GenerateParticle(List<Particle> particles, float particleSize)
    {
        GameObject newParticleObject = Instantiate(particlePrefab, transform);
        newParticleObject.transform.position = spawnPosition;
        newParticleObject.name = $"Particle ({particleCount})";
        particleCount++;

        Vector3 localSize = new Vector3(
            1 / transform.localScale.x,
            1 / transform.localScale.y,
            1 / transform.localScale.z
        );
        Particle newParticle = newParticleObject.GetComponent<Particle>();
        newParticle.Initialize(localSize * particleSize, initialVelocity);
        particles.Add(newParticle);
    }
}
