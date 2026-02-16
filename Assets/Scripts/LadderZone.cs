using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LadderZone : MonoBehaviour
{
    [Header("Centro de escalera")]
    [SerializeField] private bool useCustomCenter = false;
    [SerializeField] private Transform customCenter; // opcional: un Empty justo en el centro deseado

    private Collider2D col;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private float GetCenterX()
    {
        if (useCustomCenter && customCenter) return customCenter.position.x;
        return col.bounds.center.x; // centro del collider si no hay transform
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponentInParent<PlayerController2D>();
        if (player) player.EnterLadder(GetCenterX());
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var player = other.GetComponentInParent<PlayerController2D>();
        if (player) player.ExitLadder();
    }

    // Si tu escalera se mueve (ascensor/scroll), puedes actualizar el centro dinámicamente:
    void OnTriggerStay2D(Collider2D other)
    {
        var player = other.GetComponentInParent<PlayerController2D>();
        if (player)
        {
            // re-centra suavemente si la escalera se desplaza
            player.EnterLadder(GetCenterX());
        }
    }
}
