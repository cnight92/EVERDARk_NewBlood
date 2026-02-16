using UnityEngine;
using System;

[DisallowMultipleComponent]
public class InfectionComponent : MonoBehaviour
{
    [SerializeField, Range(0, 100)] private float value;
    [SerializeField] private float passiveDecayPerSecond = 0f; // 0 si no decae

    public float Value => value;

    public event Action<float> OnInfectionChanged; // 0..100
    public event Action<int> OnStageChanged;       // 0..4

    private int stage; // 0:[0), 1:[25), 2:[50), 3:[75), 4:100

    void Update()
    {
        if (passiveDecayPerSecond > 0f && value > 0f)
            SetValue(value - passiveDecayPerSecond * Time.deltaTime);
    }

    public void Apply(float amount)
    {
        if (amount <= 0f) return;
        SetValue(value + amount);
    }

    public void Purify(float amount)
    {
        if (amount <= 0f) return;
        SetValue(value - amount);
    }

    public void FullPurify()
    {
        SetValue(0f);
    }

    public void SetValue(float v)
    {
        var clamped = Mathf.Clamp(v, 0f, 100f);
        if (Mathf.Approximately(clamped, value)) return;
        value = clamped;
        OnInfectionChanged?.Invoke(value);

        var newStage = (value >= 100f) ? 4 :
                       (value >= 75f) ? 3 :
                       (value >= 50f) ? 2 :
                       (value >= 25f) ? 1 : 0;
        if (newStage != stage)
        {
            stage = newStage;
            OnStageChanged?.Invoke(stage);
        }
    }
}
