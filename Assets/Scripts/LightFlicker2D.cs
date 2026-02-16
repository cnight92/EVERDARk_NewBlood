using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class LightFlicker2D : MonoBehaviour
{
    [Header("Intensidad")]
    [SerializeField] private float baseIntensity = 1f;
    [SerializeField] private float intensityVariation = 0.3f;

    [Header("Radio")]
    [SerializeField] private float baseRadius = 3f;
    [SerializeField] private float radiusVariation = 0.2f;

    [Header("Velocidad")]
    [SerializeField] private float flickerSpeed = 10f;

    [Header("Aleatoriedad extra")]
    [SerializeField] private float randomJitter = 0.1f;

    private Light2D light2D;
    private float timeOffset;

    private void Awake()
    {
        light2D = GetComponent<Light2D>();
        timeOffset = Random.Range(0f, 100f); // evita que todas las luces parpadeen igual
    }

    private void Update()
    {
        float t = Time.time * flickerSpeed + timeOffset;

        // Oscilación suave tipo ruido
        float intensity = baseIntensity + Mathf.PerlinNoise(t, 0f) * intensityVariation - intensityVariation / 2f;
        float radius = baseRadius + Mathf.PerlinNoise(0f, t) * radiusVariation - radiusVariation / 2f;

        // Jitter aleatorio rápido
        intensity += Random.Range(-randomJitter, randomJitter);
        radius += Random.Range(-randomJitter * 0.5f, randomJitter * 0.5f);

        light2D.intensity = Mathf.Max(0, intensity);
        light2D.pointLightOuterRadius = Mathf.Max(0, radius);
    }
}
