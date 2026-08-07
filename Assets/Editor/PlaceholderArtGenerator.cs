using System.IO;
using UnityEditor;
using UnityEngine;

namespace SteelTempest.EditorTools
{
    /// <summary>
    /// Generates single-color silhouette sprites used as placeholder art until
    /// the final art pipeline lands. Menu: Tools > Steel Tempest > Generate Placeholder Sprites.
    /// </summary>
    public static class PlaceholderArtGenerator
    {
        private const string OutputDir = "Assets/Art/Generated";

        [MenuItem("Tools/Steel Tempest/Generate Placeholder Sprites")]
        public static void GenerateAll()
        {
            Directory.CreateDirectory(OutputDir);
            CreateSword("sword", new Color(0.75f, 0.75f, 0.75f));
            CreateCircle("orb", new Color(0f, 1f, 1f));
            CreateRect("ground", Color.white, 256, 32);
            CreateRect("boss_platform", new Color(0.3f, 0.3f, 0.3f), 512, 64);
            AssetDatabase.Refresh();
            Debug.Log("[SteelTempest] Placeholder sprites generated.");
        }

        private static void CreateSword(string name, Color color)
        {
            var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (var y = 0; y < 64; y++)
            {
                for (var x = 0; x < 64; x++)
                {
                    var isBlade = x >= 30 && x <= 34 && y >= 40 && y < 64;
                    var isGuard = x >= 22 && x <= 42 && y >= 34 && y < 40;
                    var isGrip = x >= 31 && x <= 33 && y >= 20 && y < 34;
                    var isPommel = x >= 30 && x <= 34 && y >= 14 && y < 20;
                    tex.SetPixel(x, y, (isBlade || isGuard || isGrip || isPommel) ? color : Color.clear);
                }
            }
            Save(tex, OutPath(name));
        }

        private static void CreateCircle(string name, Color color)
        {
            var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (var y = 0; y < 64; y++)
            {
                for (var x = 0; x < 64; x++)
                {
                    var d = Vector2.Distance(new Vector2(x, y), new Vector2(32f, 32f));
                    tex.SetPixel(x, y, d <= 28f ? color : Color.clear);
                }
            }
            Save(tex, OutPath(name));
        }

        private static void CreateRect(string name, Color color, int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var fill = new Color[w * h];
            for (var i = 0; i < fill.Length; i++) fill[i] = color;
            tex.SetPixels(fill);
            tex.Apply();
            Save(tex, OutPath(name));
        }

        private static string OutPath(string name) => Path.Combine(OutputDir, name) + ".png";

        private static void Save(Texture2D tex, string path)
        {
            var bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            Object.DestroyImmediate(tex);
        }
    }
}