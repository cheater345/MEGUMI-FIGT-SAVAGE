using UnityEngine;
using SteelTempest.Core.Events;
using SteelTempest.Core.Fx;

namespace SteelTempest.Combat
{
    /// <summary>Describes an incoming hit before it is resolved.</summary>
    public struct DamagePayload
    {
        public float amount;
        public float knockback;
        public float critChance;
        public float critMultiplier;
        public bool launches;
        public bool finisher;
    }

    /// <summary>
    /// Central combat math. Resolves damage against a target's
    /// <see cref="HealthComponent"/> and publishes combat events.
    /// Also owns the shared slow-motion finisher time dilation.
    /// </summary>
    public static class DamageService
    {
        public static bool ApplyDamage(GameObject source, GameObject target, DamagePayload payload)
        {
            var health = target.GetComponent<HealthComponent>();
            if (health == null || health.IsDead) return false;

            var isCrit = Random.value < payload.critChance;
            var amount = payload.amount * (isCrit ? payload.critMultiplier : 1f);

            if (health.TakeDamage(amount, out _))
            {
                var dir = Mathf.Sign(target.transform.position.x - source.transform.position.x);
                var weight = 1f + Mathf.Clamp01(payload.knockback / 12f);
                var sparkColor = target.CompareTag("Player")
                    ? new Color(1f, 0.6f, 0.35f)
                    : new Color(0.85f, 0.92f, 1f);
                ImpactFx.Spawn(
                    target.transform.position + new Vector3(dir * 0.35f, 0.65f, 0f),
                    sparkColor,
                    Mathf.RoundToInt(6 + weight * 6),
                    weight);

                EventBus.Instance.Publish(new DamageEvent(source, target, amount, isCrit, payload.finisher));
                if (payload.knockback > 0f && target.TryGetComponent<Rigidbody2D>(out var rb))
                {
                    rb.AddForce(new Vector2(dir * payload.knockback, payload.launches ? payload.knockback * 0.7f : 0f), ForceMode2D.Impulse);
                }
                if (payload.knockback >= 7f)
                {
                    CameraShake.Instance?.Shake(0.5f + Mathf.Clamp01(payload.knockback / 16f));
                }
                if (payload.launches)
                {
                    CameraShake.Instance?.Shake(1.2f);
                }
                if (health.IsDead)
                {
                    ImpactFx.Spawn(target.transform.position + Vector3.up * 0.7f, Color.white, 24, 2f);
                    CameraShake.Instance?.Shake(1.6f);
                }
                if (payload.finisher)
                {
                    TriggerFinisherSlowMo();
                }
                return true;
            }
            return false;
        }

        /// <summary>Scaled time used by enemies/player that respects the finisher dilation.</summary>
        public static float FinisherTimeScale { get; internal set; } = 1f;

        private static void TriggerFinisherSlowMo()
        {
            FinisherTimeScale = 0.25f;
            Time.timeScale = 0.25f;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            FinisherTimeController.Instance?.Begin();
        }
    }

    /// <summary>
    /// Restores normal time after the finisher cinematic window.
    /// A MonoBehaviour is used so coroutines run on the main thread.
    /// </summary>
    public sealed class FinisherTimeController : MonoBehaviour
    {
        private static FinisherTimeController _instance;
        public static FinisherTimeController Instance => _instance ??= new GameObject(nameof(FinisherTimeController)).AddComponent<FinisherTimeController>();

        public void Begin() => StartCoroutine(RestoreRoutine());

        private System.Collections.IEnumerator RestoreRoutine()
        {
            yield return new UnityEngine.WaitForSecondsRealtime(0.9f);
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            DamageService.FinisherTimeScale = 1f;
            yield return null;
        }
    }
}