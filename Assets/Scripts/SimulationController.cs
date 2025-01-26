
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class SimulationController : MonoBehaviour
{
    public float particleSize;
    public float gravity;
    [Range(0f, 1f)]
    public float resistance;

    public ContainerType container;
    public Vector2 containerSizeRec;
    public float containerSizeCir;
    public Sprite containerSprite;
    public List<Particle> particles;

    ParticleGeneration particleGeneration;
    Vector2 centerPoint;
    int spawnRateCounter = 0;
    int subSteps = 8;

    void OnValidate()
    {
        particleGeneration = GetComponent<ParticleGeneration>();
        centerPoint = transform.position;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        if (container == ContainerType.Circle)
        {
            spriteRenderer.sprite = containerSprite;
            spriteRenderer.transform.position = Vector3.zero;
            spriteRenderer.transform.localScale = Vector3.one * containerSizeCir * 2;
        }
        else spriteRenderer.sprite = null;
    }

    void FixedUpdate()
    {
        if (particleGeneration.spawnType == SpawnType.OneByOne && spawnRateCounter >= particleGeneration.spawnRate)
        {
            particleGeneration.GenerateParticle(particles, particleSize);
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
            particle.UpdatePosition(deltaTime, resistance);
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
        foreach (var particle in particles)
        {
            Vector2 pos = particle.position;
            Vector2 direction = (pos - centerPoint).normalized;
            float distance = Vector2.Distance(particle.position + direction * radius, centerPoint);
            if (distance > containerSizeCir)
            {
                particle.SetPosition(centerPoint + direction * (containerSizeCir - radius));
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