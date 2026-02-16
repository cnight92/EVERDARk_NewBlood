using UnityEngine;

public enum FireMode { Semi, Auto }

[CreateAssetMenu(menuName = "EVERDARK_Labs/WeaponData", fileName = "WD_NewWeapon")]
public class WeaponData : ScriptableObject
{
    [Header("ID y nombre")]
    public string weaponId = "pistol";
    public string displayName = "Pistol";

    [Header("Disparo")]
    public FireMode fireMode = FireMode.Semi;
    [Min(0f)] public float fireCooldown = 0.25f;   // seg entre disparos
    [Min(1)] public int ammoPerShot = 1;          // balas por disparo
    public GameObject projectilePrefab;            // opcional override
    public GameObject muzzleVFXPrefab;             // opcional override
    public GameObject shellVFXPrefab;
    public GameObject magazineVFXPrefab;

    [Header("Munición")]
    [Min(0)] public int magSize = 8;               // capacidad cargador
    [Min(0)] public int startMag = 8;              // al equipar
    [Min(0)] public int startReserve = 24;         // al equipar
    [Min(0f)] public float reloadTime = 1.1f;      // si no usas anim event
    public bool lockMovementDuringReload = false;  // inmoviliza al recargar

    [Header("Animación (parámetros en Animator)")]
    public string shootTrigger = "Shoot";
    public string reloadTrigger = "Reload";
    public string isReloadingBool = "IsReloading";
    public string dryFireTrigger = "DryFire";      // click vacío (opcional)
}
