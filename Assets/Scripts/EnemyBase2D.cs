using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public abstract class EnemyBase2D : MonoBehaviour
{
    public enum State { Idle, Patrol, Chase, Attacking, Stun, Dead }

    [Header("Refs (Child)")]
    [SerializeField] protected Animator animator;
    [SerializeField] protected SpriteRenderer sprite;
    [SerializeField] protected Rigidbody2D rb;

    [Header("Targeting")]
    [SerializeField] protected string playerTag = "Player";
    protected Transform player;

    [SerializeField] protected NavMeshAgent agent;

    [Header("Vision")]
    [SerializeField] protected bool useVisionCone = true;
    [SerializeField] protected float visionAngle = 90f;              // ángulo total del cono
    [SerializeField] protected LayerMask visionObstacleMask;         // capa de walls
    [SerializeField] protected Transform eyePoint;                   // opcional, punto desde el que "ve"
    protected Vector2 lookDirection = Vector2.right;

    [Header("Aggro")]
    [SerializeField] protected bool persistentAggro = true;     // si es true, no pierde el aggro nunca
    [SerializeField] protected bool alertAlliesOnAggro = true;  // avisa a aliados al entrar en aggro
    [SerializeField] protected float allyAlertRadius = 7f;      // radio de “grito”
    [SerializeField] protected LayerMask allyLayer;             // capa en la que están los enemigos
    [SerializeField] protected float alertCooldown = 1.0f;      // evita spam de alertas

    protected bool aggro;              // ¿está en modo persecución permanente?
    private float nextAlertTime = 0f;
    public bool IsAggro => aggro;

    [Header("Stun")]
    [SerializeField] protected float stunThreshold = 3f;   // cuántos “puntos” de disparos hasta stun
    [SerializeField] protected float stunDuration = 1.25f; // tiempo stuneado
    [SerializeField] protected bool refreshStunTimerOnRangedWhileStunned = true;

    [Header("VFX (opcionales)")]
    [SerializeField] protected GameObject hurtVFX;
    [SerializeField] protected GameObject dieVFX;
    [SerializeField] protected Transform vfxPoint;

    [Header("Colliders")]
    [SerializeField] protected Collider2D mainCollider;

    [Header("Colliders al morir")]
    [SerializeField] protected Collider2D[] disableOnDeath;

    // Animator hashes sugeridos
    protected static readonly int AnimMove = Animator.StringToHash("IsMoving");
    protected static readonly int TrigAttack = Animator.StringToHash("Attack");
    protected static readonly int TrigHurt = Animator.StringToHash("Hurt");
    protected static readonly int BoolStun = Animator.StringToHash("Stun");
    protected static readonly int TrigDie = Animator.StringToHash("Die");

    protected State state;
    protected float stunMeter;
    protected bool invulnerable; // por si quieres ventanas de i-frames

    [Header("Movement Lock")]
    [SerializeField] protected float hurtLockDuration = 0.15f; // tiempo inmóvil al recibir daño
    protected bool movementLocked;
    Coroutine movementUnlockCo;

    public bool IsMovementLocked => movementLocked;

    // -------- Horda / Grupo --------
    [Header("Horde / Group Behaviour")]
    [SerializeField] protected bool useHordeBehaviour = false;
    [SerializeField] protected bool isLeader = false;
    [SerializeField] protected int hordeId = 0; // para distinguir grupos si quieres

    [SerializeField] protected float hordeCohesionRadius = 4f;
    [SerializeField] protected float hordeSeparationRadius = 0.75f;
    [SerializeField] protected float hordeCohesionWeight = 0.25f;
    [SerializeField] protected float hordeSeparationWeight = 0.75f;
    [SerializeField] protected float hordeRandomOffsetRadius = 0.75f;

    [Header("Horde Idle Wander")]
    [SerializeField] protected bool useIdleWander = true;
    [SerializeField] protected float idleWanderRadius = 0.5f;
    [SerializeField] protected float idleWanderSpeed = 0.5f;


    protected Vector2 hordePersonalOffset;
    protected static readonly List<EnemyBase2D> AllEnemies = new List<EnemyBase2D>();

    protected Vector2 spawnPosition;
    float idleNoiseSeedX;
    float idleNoiseSeedY;

    protected void LockMovement(float duration)
    {
        movementLocked = true;
        if (rb) rb.linearVelocity = Vector2.zero;
        if (agent) agent.isStopped = true;
        if (movementUnlockCo != null) StopCoroutine(movementUnlockCo);
        movementUnlockCo = StartCoroutine(CoUnlockMovement(duration));
    }

    IEnumerator CoUnlockMovement(float t)
    {
        yield return new WaitForSeconds(t);
        if (agent) agent.isStopped = false;
        movementLocked = false;
    }

    protected void UpdateLookDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude > 0.01f)
            lookDirection = dir.normalized;
    }

    protected virtual void OnEnable()
    {
        if (!AllEnemies.Contains(this))
            AllEnemies.Add(this);

        SetupLeaderFollowerCollisions();
    }


    protected virtual void OnDisable()
    {
        AllEnemies.Remove(this);
    }

    protected virtual void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!animator) animator = GetComponentInChildren<Animator>(true);
        if (!sprite) sprite = GetComponentInChildren<SpriteRenderer>(true);
        if (!mainCollider) mainCollider = GetComponent<Collider2D>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    protected virtual void Start()
    {
        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p) player = p.transform;
        state = State.Idle;

        agent = GetComponent<NavMeshAgent>();
        if (agent)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }

        // ===== Horde / Idle seeds =====
        spawnPosition = transform.position;
        idleNoiseSeedX = Random.value * 1000f;
        idleNoiseSeedY = Random.value * 1000f;
    }

    protected virtual void Update()
    {
        if (state == State.Dead) return;
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag(playerTag);
            if (p) player = p.transform;
        }
    }

    // --------- Daño / Stun ----------
    public virtual void ReceiveHit(DamageDealer2D dealer, Collider2D source)
    {
        if (state == State.Dead || invulnerable) return;
        if (dealer.OwnerTag != playerTag) return; // solo nos afectan golpes del Player

        switch (dealer.Kind)
        {
            case DamageKind.Ranged:
                OnRangedHit(dealer.Amount, source);
                break;

            case DamageKind.Melee:
                OnMeleeHit(dealer.Amount, source);
                break;
        }
    }

    protected virtual void OnRangedHit(float amount, Collider2D src)
    {
        if (hurtVFX && vfxPoint) Instantiate(hurtVFX, vfxPoint.position, Quaternion.identity);

        // En cuanto me disparan: aggro y aviso (si procede)
        ForceAggro(target: player, cascade: true);

        // Parón breve + lógica existente
        LockMovement(hurtLockDuration);

        if (state == State.Stun)
        {
            if (refreshStunTimerOnRangedWhileStunned) RefreshStunTimer();
            return;
        }

        stunMeter += amount;
        animator?.SetTrigger(TrigHurt);

        if (stunMeter >= stunThreshold) EnterStun();
    }

    protected virtual void OnMeleeHit(float amount, Collider2D src)
    {
        // Golpean en melee: aggro fijo
        ForceAggro(target: player, cascade: true);

        if (state == State.Stun)
        {
            Die();
        }
        else
        {
            LockMovement(hurtLockDuration);
            animator?.SetTrigger(TrigHurt);
        }
    }

    protected Coroutine stunCo;
    protected float stunEndTime;

    protected virtual void EnterStun()
    {
        if (state == State.Dead) return;

        state = State.Stun;
        rb.linearVelocity = Vector2.zero;
        if (agent) agent.isStopped = true;
        animator?.SetBool(BoolStun, true);
        stunMeter = 0f; // reseteamos la “barra” al entrar en stun

        if (stunCo != null) StopCoroutine(stunCo);
        stunCo = StartCoroutine(CoStunTimer());
    }

    protected virtual IEnumerator CoStunTimer()
    {
        stunEndTime = Time.time + stunDuration;
        while (Time.time < stunEndTime && state == State.Stun)
            yield return null;

        ExitStun();
    }

    protected void RefreshStunTimer()
    {
        if (state != State.Stun) return;
        stunEndTime = Time.time + stunDuration;
    }

    protected virtual void ExitStun()
    {
        if (state != State.Stun) return;
        animator?.SetBool(BoolStun, false);
        state = State.Idle;
    }

    protected virtual void Die()
    {
        if (state == State.Dead) return;
        state = State.Dead;

        aggro = false;

        rb.linearVelocity = Vector2.zero;
        movementLocked = true;

        //Death Particles
        StartCoroutine(CoDie());
    }

    private IEnumerator CoDie()
    {
        if (hurtVFX && vfxPoint) Instantiate(hurtVFX, vfxPoint.position, Quaternion.Euler(0, 0, Random.Range(0, 360)));

        if (dieVFX) Instantiate(dieVFX, transform.position, Quaternion.identity);
        Material mat = sprite.material;
        float threshold = 1;
        while (threshold > 0)
        {
            mat.SetFloat("_Threshold", threshold);
            yield return null;
            threshold -= Time.unscaledDeltaTime;
            sprite.material = mat;
        }

        foreach (var c in disableOnDeath) if (c) c.enabled = false;

        // Destruye tras la anim si tienes evento; si no, fallback:
        Destroy(gameObject, 1.5f);
    }

    // Helpers de orientación para hijos
    protected void FlipTowards(float targetX)
    {
        if (!sprite) return;
        float dir = Mathf.Sign(targetX - transform.position.x);
        if (Mathf.Abs(dir) > 0.01f) sprite.flipX = dir > 0f;
    }

    protected bool CanThink => state != State.Stun && state != State.Dead;

    public virtual void ForceAggro(Transform target = null, bool cascade = true)
    {
        if (target) player = target;
        SetAggro(true, cascade);
    }

    protected virtual void SetAggro(bool value, bool cascade)
    {
        aggro = value;

        if (aggro && useHordeBehaviour && hordePersonalOffset == Vector2.zero)
        {
            // Offset personal para que no se apilen
            hordePersonalOffset = Random.insideUnitCircle * hordeRandomOffsetRadius;
        }

        if (aggro && cascade && alertAlliesOnAggro && Time.time >= nextAlertTime)
        {
            AlertAllies();
            nextAlertTime = Time.time + alertCooldown;
        }
    }

    protected virtual void AlertAllies()
    {
        if (!player) return;
        var hits = Physics2D.OverlapCircleAll(transform.position, allyAlertRadius, allyLayer);
        for (int i = 0; i < hits.Length; i++)
        {
            var e = hits[i].GetComponentInParent<EnemyBase2D>();
            if (e && e != this && !e.state.Equals(State.Dead))
            {
                e.OnAllyAlert(player);
            }
        }
    }

    public virtual void OnAllyAlert(Transform target)
    {
        if (!persistentAggro && aggro) return; // ya está en aggro
        if (target) player = target;
        SetAggro(true, cascade: false);
    }

    /// <summary>
    /// Devuelve una posición ajustada para que el enemigo se mueva más como horda:
    /// - cohesion: se acerca al centro de los aliados cercanos.
    /// - separation: no se apilan uno encima de otro.
    /// </summary>
    protected Vector2 GetHordeAdjustedDestination(Vector2 desiredBasePosition)
    {
        if (!useHordeBehaviour || !aggro || AllEnemies.Count <= 1)
            return desiredBasePosition;

        Vector2 selfPos = transform.position;
        Vector2 cohesion = Vector2.zero;
        Vector2 separation = Vector2.zero;
        int neighbours = 0;

        for (int i = 0; i < AllEnemies.Count; i++)
        {
            var e = AllEnemies[i];
            if (e == this || !e.aggro || e.state == State.Dead) continue;

            Vector2 otherPos = e.transform.position;
            float d = Vector2.Distance(selfPos, otherPos);
            if (d > hordeCohesionRadius) continue;

            neighbours++;
            cohesion += otherPos;

            if (d < hordeSeparationRadius && d > 0.01f)
            {
                separation += (selfPos - otherPos) / d; // empujoncito
            }
        }

        if (neighbours == 0)
            return desiredBasePosition;

        cohesion /= neighbours;
        Vector2 cohesionDir = (cohesion - selfPos);

        Vector2 offset =
            cohesionDir * hordeCohesionWeight +
            separation * hordeSeparationWeight;

        return desiredBasePosition + offset;
    }

    protected EnemyBase2D GetLeader()
    {
        if (isLeader && state != State.Dead) return this;
        if (!useHordeBehaviour) return null;

        for (int i = 0; i < AllEnemies.Count; i++)
        {
            var e = AllEnemies[i];
            if (e == null || e == this) continue;
            if (!e.useHordeBehaviour) continue;
            if (e.hordeId != this.hordeId) continue;
            if (!e.isLeader) continue;
            if (e.state == State.Dead) continue;

            return e;
        }

        return null;
    }
    protected Vector2 GetHordeIdleOffset()
    {
        if (!useHordeBehaviour || !useIdleWander)
            return hordePersonalOffset;

        float t = Time.time * idleWanderSpeed;

        float ox = (Mathf.PerlinNoise(idleNoiseSeedX, t) - 0.5f) * 2f * idleWanderRadius;
        float oy = (Mathf.PerlinNoise(idleNoiseSeedY, t) - 0.5f) * 2f * idleWanderRadius;

        return hordePersonalOffset + new Vector2(ox, oy);
    }

    void SetupLeaderFollowerCollisions()
    {
        if (!useHordeBehaviour || !mainCollider) return;

        for (int i = 0; i < AllEnemies.Count; i++)
        {
            var e = AllEnemies[i];
            if (e == null || e == this) continue;
            if (!e.useHordeBehaviour) continue;
            if (e.hordeId != this.hordeId) continue;
            if (!e.mainCollider) continue;

            // Ignoramos colisión entre líder y seguidores del mismo grupo
            if (isLeader && !e.isLeader)
            {
                Physics2D.IgnoreCollision(mainCollider, e.mainCollider, true);
            }
            else if (!isLeader && e.isLeader)
            {
                Physics2D.IgnoreCollision(mainCollider, e.mainCollider, true);
            }
        }
    }
    /// <summary>
    /// Comprueba si este enemigo puede ver al jugador dentro de un rango.
    /// </summary>
    protected bool CanSeePlayer(float maxRange)
    {
        if (!player) return false;
        return CanSeeTarget(player, maxRange);
    }

    /// <summary>
    /// Visión + FOV + línea de visión contra obstáculos.
    /// </summary>
    protected bool CanSeeTarget(Transform target, float maxRange)
    {
        if (!target) return false;

        Vector2 origin = eyePoint ? (Vector2)eyePoint.position : (Vector2)transform.position;
        Vector2 toTarget = (Vector2)target.position - origin;
        float dist = toTarget.magnitude;

        if (dist > maxRange)
            return false;

        Vector2 dir = toTarget.normalized;

        // 1) Cono de visión
        if (useVisionCone)
        {
            Vector2 forward = lookDirection;

            float angle = Vector2.Angle(forward, dir);
            if (angle > visionAngle * 0.5f)
                return false;
        }

        // 2) Línea de visión contra obstáculos (walls)
        if (visionObstacleMask.value != 0)
        {
            RaycastHit2D hit = Physics2D.Raycast(origin, dir, dist, visionObstacleMask);
            if (hit.collider != null)
            {
                // Hay algo entre medias antes de llegar al player
                return false;
            }
        }

        return true;
    }

}
