using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private HealthComponent health;
    [SerializeField] private Image fill; // Image con Type = Filled (Horizontal)

    [Header("Opciones")]
    [SerializeField] private Color healthyColor = new Color(0.9f, 0.1f, 0.1f);
    [SerializeField] private Color lowColor = new Color(0.8f, 0.6f, 0.0f);
    [SerializeField, Range(0, 1)] private float lowThreshold = 0.25f;

    private int lastMax;

    void Awake()
    {
        if (!health) health = FindFirstObjectByType<HealthComponent>();
        if (health)
        {
            health.OnHealthChanged += OnHealthChanged;
            lastMax = health.MaxHP;
        }
        RefreshImmediate();
    }

    void OnDestroy()
    {
        if (health) health.OnHealthChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(int current, int max)
    {
        if (fill)
        {
            float t = (max <= 0) ? 0f : (float)current / max;
            fill.fillAmount = t;
            fill.color = t <= lowThreshold ? lowColor : healthyColor;
        }

        if (max != lastMax)
        {
            lastMax = max;
           
        }
    }

    public void RefreshImmediate()
    {
        if (!health) return;
        OnHealthChanged(health.CurrentHP, health.MaxHP);
    }
}
