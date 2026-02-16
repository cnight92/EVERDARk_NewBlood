using UnityEngine;
using TMPro;

public class EquippedWeaponUI : MonoBehaviour
{
    [Header("Refs")]
    public TMP_Text currentBullets;
    public TMP_Text currentReserve;

    private PlayerController2D player;

    void Start()
    {
        player = FindFirstObjectByType<PlayerController2D>();
        if (!player)
        {
            Debug.LogWarning("[EquippedWeaponUI] No se encontró PlayerController2D en la escena.");
            return;
        }

        // Suscribir al evento
        player.OnAmmoChanged += OnAmmoChangedHandler;

        // Forzar una actualización inicial (por si el UI aparece después de equipar el arma)
        player.RefreshAmmoUI();
    }

    private void OnAmmoChangedHandler(string weaponId, int mag, int reserve)
    {
        if (currentBullets)
            currentBullets.text = $"{mag}";

        if (currentReserve)
            currentReserve.text = reserve.ToString();
    }

    void OnDestroy()
    {
        if (player != null)
            player.OnAmmoChanged -= OnAmmoChangedHandler;
    }
}
