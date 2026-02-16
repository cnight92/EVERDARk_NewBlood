using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class AutoDestroyVFX : MonoBehaviour
{
    [Tooltip("Si no detecta fin (loops), destruir tras este tiempo.")]
    [SerializeField] private float maxLifetimeFallback = 2f;
    [SerializeField] private bool detachFromParentOnPlay = false;

    private void OnEnable()
    {
        if (detachFromParentOnPlay) transform.SetParent(null, true);
        StartCoroutine(CoWatchAndDestroy());
    }

    private IEnumerator CoWatchAndDestroy()
    {
        float t = 0f;
        var anim = GetComponent<Animator>();
        var particles = GetComponentsInChildren<ParticleSystem>(true);

        while (true)
        {
            bool animPlaying = false;
            if (anim)
            {
                var st = anim.GetCurrentAnimatorStateInfo(0);
                // Si el estado NO es loop y aún no llegó al final
                if (!st.loop && st.normalizedTime < 0.99f) animPlaying = true;
            }

            bool particlesAlive = false;
            for (int i = 0; i < particles.Length; i++)
            {
                if (particles[i] && particles[i].IsAlive(true))
                {
                    particlesAlive = true;
                    break;
                }
            }

            if (!animPlaying && !particlesAlive) break;

            t += Time.deltaTime;
            if (t >= maxLifetimeFallback) break;
            yield return null;
        }

        Destroy(gameObject);
    }
}
