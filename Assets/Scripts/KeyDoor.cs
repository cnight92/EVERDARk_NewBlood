using UnityEngine;

public class KeyDoor : MonoBehaviour
{
    [Header("ID ÚNICO DE ESTA PUERTA")]
    public string doorId = "door_default_01";

    [Header("Llave requerida")]
    public string requiredKeyId = "default_key";
    public bool consumeKey = false;
    public Sprite openedSprite;
    private bool playerInRange = false;
    private bool isOpen = false;

    private Collider2D col;
    private Animator anim;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
    }

    private void Start()
    {
        if (GameKeysManager.Instance != null &&
            GameKeysManager.Instance.IsDoorOpened(doorId))
        {
            // Ya estaba abierta en esta partida → abrir directamente
            ForceOpenVisual();
            isOpen = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isOpen) return;

        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            // Mostrar "Pulsa E para abrir" si quieres.
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }

    private void Update()
    {
        if (!playerInRange || isOpen)
            return;

        if (UnifiedInput.Instance.WasPressedThisFrame(UnifiedInput.Instance.interactInput))
        {
            TryOpen();
        }
    }

    private void TryOpen()
    {
        if (GameKeysManager.Instance == null)
        {
            Debug.LogError("No hay GameKeysManager en la escena.");
            return;
        }

        if (GameKeysManager.Instance.HasKey(requiredKeyId))
        {
            if (consumeKey)
                GameKeysManager.Instance.RemoveKey(requiredKeyId);

            OpenDoor();
        }
        else
        {
            Debug.Log("No tienes la llave necesaria.");
        }
    }

    private void OpenDoor()
    {
        isOpen = true;

        if (GameKeysManager.Instance != null)
            GameKeysManager.Instance.RegisterDoorOpened(doorId);

        //if (anim != null)
        //    anim.SetTrigger("Open");

        //if (col != null)
        //    col.enabled = false;

        if (openedSprite != null)
        {
            GetComponent<SpriteRenderer>().sprite = openedSprite;
            Material mat = GetComponent<SpriteRenderer>().material;
            mat.SetFloat("_OutlineToggle", 0);
            mat.SetFloat("_InnerGlow", 0);
            GetComponent<SpriteRenderer>().material = mat;
        }
        else
            gameObject.SetActive(false);
    }

    // Llamada cuando cargamos escena y ya estaba abierta
    private void ForceOpenVisual()
    {
        //if (anim != null)
        //    anim.SetTrigger("Open");

        //if (col != null)
        //    col.enabled = false;


        if (openedSprite != null)
        {
            GetComponent<SpriteRenderer>().sprite = openedSprite;
            Material mat = GetComponent<SpriteRenderer>().material;
            mat.SetFloat("_OutlineToggle", 0);
            mat.SetFloat("_InnerGlow", 0);
            GetComponent<SpriteRenderer>().material = mat;
        }
        else
            gameObject.SetActive(false);
    }
}