using UnityEngine;
using System.Collections.Generic;

public class BloodFlowPath : MonoBehaviour
{
    [Header("Configuration")]
    public ParticleSystem bloodParticleSystem;

    [Header("Your Specific Waypoints")]
    public Transform point1;
    public Transform point2;
    public Transform point3;
    public Transform point4;
    public Transform point5;
    public Transform point6;

    [Header("Realism Settings")]
    public float flowSpeed = 3.0f;
    public float veinWidth = 0.5f;
    public float turbulence = 0.5f;

    [Header("Physics Interaction")]
    [Tooltip("Lower number = Easier to push around objects. Higher = Stiff movement.")]
    public float steeringStrength = 5.0f;

    private List<Transform> allPoints;

    void Start()
    {
        allPoints = new List<Transform>();
        if (point1 != null) allPoints.Add(point1);
        if (point2 != null) allPoints.Add(point2);
        if (point3 != null) allPoints.Add(point3);
        if (point4 != null) allPoints.Add(point4);
        if (point5 != null) allPoints.Add(point5);
        if (point6 != null) allPoints.Add(point6);
    }

    void LateUpdate()
    {
        if (bloodParticleSystem == null || allPoints.Count < 2) return;

        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[bloodParticleSystem.main.maxParticles];
        int numParticlesAlive = bloodParticleSystem.GetParticles(particles);

        float totalLifetime = bloodParticleSystem.main.startLifetime.constant;

        for (int i = 0; i < numParticlesAlive; i++)
        {
            // --- Standard Path Calculation (Same as before) ---
            float age = totalLifetime - particles[i].remainingLifetime;
            float normalizedLifetime = age / totalLifetime;
            float pathProgress = normalizedLifetime * (allPoints.Count - 1);
            int currentPointIndex = Mathf.FloorToInt(pathProgress);
            int nextPointIndex = currentPointIndex + 1;

            if (nextPointIndex >= allPoints.Count)
            {
                nextPointIndex = allPoints.Count - 1;
                currentPointIndex = allPoints.Count - 2;
            }

            float segmentProgress = pathProgress - currentPointIndex;
            Vector3 centerTarget = Vector3.Lerp(allPoints[currentPointIndex].position, allPoints[nextPointIndex].position, segmentProgress);

            // --- Offset Calculation ---
            uint seed = particles[i].randomSeed;
            float randomX = ((seed % 100) / 100f * 2f - 1f) * veinWidth;
            float randomY = ((seed % 123) / 123f * 2f - 1f) * veinWidth;
            Vector3 offset = new Vector3(randomX, randomY, 0);
            Vector3 wiggle = new Vector3(Mathf.Sin(Time.time * 2f + i), Mathf.Cos(Time.time * 1.5f + i), 0) * turbulence * 0.1f;

            Vector3 finalTarget = centerTarget + offset + wiggle;

            // --- THE NEW PART: Soft Steering ---
            Vector3 desiredVelocity = (finalTarget - particles[i].position).normalized * flowSpeed;

            // We "Blend" (Lerp) the current velocity with the desired velocity.
            // This preserves the "Push" from the small box!
            particles[i].velocity = Vector3.Lerp(particles[i].velocity, desiredVelocity, Time.deltaTime * steeringStrength);
        }

        bloodParticleSystem.SetParticles(particles, numParticlesAlive);
    }
}