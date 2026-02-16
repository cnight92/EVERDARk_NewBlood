using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    public string weaponId = "pistol";
    public int amount = 8;
    public Sprite openSprite;

    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerController2D>();
        if (!player) return;

        player.GiveAmmo(weaponId, amount);
        if (openSprite != null)
        {
            GetComponent<SpriteRenderer>().sprite = openSprite;
            Material mat = GetComponent<SpriteRenderer>().material;
            mat.SetFloat("_OutlineToggle", 0);
            mat.SetFloat("_InnerGlow", 0);
            GetComponent<SpriteRenderer>().material = mat;
            this.enabled = false;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
