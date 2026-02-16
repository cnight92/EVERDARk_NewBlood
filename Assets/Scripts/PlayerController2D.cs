using System.Collections;
using System.Net.Sockets;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeedX = 5f;
    [SerializeField] private float moveSpeedY = 3f;

    [Header("Orientation")]
    [SerializeField] private bool orientProjectileToAim = true; // rotar el proyectil hacia la dirección
    [SerializeField] private bool orientMuzzleVFXToAim = true;  // rotar el flash hacia la dirección


    [Header("Ladder")]
    [SerializeField] private float climbSpeed = 3.5f;

    [Header("Combat")]
    [SerializeField] private float meleeCooldown = 0.5f;

    [Header("Melee")]
    [SerializeField] private Collider2D meleeHitboxRight;
    [SerializeField] private Collider2D meleeHitboxLeft;
    [SerializeField] private Collider2D meleeHitboxUp;    // NEW
    [SerializeField] private Collider2D meleeHitboxDown;  // NEW
    [SerializeField] private float meleeWindup = 0.08f;
    [SerializeField] private float meleeActiveTime = 0.12f;
    [SerializeField] private bool meleeUseAnimEvents = false;
    [SerializeField] private bool lockMovementDuringMelee = false;

    [Header("Melee Dash")]
    [SerializeField] private float meleeDashSpeed = 8f;
    [SerializeField] private float meleeDashDuration = 0.12f;
    [SerializeField] private float meleeDashDelay = 0f;

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private float projectileLifetime = 2f;
    [SerializeField] private bool projectileFlipWithFacing = true;

    [Header("Refs (Child)")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer sprite;

    [Header("VFX & Sockets")]
    [SerializeField] private Transform shootMuzzle;       // fallback
    [SerializeField] private Transform shootMuzzleRight;
    [SerializeField] private Transform shootMuzzleLeft;
    [SerializeField] private Transform shootMuzzleUp;     // NEW
    [SerializeField] private Transform shootMuzzleDown;   // NEW
    [SerializeField] private Transform hurtPoint;
    [SerializeField] private GameObject hurtVFXPrefab;
    [SerializeField] private bool mirrorVFXWithFlip = true;

    [Header("Respawn")]
    [SerializeField] private Transform customSpawnPoint;
    [SerializeField] private float respawnDelay = 0.25f;

    [Header("Shoot Lock")]
    [SerializeField] private float shootLockDuration = 0.18f;
    [SerializeField] private bool unlockOnShootAnimEvent = false;

    [Header("Ladder Center Clamp")]
    [SerializeField] private bool clampXToLadderCenter = true;
    [SerializeField] private bool instantSnapOnLadderEnter = true;
    [SerializeField, Range(0.01f, 1f)] private float centerLerp = 0.25f;
    private float? ladderCenterX;

    [Header("Aiming (4-dir)")]
    [SerializeField, Range(0.05f, 0.9f)] private float aimThreshold = 0.4f; // NEW: cuánto input exige para cambiar la dirección
    private Vector2 facing = Vector2.right; // NEW: derecha por defecto

    [Header("Weapon System")]
    [SerializeField] private WeaponData startWeapon;
    [SerializeField] private bool useReloadAnimEvent = false; // si llamas a AnimEvent_ReloadEnd


    // Estado actual de arma/munición
    public WeaponData currentWeapon;
    private int magAmmo;       // munición en cargador
    private int reserveAmmo;   // munición en reserva
    private bool isReloading;
    private float fireCdTimer;

    [Header("Health/Infection System")]
    [SerializeField] private HealthComponent health;
    [SerializeField] private InfectionQTE infection;
    [SerializeField] private Infectable infectable;

    // Evento simple para UI (suscríbete desde un script de UI)
    public System.Action<string, int, int> OnAmmoChanged; // (weaponId, magAmmo, reserveAmmo)

    // Animator hashes
    private static readonly int AnimSpeedX = Animator.StringToHash("SpeedX");
    private static readonly int AnimSpeedY = Animator.StringToHash("SpeedY");
    private static readonly int AnimMoving = Animator.StringToHash("IsMoving");
    private static readonly int AnimClimb = Animator.StringToHash("OnLadder");
    private static readonly int TrigShoot = Animator.StringToHash("Shoot");
    private static readonly int TrigMelee = Animator.StringToHash("Melee");
    private static readonly int TrigHurt = Animator.StringToHash("Hurt");
    private static readonly int Reset = Animator.StringToHash("Reset");
    private static readonly int FaceX = Animator.StringToHash("FaceX"); // NEW
    private static readonly int FaceY = Animator.StringToHash("FaceY"); // NEW

    private Rigidbody2D rb;
    private Vector2 input;
    private bool onLadder;
    private bool movementLocked;

    private float shootTimer;
    private float meleeTimer;
    private Vector3 initialSpawnPos;
    private bool dead;
    private bool binded;

    // Dash
    private bool isDashing;
    private float dashTimer;
    private Vector2 dashVelocity;

    // En PlayerController2D (campos públicos de solo lectura)
    public int CurrentMagSize => currentWeapon ? currentWeapon.magSize : 0;
    public string CurrentWeaponId => currentWeapon ? currentWeapon.weaponId : "";
    public int CurrentMagAmmo => magAmmo;
    public int CurrentReserveAmmo => reserveAmmo;

    // Lanza el evento actual para inicializar la UI cuando haga falta
    public void RefreshAmmoUI()
    {
        NotifyAmmo(); // ya invoca OnAmmoChanged(id, mag, reserve)
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (!animator) animator = GetComponentInChildren<Animator>(true);
        if (!sprite) sprite = GetComponentInChildren<SpriteRenderer>(true);
        initialSpawnPos = transform.position;

        // Melee hitboxes off
        if (meleeHitboxRight) meleeHitboxRight.enabled = false;
        if (meleeHitboxLeft) meleeHitboxLeft.enabled = false;
        if (meleeHitboxUp) meleeHitboxUp.enabled = false;
        if (meleeHitboxDown) meleeHitboxDown.enabled = false;

        EnsureMeleeDealer(meleeHitboxRight);
        EnsureMeleeDealer(meleeHitboxLeft);
        EnsureMeleeDealer(meleeHitboxUp);
        EnsureMeleeDealer(meleeHitboxDown);

        if (startWeapon != null) EquipWeapon(startWeapon);

        if (!health) health = GetComponent<HealthComponent>();
        if (!infection) infection = FindFirstObjectByType<InfectionQTE>();
        if (!infectable) infectable = GetComponent<Infectable>();
        if (health != null) health.OnDeath += OnPlayerDeath;
        if (infection != null) infection.OnQTESuccess.AddListener(ResetMovement);

    }

    void Update()
    {
        if (dead) return;

        var myInput = UnifiedInput.Instance;

        // INPUT
        float h = myInput.GetLeftAxis().x;
        float v = myInput.GetLeftAxis().y;

        // Actualizar facing con input (4 direcciones)
        UpdateFacing(h, v);

        input = movementLocked ? Vector2.zero : new Vector2(h, v);

        if (!onLadder && !binded)
        {
            // DISPARO con arma equipada
            if (!onLadder && currentWeapon != null && !isReloading)
            {
                fireCdTimer -= Time.deltaTime;

                bool wantsFire = (currentWeapon.fireMode == FireMode.Auto)
                    ? myInput.IsPressed(myInput.shootInput)
                    : myInput.WasPressedThisFrame(myInput.shootInput);

                if (wantsFire && fireCdTimer <= 0f)
                {
                    TryFireCurrentWeapon(h, v);
                }
            }

            // RECARGA
            if (!onLadder && currentWeapon != null && myInput.WasPressedThisFrame(myInput.reloadInput))
            {
                TryReload();
            }

            // Melee
            meleeTimer -= Time.deltaTime;
            if (myInput.WasPressedThisFrame(myInput.meeleInput) && meleeTimer <= 0f && !isReloading)
            {
                meleeTimer = meleeCooldown;
                StartCoroutine(CoDoMelee(h, v));
            }
        }

        // ANIM
        bool isMoving = onLadder
            ? (!movementLocked && Mathf.Abs(v) > 0.01f)
            : (!movementLocked && (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f));

        animator.SetBool(AnimMoving, isMoving);
        animator.SetBool(AnimClimb, onLadder && !movementLocked);
        animator.SetFloat(AnimSpeedX, Mathf.Abs(h));
        animator.SetFloat(AnimSpeedY, Mathf.Abs(v));
        animator.SetFloat(FaceX, facing.x);
        animator.SetFloat(FaceY, facing.y);

        // Flip sprite solo cuando miramos a izquierda/derecha (tu convención: flipX==true mira a DERECHA)
        if (!onLadder && !movementLocked && Mathf.Abs(facing.x) > 0.01f)
            sprite.flipX = facing.x > 0f;

        // Escalera: anim speed (como tenías)
        if (onLadder) animator.speed = isMoving ? 1f : 0f;
    }

    void FixedUpdate()
    {
        if (dead) return;

        // DASH PRIORIDAD
        if (isDashing)
        {
            rb.linearVelocity = dashVelocity;
            dashTimer -= Time.fixedDeltaTime;
            if (dashTimer <= 0f) isDashing = false;
            return;
        }

        if (movementLocked)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (onLadder)
        {
            rb.linearVelocity = new Vector2(0f, input.y * climbSpeed);

            if (clampXToLadderCenter && ladderCenterX.HasValue)
            {
                var pos = rb.position;
                float targetX = ladderCenterX.Value;
                pos.x = instantSnapOnLadderEnter ? targetX : Mathf.Lerp(pos.x, targetX, centerLerp);
                rb.position = pos;
            }
            return;
        }

        // Movimiento libre (colliders limitan)
        rb.linearVelocity = new Vector2(input.x * moveSpeedX, input.y * moveSpeedY);
    }

    // ---------- Aiming / Facing ----------
    private void UpdateFacing(float h, float v)
    {
        // Prioridad vertical si es dominante
        if (Mathf.Abs(v) >= aimThreshold && Mathf.Abs(v) >= Mathf.Abs(h))
            facing = v > 0 ? Vector2.up : Vector2.down;
        else if (Mathf.Abs(h) >= aimThreshold)
            facing = h > 0 ? Vector2.right : Vector2.left;
        // si no hay input, mantenemos facing actual
    }

    private Vector2 ComputeAimDirection(float h, float v)
    {
        // En el frame del ataque, decide hacia dónde
        if (Mathf.Abs(v) >= aimThreshold && Mathf.Abs(v) >= Mathf.Abs(h))
            return v > 0 ? Vector2.up : Vector2.down;
        if (Mathf.Abs(h) >= aimThreshold)
            return h > 0 ? Vector2.right : Vector2.left;
        return facing; // fallback a la última dirección conocida
    }

    // ---------- Melee ----------
    private IEnumerator CoDoMelee(float h, float v)
    {
        animator.SetTrigger(TrigMelee);

        if (meleeUseAnimEvents)
        {
            yield return null; // Si usas eventos, llama a StartMeleeDash/AnimEvent_MeleeStart/End desde la anim
        }
        else
        {
            if (lockMovementDuringMelee)
            {
                movementLocked = true;
                rb.linearVelocity = Vector2.zero;
            }
            // 1) Windup
            yield return new WaitForSeconds(meleeWindup);

            // 2) Delay opcional sincronizado
            if (meleeDashDelay > 0f)
                yield return new WaitForSeconds(meleeDashDelay);

            // 3) Dash en la dirección apuntada
            Vector2 aim = ComputeAimDirection(h, v);
            StartMeleeDash(aim, h, v);

            // 4) Lock de movimiento del input (el dash seguirá teniendo prioridad)

            // 5) Activar hitbox del lado correcto
            var hb = GetCurrentMeleeHitbox(aim);
            if (hb) hb.enabled = true;

            // 6) Ventana activa
            yield return new WaitForSeconds(meleeActiveTime);

            // 7) Desactivar
            if (hb) hb.enabled = false;
        }

        if (lockMovementDuringMelee) movementLocked = false;
    }

    private void StartMeleeDash(Vector2 aim, float h, float v)
    {
        // 1) Fallback si por lo que sea viene (0,0)
        Vector2 dir = (aim.sqrMagnitude < 0.0001f) ? facing : aim;

        // 2) Normaliza por seguridad (aunque aim ya sea cardinal)
        dir = dir.normalized;

        // 3) Snap determinista a cardinal (sin sorpresas)
        float ax = Mathf.Abs(dir.x);
        float ay = Mathf.Abs(dir.y);

        if (ax > ay)
        {
            dir = new Vector2(Mathf.Sign(dir.x), 0f);
        }
        else if (ay > ax)
        {
            dir = new Vector2(0f, Mathf.Sign(dir.y));
        }
        else
        {
            // Empate exacto (muy raro si aim es cardinal): desempata con el input del frame
            if (Mathf.Abs(h) > Mathf.Abs(v))
                dir = new Vector2(Mathf.Sign(h != 0 ? h : (facing.x == 0 ? 1f : facing.x)), 0f);
            else if (Mathf.Abs(v) > Mathf.Abs(h))
                dir = new Vector2(0f, Mathf.Sign(v != 0 ? v : (facing.y == 0 ? 1f : facing.y)));
            else
                // Sin input: usa facing
                dir = (Mathf.Abs(facing.x) >= Mathf.Abs(facing.y))
                    ? new Vector2(Mathf.Sign(facing.x == 0 ? 1f : facing.x), 0f)
                    : new Vector2(0f, Mathf.Sign(facing.y == 0 ? 1f : facing.y));
        }

        // 4) Aplica el dash
        dashVelocity = dir * meleeDashSpeed;
        isDashing = true;
        dashTimer = meleeDashDuration;

        // DEBUG opcional:
        // Debug.DrawRay(transform.position, (Vector3)dashVelocity.normalized * 0.75f, Color.cyan, 0.25f);
    }


    private Collider2D GetCurrentMeleeHitbox(Vector2 aim)
    {
        // Normaliza a cardinal
        if (Mathf.Abs(aim.x) > Mathf.Abs(aim.y))
            return (aim.x > 0 ? (meleeHitboxRight ? meleeHitboxRight : meleeHitboxLeft)
                              : (meleeHitboxLeft ? meleeHitboxLeft : meleeHitboxRight));
        else
            return (aim.y > 0 ? (meleeHitboxUp ? meleeHitboxUp : meleeHitboxDown)
                              : (meleeHitboxDown ? meleeHitboxDown : meleeHitboxUp));
    }

    // Eventos de anim si los usas
    public void AnimEvent_MeleeStart()
    {
        var hb = GetCurrentMeleeHitbox(facing);
        if (hb) hb.enabled = true;
    }

    public void AnimEvent_MeleeEnd()
    {
        var hb = GetCurrentMeleeHitbox(facing);
        if (hb) hb.enabled = false;
        if (lockMovementDuringMelee) movementLocked = false;
    }

    private void EnsureMeleeDealer(Collider2D col)
    {
        if (!col) return;
        var dealer = col.GetComponent<DamageDealer2D>();
        if (!dealer) dealer = col.gameObject.AddComponent<DamageDealer2D>();
        dealer.Configure(DamageKind.Melee, 1f, "Player");
        col.isTrigger = true;
    }

    // ---------- Projectile ----------
    private void SpawnProjectile(Vector2 aim)
    {
        if (!projectilePrefab) return;

        Transform socket = GetCurrentMuzzle(aim);

        // Rotación objetivo (sprite dibujado mirando a la DERECHA => eje +X)
        Quaternion rot = socket.rotation;
        if (orientProjectileToAim)
        {
            float ang = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
            rot = Quaternion.AngleAxis(ang, Vector3.forward);
        }

        GameObject proj = Instantiate(projectilePrefab, socket.position, rot);

        // Movimiento del proyectil (ya tienes versión vectorial)
        var mover = proj.GetComponent<SimpleProjectile2D>();
        if (!mover) mover = proj.AddComponent<SimpleProjectile2D>();
        mover.Launch(aim, projectileSpeed, projectileLifetime);

        // Si orientamos con rotación, NO hace falta flip. Si no orientas, mantenemos tu flip opcional.
        if (!orientProjectileToAim && projectileFlipWithFacing)
        {
            var ps = proj.GetComponentInChildren<SpriteRenderer>();
            if (ps != null)
            {
                ps.flipX = aim.x < 0f;
                ps.flipY = aim.y < 0f;
            }
            else
            {
                var s = proj.transform.localScale;
                s.x = Mathf.Abs(s.x) * (aim.x < 0f ? -1f : 1f);
                s.y = Mathf.Abs(s.y) * (aim.y < 0f ? -1f : 1f);
                proj.transform.localScale = s;
            }
        }
    }

    // ----------- EQUIPAR -----------
    public void EquipWeapon(WeaponData w)
    {
        currentWeapon = w;
        magAmmo = Mathf.Clamp(w.startMag, 0, w.magSize);
        reserveAmmo = Mathf.Max(0, w.startReserve);
        fireCdTimer = 0f;
        isReloading = false;
        movementLocked = false;
        NotifyAmmo();
    }

    // ----------- DISPARAR -----------
    private void TryFireCurrentWeapon(float h, float v)
    {
        if (currentWeapon == null) return;

        // ¿hay balas en cargador?
        if (magAmmo < currentWeapon.ammoPerShot)
        {
            // click vacío
            if (!string.IsNullOrEmpty(currentWeapon.dryFireTrigger))
                animator.SetTrigger(currentWeapon.dryFireTrigger);
            return;
        }

        // Cooldown
        fireCdTimer = currentWeapon.fireCooldown;

        // Anim de disparo + lock si ya lo hacías
        if (!string.IsNullOrEmpty(currentWeapon.shootTrigger))
            animator.SetTrigger(currentWeapon.shootTrigger);

        StartShootLock();

        // Calcular dirección de apuntado
        Vector2 aim = ComputeAimDirection(h, v);

        // VFX: usa override del arma si lo hay
        if (currentWeapon.muzzleVFXPrefab)
            SpawnShootVFX_Override(aim, currentWeapon.muzzleVFXPrefab);
        else
            SpawnShootVFX(aim);

        // Proyectil: usa override del arma si lo hay
        GameObject prefab = currentWeapon.projectilePrefab ? currentWeapon.projectilePrefab : projectilePrefab;
        SpawnProjectile_Override(aim, prefab);

        // Coste de munición
        magAmmo = Mathf.Max(0, magAmmo - currentWeapon.ammoPerShot);
        NotifyAmmo();
    }

    private void SpawnProjectile_Override(Vector2 aim, GameObject prefabOverride)
    {
        // Si no hay override, usa tu método existente
        if (!prefabOverride) { SpawnProjectile(aim); return; }

        Transform socket = GetCurrentMuzzle(aim);

        // Orientación: usa el mismo criterio que ya tienes
        Quaternion rot = Quaternion.identity;
        if (orientProjectileToAim)
        {
            float ang = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
            rot = Quaternion.AngleAxis(ang, Vector3.forward);
        }
        else rot = socket.rotation;

        var proj = Instantiate(prefabOverride, socket.position, rot);
        var mover = proj.GetComponent<SimpleProjectile2D>();
        if (!mover) mover = proj.AddComponent<SimpleProjectile2D>();
        mover.Launch(aim, projectileSpeed, projectileLifetime); // usa tu speed/lifetime globales o pásalos desde el arma si prefieres
    }

    private void SpawnShootVFX_Override(Vector2 aim, GameObject vfxOverride)
    {
        if (!vfxOverride) { SpawnShootVFX(aim); return; }

        Transform socket = GetCurrentMuzzle(aim);
        Quaternion rot = socket.rotation;
        if (orientMuzzleVFXToAim)
        {
            float ang = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
            rot = Quaternion.AngleAxis(ang, Vector3.forward);
        }

        var vfx = Instantiate(vfxOverride, socket.position, rot);
        if (currentWeapon.shellVFXPrefab) Instantiate(currentWeapon.shellVFXPrefab, socket.position, socket.rotation);
        if (!vfx.GetComponent<AutoDestroyVFX>()) vfx.AddComponent<AutoDestroyVFX>();
    }


    // ----------- RECARGAR -----------
    private void TryReload()
    {
        if (currentWeapon == null || isReloading) return;
        if (magAmmo >= currentWeapon.magSize) return;
        if (reserveAmmo <= 0) return;

        isReloading = true;

        // Animación de reload
        if (!string.IsNullOrEmpty(currentWeapon.isReloadingBool))
            animator.SetBool(currentWeapon.isReloadingBool, true);
        if (!string.IsNullOrEmpty(currentWeapon.reloadTrigger))
            animator.SetTrigger(currentWeapon.reloadTrigger);

        if (currentWeapon.lockMovementDuringReload)
        {
            movementLocked = true;
            rb.linearVelocity = Vector2.zero;
        }

        if (!useReloadAnimEvent)
        {
            // Recarga por tiempo fijo
            StopCoroutine(nameof(CoReload));
            StartCoroutine(CoReload(currentWeapon.reloadTime));
        }
        // Si usas anim event, espera a AnimEvent_ReloadEnd()
    }

    private IEnumerator CoReload(float t)
    {
        if (currentWeapon.magazineVFXPrefab) Instantiate(currentWeapon.magazineVFXPrefab, transform.position + Vector3.up, Quaternion.identity);
        yield return new WaitForSeconds(t);
        FinishReload();
    }

    // Llamar desde Animation Event al final de la animación de recarga (si useReloadAnimEvent = true)
    public void AnimEvent_ReloadEnd()
    {
        if (useReloadAnimEvent && isReloading) FinishReload();
    }

    private void FinishReload()
    {
        // Transferir balas de reserva al cargador
        int need = currentWeapon.magSize - magAmmo;
        int give = Mathf.Min(need, reserveAmmo);
        magAmmo += give;
        reserveAmmo -= give;

        // Reset flags/cooldown
        isReloading = false;
        fireCdTimer = 0f;

        if (!string.IsNullOrEmpty(currentWeapon.isReloadingBool))
            animator.SetBool(currentWeapon.isReloadingBool, false);

        if (currentWeapon.lockMovementDuringReload)
            movementLocked = false;

        NotifyAmmo();
    }

    public void GiveAmmo(string weaponId, int amount)
    {
        if (currentWeapon != null && currentWeapon.weaponId == weaponId)
        {
            reserveAmmo += Mathf.Max(0, amount);
            NotifyAmmo();
        }

    }

    private void NotifyAmmo()
    {
        if (OnAmmoChanged != null && currentWeapon != null)
            OnAmmoChanged.Invoke(currentWeapon.weaponId, magAmmo, reserveAmmo);
    }


    private Transform GetCurrentMuzzle(Vector2 aim)
    {
        // Cardinal por prioridad
        if (Mathf.Abs(aim.y) > Mathf.Abs(aim.x))
        {
            if (aim.y > 0 && shootMuzzleUp) return shootMuzzleUp;
            if (aim.y < 0 && shootMuzzleDown) return shootMuzzleDown;
        }
        else
        {
            if (aim.x > 0 && shootMuzzleRight) return shootMuzzleRight;
            if (aim.x < 0 && shootMuzzleLeft) return shootMuzzleLeft;
        }
        // Fallbacks
        if (shootMuzzle) return shootMuzzle;
        if (shootMuzzleRight && facing.x >= 0) return shootMuzzleRight;
        if (shootMuzzleLeft && facing.x < 0) return shootMuzzleLeft;
        return transform;
    }

    // ---------- VFX ----------
    private void SpawnShootVFX(Vector2 aim)
    {
        if (!currentWeapon.muzzleVFXPrefab) return;
        Transform socket = GetCurrentMuzzle(aim);

        Quaternion rot = socket.rotation;
        if (orientMuzzleVFXToAim)
        {
            float ang = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
            rot = Quaternion.AngleAxis(ang, Vector3.forward);
        }

        GameObject vfx = Instantiate(currentWeapon.muzzleVFXPrefab, socket.position, rot);
        if (currentWeapon.shellVFXPrefab) Instantiate(currentWeapon.shellVFXPrefab, socket.position, socket.rotation);

        if (!vfx.GetComponent<AutoDestroyVFX>()) vfx.AddComponent<AutoDestroyVFX>();

        // Si orientas por rotación, no espejes por escala para no fastidiar el ángulo
        if (!orientMuzzleVFXToAim && mirrorVFXWithFlip && sprite != null)
        {
            var s = vfx.transform.localScale;
            s.x = Mathf.Abs(s.x) * (facing.x > 0 ? 1f : -1f);
            vfx.transform.localScale = s;
        }
    }


    private Transform GetCurrentMuzzle() => GetCurrentMuzzle(facing); // compat

    // ---------- Shoot Lock ----------
    private void StartShootLock()
    {
        movementLocked = true;
        rb.linearVelocity = Vector2.zero;

        if (!unlockOnShootAnimEvent)
        {
            StopCoroutine(nameof(CoUnlockAfterShoot));
            StartCoroutine(nameof(CoUnlockAfterShoot));
        }
    }

    private IEnumerator CoUnlockAfterShoot()
    {
        yield return new WaitForSeconds(shootLockDuration);
        movementLocked = false;
    }

    public void AnimEvent_ShootEnd()
    {
        if (unlockOnShootAnimEvent) movementLocked = false;
    }

    // ---------- Damage / Respawn ----------
    public void TakeHitFrom(DamageDealer2D dealer)
    {
        if (health == null) { TakeHit(); return; }
        if (health.CurrentHP > 0)
        {
            animator.SetTrigger(TrigHurt);
            health.TakeDamage((int)dealer.Amount);
            if (dealer.Tag == DamageTag.Bite && health.CurrentHP > 0 && !binded)
            {
                StopAllCoroutines();
                infection.StartQTE(infectable, dealer.QTERequiredPresses, dealer.QTEDuration);
                animator.SetTrigger("Binded");
                StartCoroutine(LockMovementCoroutine(dealer.QTEDuration));
            }
            if (hurtVFXPrefab && hurtPoint) Instantiate(hurtVFXPrefab, hurtPoint.position, Quaternion.identity);
        }
    }

    private void ResetMovement()
    {
        binded = false;
        movementLocked = false;
    }

    private IEnumerator LockMovementCoroutine(float t)
    {
        binded = true;
        movementLocked = true;
        yield return new WaitForSeconds(t);
        binded = false;
        movementLocked = false;
    }



    private void OnPlayerDeath()
    {
        StopAllCoroutines();
        StartCoroutine(CoRespawn());
    }

    public void TakeHit()
    {
        if (health != null) health.TakeDamage(1);
    }

    private IEnumerator CoRespawn()
    {
        animator.ResetTrigger(TrigHurt);
        animator.Play("Die");
        dead = true;
        movementLocked = true;
        rb.linearVelocity = Vector2.zero;
        onLadder = false;
        yield return new WaitForSeconds(respawnDelay);


        Vector3 spawnPos = customSpawnPoint ? customSpawnPoint.position : initialSpawnPos;
        transform.position = spawnPos;
        movementLocked = false;
        binded = false;
        dead = false;
        FinishReload();
        health.FullHeal();
        animator.SetTrigger(Reset);
    }

    // ---------- Ladders ----------
    public void EnterLadder(float centerX)
    {
        onLadder = true;
        ladderCenterX = centerX;

        if (clampXToLadderCenter && instantSnapOnLadderEnter)
        {
            var pos = rb.position;
            pos.x = centerX;
            rb.position = pos;
        }

        // En escalera, orienta facing vertical si hay input
        if (Mathf.Abs(input.y) > aimThreshold) facing = input.y > 0 ? Vector2.up : Vector2.down;
    }

    public void ExitLadder()
    {
        onLadder = false;
        ladderCenterX = null;
    }
}
