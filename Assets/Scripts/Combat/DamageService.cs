using UnityEngine;
using SteelTempest.Core.Events;

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
                EventBus.Instance.Publish(new DamageEvent(source, target, amount, isCrit, payload.finisher));
                if (payload.knockback > 0f && target.TryGetComponent<Rigidbody2D>(out var rb))
                {
                    var dir = Mathf.Sign(target.transform.position.x - source.transform.position.x);
                    rb.AddForce(new Vector2(dir * payload.knockback, payload.launches ? payload.knockback * 0.7f : 0f), ForceMode2D.Impulse);
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