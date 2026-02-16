using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BasicChaseMeleeEnemy : EnemyBase2D
{
    [Header("Movement/AI")]
    [SerializeField] private float detectionRange = 6f;
    [SerializeField] private float loseAggroRange = 8f;
    [SerializeField] private float attackRange = 1.1f;

    [Header("Patrol")]
    [SerializeField] private bool enablePatrol = false;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolWaitTime = 1.0f;

    private enum PatrolMode { Loop, PingPong }
    [SerializeField] private PatrolMode patrolMode = PatrolMode.Loop;

    private int patrolIndex = 0;
    private int patrolDirection = 1;
    private float patrolWaitTimer = 0f;

    [Header("Attack")]
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private float attackWindup = 0.15f; // tiempo antes de activar el hitbox
    [SerializeField] private float attackActiveTime = 0.15f; // ventana activa del golpe
    [SerializeField] private List<EnemyAttackHitbox> attackHitboxes; // hijo con collider isTrigger (desactivado por defecto)

    private float attackTimer;

    protected override void Update()
    {
        base.Update();
        if (!player || state == State.Dead) return;

        attackTimer -= Time.deltaTime;

        // No moverse si hay locks/ataque/stun
        if (movementLocked || state == State.Attacking || !CanThink)
        {
            if (rb) rb.linearVelocity = Vector2.zero;
            if (agent) agent.isStopped = true;
            animator?.SetBool(AnimMove, false);
            return;
        }

        float dist = Vector2.Distance(transform.position, player.position);

        // --- VISIÓN + RANGO ---
        bool hasLoS = CanSeePlayer(detectionRange);   // usa cono + raycast contra walls

        // Si ya está en Aggro => sigue persiguiendo
        // Si no, solo entra si tiene visión + rango
        bool shouldChase = aggro || hasLoS;

        if (!aggro && hasLoS)
        {
            // Primera vez que ve al player => entra en aggro y “grita”
            ForceAggro(player, cascade: true);
        }

        if (shouldChase)
        {
            HandleChase(dist);
        }
        else
        {
            HandleNoAggroBehaviour();
        }
    }


    private void HandleChase(float dist)
    {
        if (!agent) return;

        UpdateLookDirection((player.position - transform.position));

        FlipTowards(player.position.x);

        if (dist > attackRange)
        {
            state = State.Chase;

            agent.isStopped = false;

            // Destino base: player + offset personal de horda
            Vector2 desired = (Vector2)player.position + hordePersonalOffset;
            Vector2 finalDest = GetHordeAdjustedDestination(desired);

            agent.SetDestination(finalDest);
            animator?.SetBool(AnimMove, true);
        }
        else
        {
            if (rb) rb.linearVelocity = Vector2.zero;
            animator?.SetBool(AnimMove, false);

            if (attackTimer <= 0f)
            {
                StartCoroutine(CoDoAttack());
                attackTimer = attackCooldown;
            }
        }

        // Si puede perder el aggro, comprobamos distancia
        if (!persistentAggro && dist > loseAggroRange)
        {
            aggro = false;
            state = State.Idle;
            agent.isStopped = true;
            if (rb) rb.linearVelocity = Vector2.zero;
            animator?.SetBool(AnimMove, false);
        }
    }

    private void HandleNoAggroBehaviour()
    {
        // Si hay patrulla configurada, usamos la lógica de patrulla (ver siguiente punto)
        if (enablePatrol && patrolPoints != null && patrolPoints.Length > 0)
        {
            HandlePatrol();
            return;
        }

        // SIN patrulla, pero con comportamiento de horda: idle “vivo”
        if (useHordeBehaviour && agent)
        {
            state = State.Idle;
            agent.isStopped = false;

            // Centro de referencia: el líder si existe, si no, la posición de spawn
            EnemyBase2D leader = GetLeader();
            Vector2 center = leader ? (Vector2)leader.transform.position : spawnPosition;

            Vector2 target = center + GetHordeIdleOffset();
            target = GetHordeAdjustedDestination(target);

            agent.SetDestination(target);

            UpdateLookDirection(agent.steeringTarget - transform.position);

            bool isMoving = agent.hasPath && agent.remainingDistance > agent.stoppingDistance + 0.02f;
            animator?.SetBool(AnimMove, isMoving);

            if (agent.hasPath)
                FlipTowards(agent.steeringTarget.x);
        }
        else
        {
            // Idle normal, quieto
            state = State.Idle;
            if (agent) agent.isStopped = true;
            if (rb) rb.linearVelocity = Vector2.zero;
            animator?.SetBool(AnimMove, false);
        }
    }



    private void HandlePatrol()
    {
        if (!agent) return;


        EnemyBase2D leader = GetLeader();

        // === CASO LÍDER o sin líder (fallback: patrulla normal) ===
        if (isLeader || leader == null)
        {
            if (state != State.Patrol)
            {
                state = State.Patrol;
                patrolIndex = GetClosestPatrolIndex();
                patrolDirection = 1;
                patrolWaitTimer = 0f;
                agent.isStopped = false;
                if (patrolPoints != null && patrolPoints.Length > 0)
                    agent.SetDestination(patrolPoints[patrolIndex].position);
            }

            // Llegada al waypoint actual
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.05f)
                {
                    if (patrolWaitTime > 0f)
                    {
                        patrolWaitTimer -= Time.deltaTime;
                        if (patrolWaitTimer <= 0f)
                        {
                            AdvancePatrolIndex();
                        }
                    }
                    else
                    {
                        AdvancePatrolIndex();
                    }
                }
            }

            bool isMoving = agent.hasPath && agent.remainingDistance > agent.stoppingDistance + 0.02f;
            animator?.SetBool(AnimMove, isMoving);

            if (agent.hasPath)
                UpdateLookDirection(agent.steeringTarget - transform.position);

            if (agent.hasPath)
                FlipTowards(agent.steeringTarget.x);

            return;
        }

        // === CASO SEGUIDOR DE HORDA EN PATRULLA ===
        // Hay líder y este enemigo NO es líder
        state = State.Patrol;
        agent.isStopped = false;

        Vector2 basePos = leader.transform.position;

        // Un poco de offset + lógica de cohesión/separación: se “agrupan” alrededor del líder
        Vector2 desired = basePos + GetHordeIdleOffset(); // pequeños movimientos relativos
        Vector2 finalDest = GetHordeAdjustedDestination(desired);

        agent.SetDestination(finalDest);

        if (agent.hasPath)
            UpdateLookDirection(agent.steeringTarget - transform.position);

        bool moving = agent.hasPath && agent.remainingDistance > agent.stoppingDistance + 0.02f;
        animator?.SetBool(AnimMove, moving);

        if (agent.hasPath)
            FlipTowards(agent.steeringTarget.x);
    }


    private void AdvancePatrolIndex()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        switch (patrolMode)
        {
            case PatrolMode.Loop:
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                break;

            case PatrolMode.PingPong:
                patrolIndex += patrolDirection;
                if (patrolIndex >= patrolPoints.Length)
                {
                    patrolDirection = -1;
                    patrolIndex = Mathf.Clamp(patrolIndex - 1, 0, patrolPoints.Length - 1);
                }
                else if (patrolIndex < 0)
                {
                    patrolDirection = 1;
                    patrolIndex = Mathf.Clamp(patrolIndex + 1, 0, patrolPoints.Length - 1);
                }
                break;
        }

        patrolWaitTimer = patrolWaitTime;
        agent.SetDestination(patrolPoints[patrolIndex].position);
    }

    private int GetClosestPatrolIndex()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return 0;

        int bestIndex = 0;
        float bestDist = float.MaxValue;
        Vector2 pos = transform.position;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (!patrolPoints[i]) continue;
            float d = Vector2.Distance(pos, patrolPoints[i].position);
            if (d < bestDist)
            {
                bestDist = d;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private IEnumerator CoDoAttack()
    {
        if (!CanThink) yield break;

        state = State.Attacking;
        if (rb) rb.linearVelocity = Vector2.zero;
        if (agent) agent.isStopped = true;
        animator?.SetTrigger(TrigAttack);

        // Bloquea el movimiento mientras dura el ataque (windup + ventana activa)
        LockMovement(attackWindup + attackActiveTime);

        // Windup
        yield return new WaitForSeconds(attackWindup);

        // Activar hitbox
        int hitbox = Random.Range(0, attackHitboxes.Count);
        if (attackHitboxes.Count > 0)
        {
            attackHitboxes[hitbox].SetActive(true);
        }

        // Ventana activa
        yield return new WaitForSeconds(attackActiveTime);

        if (attackHitboxes.Count > 0) attackHitboxes[hitbox].SetActive(false);
        if (agent) agent.isStopped = false;
        if (state != State.Dead && state != State.Stun)
            state = State.Chase;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0.6f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = new Color(1, 0, 0, 0.6f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, allyAlertRadius);

        // Patrol gizmos
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (!patrolPoints[i]) continue;
                Gizmos.DrawWireSphere(patrolPoints[i].position, 0.15f);

                int next = (i + 1) % patrolPoints.Length;
                if (patrolMode == PatrolMode.PingPong && i == patrolPoints.Length - 1) continue;
                if (patrolPoints[next])
                    Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[next].position);
            }
        }

        // Cono de visión
        if (useVisionCone)
        {
            Vector2 origin = Application.isPlaying && eyePoint ? (Vector2)eyePoint.position : (Vector2)transform.position;

            Vector2 forward = lookDirection;

            float halfAngle = visionAngle * 0.5f * Mathf.Deg2Rad;

            Vector2 dirA = new Vector2(
                forward.x * Mathf.Cos(halfAngle) - forward.y * Mathf.Sin(halfAngle),
                forward.x * Mathf.Sin(halfAngle) + forward.y * Mathf.Cos(halfAngle)
            );

            Vector2 dirB = new Vector2(
                forward.x * Mathf.Cos(-halfAngle) - forward.y * Mathf.Sin(-halfAngle),
                forward.x * Mathf.Sin(-halfAngle) + forward.y * Mathf.Cos(-halfAngle)
            );

            Gizmos.color = new Color(0f, 1f, 0.3f, 0.35f);
            Gizmos.DrawLine(origin, origin + dirA * detectionRange);
            Gizmos.DrawLine(origin, origin + dirB * detectionRange);
        }
    }

}
