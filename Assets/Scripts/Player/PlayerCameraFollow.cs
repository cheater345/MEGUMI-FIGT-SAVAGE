using UnityEngine;
using SteelTempest.Core.Fx;

namespace SteelTempest.Player
{
    /// <summary>
    /// Smooth 2D camera follow with a fixed z depth and configurable look-ahead.
    /// Attached to the main camera; picks up the player when absent.
    /// Also drifts any <see cref="ParallaxLayer"/> scenery on screen and
    /// applies the optional <see cref="CameraShake"/> offset on top.
    /// </summary>
    public sealed class PlayerCameraFollow : MonoBehaviour
    {
        [SerializeField] private float followSpeed = 6f;
        [SerializeField] private Vector3 offset = new(0f, 0f, -10f);
        [SerializeField] private float minY = -3f;
        [SerializeField] private float maxY = 6f;
        [SerializeField] private float minX = -45f;
        [SerializeField] private float maxX = 45f;

        private Transform _target;
        private ParallaxLayer[] _parallax;
        private float _startX;

        private void Awake()
        {
            if (_target == null)
            {
                var marker = FindObjectOfType<SteelTempest.Enemies.PlayerMarker>();
                _target = marker ? marker.transform : null;
            }
            _parallax = FindObjectsOfType<ParallaxLayer>();
            _startX = transform.position.x;
        }

        private void LateUpdate()
        {
            if (_target == null) return;
            var desired = _target.position + offset;
            desired.y = Mathf.Clamp(desired.y, minY, maxY);
            desired.x = Mathf.Clamp(desired.x, minX, maxX);
            var current = transform.position;
            current = Vector3.Lerp(current, desired, Time.deltaTime * followSpeed);
            if (CameraShake.Instance != null)
            {
                current += CameraShake.Instance.Offset;
            }
            transform.position = current;

            var travel = transform.position.x - _startX;
            for (var i = 0; i < _parallax.Length; i++)
            {
                var layer = _parallax[i];
                var basePos = layer.BasePosition;
                layer.transform.position = new Vector3(
                    basePos.x + travel * layer.Factor,
                    layer.transform.position.y,
                    layer.transform.position.z);
            }
        }
    }
}