using UnityEngine;
using UnityEngine.UI;

public class InfectionBarUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private InfectionComponent infection;
    [SerializeField] private Image fill; // Image con Type = Filled (Horizontal)

    [Header("Stage Colors (0..4)")]
    [SerializeField] private Color stage0 = new Color(0.4f, 0.0f, 0.4f, 0.6f);
    [SerializeField] private Color stage1 = new Color(0.6f, 0.0f, 0.6f, 0.8f);
    [SerializeField] private Color stage2 = new Color(0.8f, 0.0f, 0.8f, 0.9f);
    [SerializeField] private Color stage3 = new Color(1.0f, 0.0f, 1.0f, 1.0f);
    [SerializeField] private Color stage4 = new Color(0.9f, 0.1f, 0.9f, 1.0f);

    private int currentStage;

    void Awake()
    {
        if (!infection) infection = FindFirstObjectByType<InfectionComponent>();
        if (infection)
        {
            infection.OnInfectionChanged += OnInfectionChanged;
            infection.OnStageChanged += OnStageChanged;
        }
        RefreshImmediate();
    }

    void OnDestroy()
    {
        if (infection)
        {
            infection.OnInfectionChanged -= OnInfectionChanged;
            infection.OnStageChanged -= OnStageChanged;
        }
    }

    private void OnInfectionChanged(float value01_100)
    {
        if (!fill) return;
        fill.fillAmount = Mathf.Clamp01(value01_100 / 100f);
    }

    private void OnStageChanged(int stage)
    {
        currentStage = stage;
        if (!fill) return;

        fill.color = stage switch
        {
            0 => stage0,
            1 => stage1,
            2 => stage2,
            3 => stage3,
            _ => stage4
        };
    }

    public void RefreshImmediate()
    {
        if (!infection || !fill) return;
        OnInfectionChanged(infection.Value);
        // Forzar color inicial
        var v = infection.Value;
        var st = (v >= 100f) ? 4 : (v >= 75f ? 3 : (v >= 50f ? 2 : (v >= 25f ? 1 : 0)));
        OnStageChanged(st);
    }
}
