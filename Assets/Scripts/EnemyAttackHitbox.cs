using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyAttackHitbox : MonoBehaviour
{
    private Collider2D col;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;
        col.enabled = false;
    }

    public void SetActive(bool active)
    {
        if (col) col.enabled = active;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponentInParent<PlayerController2D>();
        if (player)
        {
            var dealer = GetComponent<DamageDealer2D>();
            if (!dealer) { dealer = gameObject.AddComponent<DamageDealer2D>(); dealer.Configure(DamageKind.Melee, 1f, "Enemy", DamageTag.Default); }
            player.TakeHitFrom(dealer);
        }
    }

}
