using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CameraSafeZone : MonoBehaviour
{
    public enum LockMode { FreezeOnEnter, UseAnchor }

    [Header("Modo de bloqueo")]
    public LockMode lockMode = LockMode.FreezeOnEnter;

    [Header("Anchor (opcional si usas UseAnchor)")]
    public Transform anchor;  // punto fijo de cámara para esta zona

    private Collider2D col;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    // Devuelve dónde debe quedarse la cámara mientras el player esté en la zona
    public Vector3 GetLockPositionOrCurrent(Vector3 cameraCurrent, Camera cam)
    {
        if (lockMode == LockMode.UseAnchor && anchor != null)
        {
            return new Vector3(anchor.position.x, anchor.position.y, cameraCurrent.z);
        }
        // Por defecto, congela la cámara en su posición al entrar
        return cameraCurrent;
    }
}
