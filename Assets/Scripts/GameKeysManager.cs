using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameKeysManager : MonoBehaviour
{
    public static GameKeysManager Instance { get; private set; }

    private HashSet<string> keys = new HashSet<string>();
    private HashSet<string> collectedPickups = new HashSet<string>();
    private HashSet<string> openedDoors = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        //LoadState();
    }

    // ===== LLAVES EN EL INVENTARIO =====
    public void AddKey(string keyId)
    {
        if (keys.Add(keyId))
            Debug.Log($"Llave añadida: {keyId}");
    }

    public bool HasKey(string keyId) => keys.Contains(keyId);

    public void RemoveKey(string keyId)
    {
        if (keys.Remove(keyId))
            Debug.Log($"Llave eliminada: {keyId}");
    }

    // ===== PICKUPS (OBJETOS RECOGIBLES) =====
    public void RegisterPickupCollected(string pickupId)
    {
        if (string.IsNullOrEmpty(pickupId)) return;

        if (collectedPickups.Add(pickupId))
            SaveState();
    }

    public bool IsPickupCollected(string pickupId)
    {
        if (string.IsNullOrEmpty(pickupId)) return false;
        return collectedPickups.Contains(pickupId);
    }

    // ===== PUERTAS / OBJETOS ABIERTOS =====
    public void RegisterDoorOpened(string doorId)
    {
        if (string.IsNullOrEmpty(doorId)) return;

        if (openedDoors.Add(doorId))
            SaveState();
    }

    public bool IsDoorOpened(string doorId)
    {
        if (string.IsNullOrEmpty(doorId)) return false;
        return openedDoors.Contains(doorId);
    }

    // ===== GUARDAR / CARGAR (PLAYERPREFS SIMPLE) =====
    private const string KEYS_SAVE = "GK_keys";
    private const string PICKUPS_SAVE = "GK_pickups";
    private const string DOORS_SAVE = "GK_doors";

    public void SaveState()
    {
        PlayerPrefs.SetString(KEYS_SAVE, string.Join(",", keys));
        PlayerPrefs.SetString(PICKUPS_SAVE, string.Join(",", collectedPickups));
        PlayerPrefs.SetString(DOORS_SAVE, string.Join(",", openedDoors));
        PlayerPrefs.Save();
    }

    public void LoadState()
    {
        keys = LoadSet(KEYS_SAVE);
        collectedPickups = LoadSet(PICKUPS_SAVE);
        openedDoors = LoadSet(DOORS_SAVE);
    }

    private HashSet<string> LoadSet(string key)
    {
        var result = new HashSet<string>();
        string data = PlayerPrefs.GetString(key, "");
        if (string.IsNullOrEmpty(data))
            return result;

        var split = data.Split(',');
        foreach (var id in split)
        {
            if (!string.IsNullOrWhiteSpace(id))
                result.Add(id);
        }
        return result;
    }
}
