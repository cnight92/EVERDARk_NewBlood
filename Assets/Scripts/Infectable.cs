using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Infectable : MonoBehaviour
{
    [Header("Infection settings")]
    public float infectionDuration = 10f;            // tiempo desde infectado hasta muerte si no se cura
    public bool isInfected = false;
    public float infectedRemaining = 0f;

    [Header("QTE & probability")]
    [Range(0f, 1f)] public float baseInfectionChance = 1f; // probabilidad base de infectarse si falla QTE (1 = 100%)
    public bool immune = false; // si quieres bloquear infección por items

    // Eventos
    public UnityEvent OnInfected;      // dispara al entrar en estado infectado
    public UnityEvent OnCured;         // dispara cuando se cura
    public UnityEvent OnDeathByInfection;

    Coroutine infectionCoroutine;

    void Update()
    {
        // debug / testing
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.K)) // forzar muerte test
        {
            KillByInfection();
        }
#endif
    }

    public void TryApplyInfection(float chance = 1f, float forcedDuration = -1f)
    {
        if (immune || isInfected) return;

        float roll = UnityEngine.Random.value;
        if (roll <= chance)
        {
            StartInfection(forcedDuration < 0f ? infectionDuration : forcedDuration);
        }
    }

    public void StartInfection(float duration)
    {
        if (immune || isInfected) return;

        isInfected = true;
        infectedRemaining = duration;
        OnInfected?.Invoke();

        if (infectionCoroutine != null) StopCoroutine(infectionCoroutine);
        infectionCoroutine = StartCoroutine(InfectionCountdown());
    }

    IEnumerator InfectionCountdown()
    {
        while (infectedRemaining > 0f)
        {
            infectedRemaining -= Time.deltaTime;
            yield return null;
        }
        infectionCoroutine = null;
        // tiempo agotado -> muerte
        KillByInfection();
    }

    public void Cure()
    {
        if (!isInfected) return;
        isInfected = false;
        infectedRemaining = 0f;
        if (infectionCoroutine != null) StopCoroutine(infectionCoroutine);
        infectionCoroutine = null;
        OnCured?.Invoke();
    }

    void KillByInfection()
    {
        // Lógica de muerte: llamar a tu sistema de salud / muerte
        OnDeathByInfection?.Invoke();

        // Ejemplo genérico: si tienes PlayerHealth:
        var ph = GetComponent<HealthComponent>();
        if (ph != null)
        {
            ph.TakeDamage(ph.CurrentHP);
            return;
        }

        // Fallback: Desactivar jugador (modifica según tu flujo)
        gameObject.SetActive(false);
    }
}
