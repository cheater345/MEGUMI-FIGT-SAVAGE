using System.Collections;
using UnityEngine;

namespace SteelTempest.Core.Fx
{
    /// <summary>
    /// Non-physics camera shake for impacts, finishers and parries.
    /// Attach to the main camera; call <see cref="Shake"/> with an intensity.
    /// </summary>
    public sealed class CameraShake : MonoBehaviour
    {
        private static CameraShake _instance;
        public static CameraShake Instance => _instance;

        [SerializeField] private float duration = 0.2f;
        [SerializeField] private float maxIntensity = 0.35f;

        private Vector3 _base;
        private Coroutine _routine;

        private void Awake()
        {
            _instance = this;
            _base = transform.localPosition;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        public void Shake(float intensity = 1f)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(Routine(Mathf.Clamp01(intensity * maxIntensity)));
        }

        private IEnumerator Routine(float magnitude)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                var t = 1f - (elapsed / duration);
                var mag = magnitude * t;
                transform.localPosition = _base + new Vector3(
                    Random.Range(-mag, mag),
                    Random.Range(-mag, mag),
                    0f);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            transform.localPosition = _base;
            _routine = null;
        }
    }
}