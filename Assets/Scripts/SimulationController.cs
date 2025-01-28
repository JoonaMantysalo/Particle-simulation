using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class SimulationController : MonoBehaviour
{
    public float particleSize;
    public float gravity;
    [Range(0f, 1f)]
    public float resistance;
    public bool spawnParticles;

    public ContainerType container;
    public Vector2 containerSizeRec;
    public float containerSizeCir;
    public Sprite containerSprite;
    public List<Particle> particles;

    ParticleGeneration particleGeneration;
    Vector2 centerPoint;
    int spawnRateCounter = 0;
    int subSteps = 8;

    Dictionary<Particle, (int, int)> particleCell = new Dictionary<Particle, (int, int)>();
    Dictionary<(int, int), List<Particle>> cellParticles = new Dictionary<(int, int), List<Particle>>();

    public ComputeShader computeShader;
    ComputeBuffer particleBuffer;
    ComputeBuffer positionChangesBuffer;


    struct ParticleStruct
    {
        public float2 position;
    }


    void OnValidate()
    {
        if (particleSize == 0) return;

        

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        if (container == ContainerType.Circle)
        {
            spriteRenderer.sprite = containerSprite;
            spriteRenderer.transform.position = Vector3.zero;
            spriteRenderer.transform.localScale = Vector3.one * containerSizeCir * 2;
        }
        else spriteRenderer.sprite = null;

        
    }

    private void Start()
    {
        centerPoint = transform.position;
        particleGeneration = GetComponent<ParticleGeneration>();
        particles = particleGeneration.Generate();
        CreateGrid();
        foreach (Particle p in particles)
        {
            
            (int, int) cell = GetCell(p);
            particleCell[p] = cell;
            cellParticles[cell].Add(p);
        }
    }

    void CreateGrid()
    {
        if (container == ContainerType.Circle)
        {
            float containerMax = containerSizeCir;
            for (float i = -containerMax; i < containerMax; i += particleSize)
            {
                for (float j = -containerMax; j < containerMax; j += particleSize)
                {
                    cellParticles[(Mathf.FloorToInt(i / particleSize), Mathf.FloorToInt(j / particleSize))] = new List<Particle>();
                }
            }
        }
    }

    void Collisions()
    {
        foreach (Particle particle in particles)
        {
            (int, int) currentCell = particleCell[particle];
            for (int i = currentCell.Item1 - 1; i <= currentCell.Item1 + 1; i++)
            {
                for (int j = currentCell.Item2 - 1; j <= currentCell.Item2 + 1; j++)
                {
                    (int, int) neighborCell = (i, j);
                    if (cellParticles.ContainsKey(neighborCell))
                    {
                        foreach (Particle neighbor in cellParticles[neighborCell])
                        {
                            if (particle == neighbor) continue;

                            float distance = Vector2.Distance(particle.position, neighbor.position);
                            if (distance < particleSize)
                            {
                                Vector2 collisionAxis = particle.position - neighbor.position;
                                Vector2 n = collisionAxis / distance;
                                float delta = particleSize - distance;
                                particle.SetPosition(particle.position + 0.5f * delta * n);
                                neighbor.SetPosition(neighbor.position - 0.5f * delta * n);

                            }
                        }
                    } 
                }
            }
            
        }
    }

    (int, int) GetCell(Particle particle)
    {
        int x = Mathf.FloorToInt(particle.position.x / particleSize);
        int y = Mathf.FloorToInt(particle.position.y / particleSize);
        return (x, y);
    }

    void FixedUpdate()
    {
        if (particleGeneration.spawnType == SpawnType.OneByOne 
            && spawnRateCounter >= particleGeneration.spawnRate 
            && spawnParticles)
        {
            Particle newParticle = particleGeneration.GenerateParticle();
            particles.Add(newParticle);
            (int, int) cell = GetCell(newParticle);
            particleCell[newParticle] = cell;
            cellParticles[cell].Add(newParticle);

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

            //ParticleCollisions();
            //Collisions();
            CollisionsGPU();
            //Test();

            UpdatePositions(deltaTime);
        }
    }

    void CollisionsGPU()
    {
        // Set buffers
        particleBuffer = new ComputeBuffer(particles.Count, sizeof(float) * 2);
        positionChangesBuffer = new ComputeBuffer(particles.Count, sizeof(float) * 2);

        int i = 0;
        ParticleStruct[] particleStructs = new ParticleStruct[particles.Count];
        float2[] positionChanges = new float2[particles.Count];
        foreach (Particle particle in particles)
        {
            particleStructs[i].position = particle.position;
            positionChanges[i] = float2.zero;
            i++;
        }
        particleBuffer.SetData(particleStructs);
        positionChangesBuffer.SetData(positionChanges);

        // Set shader
        computeShader.SetBuffer(0, "particles", particleBuffer);
        computeShader.SetBuffer(0, "positionChanges", positionChangesBuffer);
        computeShader.SetFloat("particleRadius", particleSize / 2.0f);

        int threadGroups = Mathf.CeilToInt(particles.Count / 256.0f);
        computeShader.Dispatch(0, threadGroups, 1, 1);

        positionChanges = new float2[particles.Count];
        positionChangesBuffer.GetData(positionChanges);
        particleBuffer.Release();
        positionChangesBuffer.Release();

        i = 0;
        foreach (Particle particle in particles)
        {
            Vector2 posChange = new Vector2(positionChanges[i].x, positionChanges[i].y);
            particle.SetPosition(particle.position + posChange);
            i++;
        }
    }

    void Test()
    {
        // Each cell
        foreach (var partList in cellParticles.Values)
        {
            // Each particle in that cell
            foreach(var particle in partList)
            {
                (int, int) currentCell = particleCell[particle];
                for (int i = currentCell.Item1 - 1; i <= currentCell.Item1 + 1; i++)
                {
                    for (int j = currentCell.Item2 - 1; j <= currentCell.Item2 + 1; j++)
                    {
                        (int, int) neighborCell = (i, j);
                        if (cellParticles.ContainsKey(neighborCell))
                        {
                            foreach (Particle neighbor in cellParticles[neighborCell])
                            {
                                if (particle == neighbor) continue;

                                float distance = Vector2.Distance(particle.position, neighbor.position);
                                if (distance < particleSize)
                                {
                                    Vector2 collisionAxis = particle.position - neighbor.position;
                                    Vector2 n = collisionAxis / distance;
                                    float delta = particleSize - distance;
                                    particle.SetPosition(particle.position + 0.5f * delta * n);
                                    neighbor.SetPosition(neighbor.position - 0.5f * delta * n);

                                }
                            }
                        }
                    }
                }
            }
        }
    }

    void UpdatePositions(float deltaTime)
    {
        foreach(var particle in particles)
        {
            particle.UpdatePosition(deltaTime, resistance);
            UpdateCell(particle);
        }
    }

    void UpdateCell(Particle particle)
    {
        cellParticles[particleCell[particle]].Remove(particle);

        (int, int) newCell = GetCell(particle);
        cellParticles[newCell].Add(particle);
        particleCell[particle] = newCell;
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

    // Old
    void ParticleCollisions()
    {
        for (int i = 0; i < particles.Count; i++)
        {
            Particle particle1 = particles[i];
            for (int j = i + 1; j < particles.Count; j++)
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

    private void OnDestroy()
    {
        if (particleBuffer != null)
        {
            particleBuffer.Release();
        }
        if (positionChangesBuffer != null)
        {
            positionChangesBuffer.Release();
        }
    }


    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 0.6f, 0.3f, 0.6f);
        if (container == ContainerType.Rectangle)
            Gizmos.DrawWireCube(Vector2.zero, containerSizeRec);
        if (container == ContainerType.Circle)
            Gizmos.DrawWireSphere(Vector2.zero, containerSizeCir);        

        
    }
}