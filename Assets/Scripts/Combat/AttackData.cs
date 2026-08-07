using UnityEngine;

namespace SteelTempest.Combat
{
    /// <summary>
    /// Data-driven descriptor of a single attack action.
    /// Assets are authored as ScriptableObjects by the design team.
    /// </summary>
    [CreateAssetMenu(menuName = "SteelTempest/Combat/Attack Data", fileName = "AttackData")]
    public sealed class AttackData : ScriptableObject
    {
        [Header("Timing")]
        public float startupSeconds = 0.1f;
        public float activeSeconds = 0.15f;
        public float recoverySeconds = 0.2f;

        [Header("Damage")]
        public float damage = 10f;
        public float knockbackForce = 3f;
        public float critChance = 0.05f;
        public float critMultiplier = 1.5f;

        [Header("Range")]
        public float reach = 1.1f;
        public float height = 0.9f;
        public float forwardOffset = 0.35f;

        [Header("Flags")]
        public bool launches;         // lifts target into the air
        public bool isFinisher;       // triggers slow-motion cinematic hit
        public float chargeSeconds;   // >0 => charged (hold) attack
    }
}