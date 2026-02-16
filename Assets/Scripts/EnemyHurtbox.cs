using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyHurtbox : MonoBehaviour
{
    private EnemyBase2D enemy;

    void Awake()
    {
        enemy = GetComponentInParent<EnemyBase2D>();
        var c = GetComponent<Collider2D>();
        c.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!enemy) return;
        var dealer = other.GetComponent<DamageDealer2D>();
        if (dealer != null) enemy.ReceiveHit(dealer, other);
    }
}
