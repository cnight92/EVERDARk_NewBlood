using UnityEngine;

public class KeyPickup : MonoBehaviour
{
    [Header("ID ÚNICO DE ESTE PICKUP EN EL MUNDO")]
    public string pickupId = "pickup_default_01";

    [Header("Llave que añade al inventario")]
    public string keyId = "default_key";

    public bool destroyOnPickup = true;

    private void Start()
    {
        // Si ya se recogió en esta partida, no aparece
        if (GameKeysManager.Instance != null &&
            GameKeysManager.Instance.IsPickupCollected(pickupId))
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (GameKeysManager.Instance == null)
        {
            Debug.LogError("No hay GameKeysManager en la escena.");
            return;
        }

        GameKeysManager.Instance.AddKey(keyId);
        GameKeysManager.Instance.RegisterPickupCollected(pickupId);

        if (destroyOnPickup)
            Destroy(gameObject);
    }
}
