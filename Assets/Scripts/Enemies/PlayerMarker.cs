using UnityEngine;

namespace SteelTempest.Enemies
{
    /// <summary>
    /// Tagging marker on the player root used by enemies to find their target
    /// without string tags or FindByType ambiguity.
    /// </summary>
    public sealed class PlayerMarker : MonoBehaviour
    {
        public static PlayerMarker Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}