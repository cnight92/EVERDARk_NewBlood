using UnityEngine;
using System;

[DisallowMultipleComponent]
public class HealthComponent : MonoBehaviour
{
    [SerializeField] private int maxHP = 8;
    [SerializeField] private float hitInvuln = 0.35f;

    public int MaxHP => maxHP;
    public int CurrentHP { get; private set; }

    public event Action<int,int> OnHealthChanged; // (current, max)
    public event Action OnDeath;

    private bool invulnerable;
    private float invulnTimer;

    void Awake()
    {
        CurrentHP = Mathf.Max(1, maxHP);
        OnHealthChanged?.Invoke(CurrentHP, maxHP);
    }

    void Update()
    {
        if (invulnerable)
        {
            invulnTimer -= Time.deltaTime;
            if (invulnTimer <= 0f) invulnerable = false;
        }
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || invulnerable) return;
        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        OnHealthChanged?.Invoke(CurrentHP, maxHP);
        if (CurrentHP <= 0) OnDeath?.Invoke();
        else { invulnerable = true; invulnTimer = hitInvuln; }
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        CurrentHP = Mathf.Min(maxHP, CurrentHP + amount);
        OnHealthChanged?.Invoke(CurrentHP, maxHP);
    }

    public void SetMax(int newMax, bool healToFull = true)
    {
        maxHP = Mathf.Max(1, newMax);
        if (healToFull) CurrentHP = maxHP;
        else CurrentHP = Mathf.Min(CurrentHP, maxHP);
        OnHealthChanged?.Invoke(CurrentHP, maxHP);
    }

    public void FullHeal()
    {
        Heal(maxHP);
    }
}
