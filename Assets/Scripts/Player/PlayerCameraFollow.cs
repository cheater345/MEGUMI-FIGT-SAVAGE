using UnityEngine;

namespace SteelTempest.Player
{
    /// <summary>
    /// Smooth 2D camera follow with a fixed z depth and configurable look-ahead.
    /// Attached to the main camera; picks up the player when absent.
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

        private void Awake()
        {
            if (_target == null)
            {
                var marker = FindObjectOfType<SteelTempest.Enemies.PlayerMarker>();
                _target = marker ? marker.transform : null;
            }
        }

        private void LateUpdate()
        {
            if (_target == null) return;
            var desired = _target.position + offset;
            desired.y = Mathf.Clamp(desired.y, minY, maxY);
            desired.x = Mathf.Clamp(desired.x, minX, maxX);
            var current = transform.position;
            transform.position = Vector3.Lerp(current, desired, Time.deltaTime * followSpeed);
        }
    }
}