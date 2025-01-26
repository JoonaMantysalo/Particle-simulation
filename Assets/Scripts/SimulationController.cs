
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class SimulationController : MonoBehaviour
{
    public GameObject particlePrefab;
    public SpawnType spawnType;
    public int particlesPerRow;
    public int numOfRows;
    public float spawnRate;
    public Vector2 spawnPosition;
    public float particleSize;
    [Range(1.0f, 3.0f)]
    public float density;

    public float gravity;
    [Range(0f, 1f)]
    public float resistance;
    public Vector2 initialVelocity;

    public ContainerType container; 
    public Vector2 containerSizeRec;
    public float containerSizeCir;

    public Sprite containerSprite;
    public List<Particle> particles;


    int spawnRateCounter = 0;
    int particleCount = 0;
    int subSteps = 8;

    void OnValidate()
    {
        if (spawnType == SpawnType.AllAtOnce)
        {
            if (particlePrefab == null || numOfRows <= 0 || particlesPerRow <= 0)
            {
                Debug.LogWarning("Invalid configuration in ParticleGenerator");
                return;
            }

            particles = new List<Particle>();

            // Defer the cleanup and generation process to avoid conflicts
            EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                                      
                ClearGeneratedParticles();

                GenerateParticlesAll();
            };
        }
        if (spawnType == SpawnType.OneByOne)
        {
            particles = new List<Particle>();

            // Defer the cleanup and generation process to avoid conflicts
            EditorApplication.delayCall += () =>
            {
                if (this == null) return;

                ClearGeneratedParticles();
                GenerateParticle();
            };
        }

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        if (container == ContainerType.Circle)
        {
            spriteRenderer.sprite = containerSprite;
            spriteRenderer.transform.position = Vector3.zero;
            spriteRenderer.transform.localScale = Vector3.one * containerSizeCir * 2;
        }
        else spriteRenderer.sprite = null;

    }

    void ClearGeneratedParticles()
    {
        // Destroy all child GameObjects
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
        particles.Clear();
    }

    void GenerateParticlesAll()
    {
        for (int i = 0; i < numOfRows; i++)
        {
            for (int j = 0; j < particlesPerRow; j++)
            {
                float jitterX = Random.Range(-(density - 1) / 2, (density - 1) / 2)/10;
                float jitterY = Random.Range(-(density - 1) / 2, (density - 1) / 2)/10;

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
                newParticle.Initialize(localSize * particleSize, initialVelocity, resistance);
                particles.Add(newParticle);
            }
        }
    }

    void GenerateParticle()
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
        newParticle.Initialize(localSize * particleSize, initialVelocity, resistance);
        particles.Add(newParticle);
    }

    void FixedUpdate()
    {
        if (spawnType == SpawnType.OneByOne && spawnRateCounter >= spawnRate)
        {
            GenerateParticle();
            spawnRateCounter = 0;
        }
        else
        {
            spawnRateCounter++;
        }

        float deltaTime = Time.deltaTime / subSteps;
        for (int i = 0; i < subSteps; i++)
        {
            ApplyGravity();

            if (container == ContainerType.Rectangle) WallCollisionsRectangle();
            if (container == ContainerType.Circle) WallCollisionsCircle();

            ParticleCollisions();
            UpdatePositions(deltaTime);
        }
    }

    void UpdatePositions(float deltaTime)
    {
        foreach(var particle in particles)
        {
            particle.UpdatePosition(deltaTime);
        }
    }

    void ApplyGravity()
    {
        foreach(var particle in particles)
        {
            particle.Accelerate(new Vector2(0, -gravity));
        }
    }

    void WallCollisionsRectangle()
    {
        float radius = particleSize / 2;
        foreach(var particle in particles)
        {
            Vector2 pos = particle.position;
            Vector2 newPos = pos;
            float distanceX = containerSizeRec.x / 2 - Mathf.Abs(pos.x) - radius;
            if (distanceX < 0)
            {
                newPos.x = pos.x + distanceX * Mathf.Sign(pos.x);
            }
            float distanceY = containerSizeRec.y / 2 - Mathf.Abs(pos.y) - radius;
            if (distanceY < 0)
            {
                newPos.y = pos.y + distanceY * Mathf.Sign(pos.y);
            }
            if (distanceX < 0 || distanceY < 0)
            {
                particle.SetPosition(newPos);
                particle.collision = true;
            }
        }
    }

    void WallCollisionsCircle()
    {
        float radius = particleSize / 2;
        Vector2 circleCenter = Vector2.zero;
        foreach (var particle in particles)
        {
            Vector2 pos = particle.position;
            Vector2 direction = (pos - circleCenter).normalized;
            float distance = Vector2.Distance(particle.position + direction * radius, circleCenter);
            if (distance > containerSizeCir)
            {
                particle.SetPosition(circleCenter + direction * (containerSizeCir - radius));
                particle.collision = true;
            }
        }
    }

    void ParticleCollisions()
    {
        for (int i = 0; i < particles.Count; i++)
        {
            Particle particle1 = particles[i];
            for (int j = i+1; j < particles.Count; j++)
            {
                Particle particle2 = particles[j];
                float distance = Vector2.Distance(particle1.position, particle2.position);
                if (distance < particleSize)
                {
                    Vector2 collisionAxis = particle1.position - particle2.position;
                    Vector2 n = collisionAxis / distance;
                    float delta = particleSize - distance;
                    particle1.SetPosition(particle1.position + 0.5f * delta * n);
                    particle2.SetPosition(particle2.position - 0.5f * delta * n);
                }
            }
        }
    }


    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 0.6f, 0.3f, 0.6f);
        if (container == ContainerType.Rectangle)
            Gizmos.DrawWireCube(Vector2.zero, containerSizeRec);
        //if (container == ContainerType.Circle)
        //    Gizmos.DrawWireSphere(Vector2.zero, containerSizeCir);
    }
}

public enum ContainerType
{
    Rectangle,
    Circle
}

public enum SpawnType
{
    AllAtOnce,
    OneByOne
}