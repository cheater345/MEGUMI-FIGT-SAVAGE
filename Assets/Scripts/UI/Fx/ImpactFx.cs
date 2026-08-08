using System.Collections.Generic;
using UnityEngine;

namespace SteelTempest.Core.Fx
{
    /// <summary>
    /// Cheap pooled impact sparks: a quick burst of glowing motes that
    /// expand, arc slightly and fade. Gives melee hits their "weight".
    /// </summary>
    public sealed class ImpactFx : MonoBehaviour
    {
        private static ImpactFx _instance;
        public static ImpactFx Instance => _instance;

        [SerializeField] private int poolSize = 64;
        [SerializeField] private float moteScale = 0.18f;

        private readonly Queue<SparkMote> _pool = new Queue<SparkMote>();
        private readonly List<SparkMote> _active = new List<SparkMote>();
        private Sprite _sprite;

        private sealed class SparkMote
        {
            public Transform t;
            public SpriteRenderer sr;
            public Vector3 velocity;
            public float life;
            public float maxLife;
            public Color start;
        }

        private void Awake()
        {
            _instance = this;
            _sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 100f);
            for (var i = 0; i < poolSize; i++)
            {
                var go = new GameObject("Spark");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _sprite;
                sr.sortingOrder = 30;
                var mote = new SparkMote { t = go.transform, sr = sr };
                _pool.Enqueue(mote);
                go.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        public static void Spawn(Vector3 position, Color color, int count = 10, float strength = 1f)
        {
            if (_instance == null) return;
            _instance.Burst(position, color, count, strength);
        }

        private void Burst(Vector3 position, Color color, int count, float strength)
        {
            for (var i = 0; i < count; i++)
            {
                if (_pool.Count == 0) return;
                var mote = _pool.Dequeue();
                var angle = Random.Range(130f, 410f) * Mathf.Deg2Rad;
                var speed = Random.Range(2.5f, 6.5f) * strength;
                mote.velocity = new Vector3(Mathf.Cos(angle) * 0.9f, Mathf.Sin(angle) * 1.2f + 1.5f, 0f) * speed;
                mote.maxLife = mote.life = Random.Range(0.22f, 0.45f);
                mote.start = color;
                mote.t.position = position + new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(-0.1f, 0.35f), -2f);
                mote.t.localScale = Vector3.one * moteScale * Random.Range(0.7f, 1.4f);
                mote.sr.color = color;
                mote.sr.gameObject.SetActive(true);
                _active.Add(mote);
            }
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            for (var i = _active.Count - 1; i >= 0; i--)
            {
                var mote = _active[i];
                mote.life -= dt;
                if (mote.life <= 0f)
                {
                    mote.sr.gameObject.SetActive(false);
                    _pool.Enqueue(mote);
                    _active.RemoveAt(i);
                    continue;
                }
                mote.velocity += Vector3.down * 7f * dt;
                mote.t.position += mote.velocity * dt;
                var t = mote.life / mote.maxLife;
                var c = mote.start;
                c.a = t;
                mote.sr.color = c;
            }
        }
    }
}
