using System;
using Unity.VisualScripting;
using UnityEngine;


public class Particle : MonoBehaviour
{
    public Vector2 position { private set; get; }
    Vector2 oldPosition;
    Vector2 acceleration;
    public bool collision;

    public void Initialize(Vector3 particleSize, Vector3 next_position)
    {
        transform.localScale = particleSize;
        position = transform.position + next_position;
        oldPosition = transform.position;
        acceleration = Vector2.zero;
        collision = false;
    }

    public void UpdatePosition(float deltaTime, float resistance)
    {
        Vector2 velocity;
        if (collision)
        {
            velocity = (position - oldPosition) * resistance;
            collision = false;
        }
        else velocity = position - oldPosition;
        oldPosition = position;

        // Verlet integration
        position += velocity + acceleration * Mathf.Pow(deltaTime, 2);
        transform.position = position;

        acceleration = Vector2.zero;
    }

    public void Accelerate(Vector2 accelaration)
    {
        this.acceleration = accelaration;
    }

    public void SetPosition(Vector2 pos)
    {
        position = pos;
    }

}
