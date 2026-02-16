using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class CameraFollow2D : MonoBehaviour
{
    public enum FollowAxis { Both, XOnly, YOnly }

    [Header("Target")]
    [SerializeField] private Transform target;       // Player
    [SerializeField] private Vector2 offset = Vector2.zero;
    [SerializeField] private FollowAxis followAxis = FollowAxis.Both;

    [Header("Smoothing (follow)")]
    [SerializeField, Range(0.01f, 0.6f)] private float smoothTime = 0.15f;

    [Header("Dead-Zone (fracción del tamaño visible)")]
    [Tooltip("Ancho relativo de la zona muerta (0..1) respecto al ancho de la vista.")]
    [SerializeField, Range(0f, 1f)] private float deadZoneWidthPct = 0.35f;
    [Tooltip("Alto relativo de la zona muerta (0..1) respecto al alto de la vista.")]
    [SerializeField, Range(0f, 1f)] private float deadZoneHeightPct = 0.25f;
    [SerializeField] private bool showDeadZoneGizmo = true;

    [Header("Bounds (elige uno)")]
    [SerializeField] private Collider2D worldBoundsCollider; // opcional (Box/Composite/Tilemap)
    [SerializeField] private Vector2 worldMin = new Vector2(-999, -999);
    [SerializeField] private Vector2 worldMax = new Vector2(999, 999);

    [Header("Safe Zones")]
    [SerializeField] private LayerMask safeZoneMask; // capa de CameraSafeZone (trigger)
    [Tooltip("Cuando entras en una zona con anchor, el movimiento hacia el anchor será suave.")]
    [SerializeField] private bool smoothSnapToAnchor = true;
    [SerializeField, Range(0.01f, 0.6f)] private float zoneSmoothTime = 0.2f;

    [Header("Misc")]
    [SerializeField] private float zOffset = -10f; // z de la cámara

    private Camera cam;
    private Vector3 velRef;              // para SmoothDamp (seguimiento normal)
    private Vector3 zoneVelRef;          // para SmoothDamp dentro de zona segura
    private CameraSafeZone currentZone;  // zona en la que estamos (si hay)
    private Vector3 frozenPos;           // posición estática/anchor mientras dure la zona
    private bool hadZoneLastFrame;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (!target)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.transform;
        }
    }

    void LateUpdate()
    {
        if (!target) return;

        // 1) ¿Estamos dentro de una zona segura?
        var zone = GetZoneUnderTarget();

        if (zone != null)
        {
            if (!hadZoneLastFrame || zone != currentZone)
            {
                // Entramos a zona nueva -> decidir posición “objetivo” de la cámara (freeze/anchor)
                currentZone = zone;
                frozenPos = zone.GetLockPositionOrCurrent(transform.position, cam);
            }

            // Mientras estemos en la zona, ir hacia frozenPos (suave si se pide)
            Vector3 desired = new Vector3(frozenPos.x, frozenPos.y, zOffset);
            desired = ClampToBounds(desired);

            if (smoothSnapToAnchor)
                transform.position = Vector3.SmoothDamp(transform.position, desired, ref zoneVelRef, zoneSmoothTime);
            else
                transform.position = desired;

            hadZoneLastFrame = true;
            return;
        }
        else
        {
            // Salimos de cualquier zona
            currentZone = null;
            hadZoneLastFrame = false;
        }

        // 2) Seguimiento normal con DEAD-ZONE (beat'em up)
        FollowWithDeadZone();
    }

    private void FollowWithDeadZone()
    {
        // Tamaño visible de la cámara
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        // Dead-zone en unidades mundo
        float dzHalfW = Mathf.Clamp01(deadZoneWidthPct) * halfW;
        float dzHalfH = Mathf.Clamp01(deadZoneHeightPct) * halfH;

        // Objetivo del player con offset
        Vector3 t = target.position + (Vector3)offset;

        // Partimos del centro actual de la cámara
        Vector3 desired = transform.position;

        if (followAxis == FollowAxis.Both || followAxis == FollowAxis.XOnly)
        {
            float dx = t.x - transform.position.x;
            if (dx > dzHalfW) desired.x = t.x - dzHalfW;
            else if (dx < -dzHalfW) desired.x = t.x + dzHalfW;
            // si está dentro de la dead-zone, no movemos X
        }

        if (followAxis == FollowAxis.Both || followAxis == FollowAxis.YOnly)
        {
            float dy = t.y - transform.position.y;
            if (dy > dzHalfH) desired.y = t.y - dzHalfH;
            else if (dy < -dzHalfH) desired.y = t.y + dzHalfH;
            // si está dentro de la dead-zone, no movemos Y
        }

        desired.z = zOffset;

        desired = ClampToBounds(desired);

        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velRef, smoothTime);
    }

    // --- Detectar zona bajo el player (por punto) ---
    private CameraSafeZone GetZoneUnderTarget()
    {
        if (safeZoneMask == 0) return null;
        Collider2D hit = Physics2D.OverlapPoint(target.position, safeZoneMask);
        if (!hit) return null;
        return hit.GetComponent<CameraSafeZone>();
    }

    // --- Clampear para no salir del escenario ---
    private Vector3 ClampToBounds(Vector3 desired)
    {
        Bounds b = GetWorldBounds();

        // Half extents de la cámara ortográfica
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        float minX = b.min.x + halfW;
        float maxX = b.max.x - halfW;
        float minY = b.min.y + halfH;
        float maxY = b.max.y - halfH;

        // Si el escenario es más pequeño que la vista, centramos
        if (minX > maxX)
        {
            float cx = (b.min.x + b.max.x) * 0.5f;
            desired.x = cx;
        }
        else desired.x = Mathf.Clamp(desired.x, minX, maxX);

        if (minY > maxY)
        {
            float cy = (b.min.y + b.max.y) * 0.5f;
            desired.y = cy;
        }
        else desired.y = Mathf.Clamp(desired.y, minY, maxY);

        return desired;
    }

    private Bounds GetWorldBounds()
    {
        if (worldBoundsCollider)
            return worldBoundsCollider.bounds;

        Vector2 size = new Vector2(Mathf.Abs(worldMax.x - worldMin.x), Mathf.Abs(worldMax.y - worldMin.y));
        Vector2 center = (worldMin + worldMax) * 0.5f;
        return new Bounds(center, size);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!showDeadZoneGizmo) return;
        Camera c = cam ? cam : GetComponent<Camera>();
        if (!c) return;

        float halfH = c.orthographicSize;
        float halfW = halfH * c.aspect;

        float dzHalfW = Mathf.Clamp01(deadZoneWidthPct) * halfW;
        float dzHalfH = Mathf.Clamp01(deadZoneHeightPct) * halfH;

        Vector3 center = Application.isPlaying ? transform.position : transform.position;
        Vector3 size = new Vector3(dzHalfW * 2f, dzHalfH * 2f, 0f);

        Gizmos.color = new Color(1f, 1f, 0f, 0.35f);
        Gizmos.DrawWireCube(new Vector3(center.x, center.y, 0f), size);
    }

    void OnDrawGizmosSelected()
    {
        Camera c = cam ? cam : GetComponent<Camera>();
        if (!c) return;

        Bounds b = worldBoundsCollider ? worldBoundsCollider.bounds :
            new Bounds((worldMin + worldMax) * 0.5f, (worldMax - worldMin));

        Gizmos.color = new Color(0f, 0.6f, 1f, 0.25f);
        Gizmos.DrawWireCube(b.center, b.size);
    }
#endif
}
