using UnityEngine;

public enum DamageKind { Ranged, Melee }
public enum DamageTag { Default, Bite, Holy, Fire }

[DisallowMultipleComponent]
public class DamageDealer2D : MonoBehaviour
{
    [SerializeField] private DamageKind kind = DamageKind.Ranged;
    [SerializeField] private float amount = 1f;
    [SerializeField] private string ownerTag = "Player"; // quién inflige el daño
    [SerializeField] private DamageTag tag = DamageTag.Default;
    [Header("Bite Infection Settings")]
    [SerializeField] private int qteRequiredPresses = 12;
    [SerializeField] private float qteDuration = 3f;

    public DamageKind Kind => kind;
    public float Amount => amount;
    public string OwnerTag => ownerTag;
    public DamageTag Tag => tag;
    public int QTERequiredPresses => qteRequiredPresses;
    public float QTEDuration => qteDuration;

    public void Configure(DamageKind k, float amt, string owner, DamageTag t = DamageTag.Default)
    {
        kind = k; amount = amt; ownerTag = owner; tag = t;
    }
}
