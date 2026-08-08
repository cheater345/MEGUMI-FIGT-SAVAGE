using UnityEngine;

namespace SteelTempest.Player
{
    /// <summary>
    /// Marks a background layer for parallax drift. The camera shifts the
    /// layer horizontally at <see cref="factor"/> of its own movement,
    /// giving distant scenery an illusion of depth.
    /// </summary>
    public sealed class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] private float factor = 0.5f;

        public float Factor => factor;
        public Vector3 BasePosition { get; private set; }

        private void Awake()
        {
            BasePosition = transform.position;
        }
    }
}
