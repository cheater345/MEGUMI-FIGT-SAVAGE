using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SteelTempest.Combat;
using SteelTempest.Core.Bootstrap;
using SteelTempest.Enemies;
using SteelTempest.Modes;
using SteelTempest.Player;
using SteelTempest.Weapons;

namespace SteelTempest.EditorTools
{
    /// <summary>
    /// Builds the playable "Boot" scene when the project ships with no assets:
    /// camera, player + weapon + hitbox, a flat arena with two platforms,
    /// enemy prefabs (light/heavy/assassin/boss), a spawner with a
    /// ModeSession and a minimal HUD. Runs only in the editor builder path.
    /// </summary>
    public static class BootSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Boot.unity";
        private const string GeneratedDir = "Assets/Generated";
        private const string SpritesDir = GeneratedDir + "/Sprites";
        private const string WeaponsDir = GeneratedDir + "/Weapons";
        private const string EnemiesDir = GeneratedDir + "/Enemies";

        public static string Build()
        {
            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory(SpritesDir);
            Directory.CreateDirectory(WeaponsDir);
            Directory.CreateDirectory(EnemiesDir);

            EnsureTag("Enemy");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var boot = new GameObject("GameBootstrap");
            boot.AddComponent<GameBootstrap>();

var player = BuildPlayer();
            BuildCamera(player.transform.position);
            BuildWeapon(player);
            BuildWorld();
            BuildHud();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[BootSceneBuilder] full playable scene created at " + ScenePath);
            return ScenePath;
        }

        // ---------- small helpers ----------

        private static void EnsureTag(string tag)
        {
            var manager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (manager == null || manager.Length == 0) return;
            var so = new SerializedObject(manager[0]);
            var tags = so.FindProperty("tags");
            for (var i = 0; i < tags.arraySize; i++)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
            }
            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            so.ApplyModifiedProperties();
        }

        private static Sprite MakeColoredSprite(string name, Color color, float px = 32f)
        {
            var texture = new Texture2D((int)px, (int)px, TextureFormat.RGBA32, false);
            var pixels = new Color[(int)(px * px)];
            for (var i = 0; i < pixels.Length; i++) pixels[i] = color;
            texture.SetPixels(pixels);
            texture.Apply();
            var bytes = texture.EncodeToPNG();
            var path = Path.Combine(SpritesDir, name + ".png");
            File.WriteAllBytes(path, bytes);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var sprite = Sprite.Create(asset, new Rect(0f, 0f, asset.width, asset.height), new Vector2(0.5f, 0.5f), 100f);
            AssetDatabase.AddObjectToAsset(sprite, asset);
            AssetDatabase.SaveAssets();
            return sprite;
        }

        /// <summary>Shadow-Fight-style silhouette fighter painted procedurally on a 64px canvas.</summary>
        private static Sprite MakeFighterSprite(
            string name,
            Color body,
            Color accent,
            float ppu = 64f,
            bool slender = false,
            bool heavy = false,
            bool hooded = false,
            bool horned = false,
            int weapon = 0)
        {
            const int S = 64;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            var empty = new Color(0f, 0f, 0f, 0f);
            for (var y = 0; y < S; y++)
                for (var x = 0; x < S; x++)
                    tex.SetPixel(x, y, empty);

            var dark = body * 0.55f;
            var edge = body * 0.8f;
            var steel = new Color(0.72f, 0.75f, 0.8f);
            var wood = new Color(0.4f, 0.28f, 0.2f);

            void R(int x, int y, int w, int h, Color c)
            {
                for (var j = y; j < y + h && j >= 0 && y < S; j++)
                    for (var i = x; i < x + w && i >= 0 && x < S; i++)
                    {
                        if (i < 0 || j < 0 || i >= S || j >= S) continue;
                        tex.SetPixel(i, j, c);
                    }
            }

            void D(int cx, int cy, int r, Color c)
            {
                for (var j = cy - r; j <= cy + r; j++)
                    for (var i = cx - r; i <= cx + r; i++)
                    {
                        if (i < 0 || j < 0 || i >= S || j >= S) continue;
                        var dx = i - cx;
                        var dy = j - cy;
                        if (dx * dx + dy * dy <= r * r) tex.SetPixel(i, j, c);
                    }
            }

            void Ln(int x0, int y0, int x1, int y1, int th, Color c)
            {
                var r = (th + 1) / 2;
                const int steps = 64;
                for (var s = 0; s <= steps; s++)
                {
                    var t = s / (float)steps;
                    D((int)Mathf.Lerp(x0, x1, t), (int)Mathf.Lerp(y0, y1, t), r, c);
                }
            }

            // ---- silhouette (facing right) ----
            if (heavy)
            {
                R(10, 1, 18, 6, body);
                R(30, 1, 20, 6, body);       // feet
                Ln(14, 7, 14, 20, 6, body);  // back shin+thigh
                Ln(32, 7, 33, 20, 6, body);  // front leg
                R(12, 18, 34, 14, body);     // torso (bulky)
                R(16, 32, 27, 6, body);      // shoulders
            }
            else
            {
                R(8, 1, 10, 4, dark);
                R(24, 1, 10, 4, body);       // feet
                Ln(9, 5, 10, 20, 5, body);   // back shin+thigh
                Ln(23, 5, 24, 20, 5, body);  // front shin+thigh
                R(12, 18, 16, 13, body);     // torso
                R(14, 24, 20, 9, body);      // torso upper
                R(13, 29, 24, 4, body);      // shoulders
            }

            if (slender)
            {
                R(19, 18, 8, 14, empty);     // carve slim waist away
            }

            // cape / cloth trailing behind
            Ln(12, 26, 4, 42, 6, dark);
            Ln(8, 34, 3, 46, 3, dark);

            // back arm
            Ln(19, 30, 14, 37, 4, body);
            D(13, 38, 2, body);

            // front arm + hand
            Ln(27, 31, 34, 36, 4, body);
            Ln(34, 36, 42, 32, 3, body);
            D(42, 32, 3, body);

            // ---- head ----
            R(26, 39, 7, 5, body);       // neck
            D(32, 50, 7, body);          // skull
            if (hooded)
            {
                D(32, 47, 10, body);     // hood dome
                R(25, 45, 14, 4, dark);  // face shadow
                D(31, 47, 3, empty);     // eye gap
                R(36, 48, 4, 3, accent); // glowing eye in the dark
            }
            else
            {
                Ln(30, 57, 17, 64, 3, body);  // hair flowing back
                Ln(26, 60, 20, 66, 2, body);
                R(26, 54, 10, 2, body);       // hair cap
            }

            if (horned)
            {
                Ln(22, 54, 16, 70, 4, body);
                Ln(27, 54, 36, 70, 4, body);
                R(21, 56, 16, 4, accent);
            }

            // ---- weapons ----
            if (weapon == 0)
            {
                // katana held forward
                Ln(42, 37, 56, 40, 5, steel);      // blade back edge
                Ln(42, 35, 57, 38, 2, new Color(0.95f, 0.97f, 1f)); // cutting edge
                D(58, 38, 2, new Color(1f, 1f, 1f, 0.9f));
                R(41, 33, 4, 3, wood);               // grip
            }
            else if (weapon == 1)
            {
                Ln(42, 33, 54, 27, 2, steel);         // dagger
                D(55, 26, 2, new Color(1f, 1f, 1f, 0.9f));
            }
            else
            {
                // war axe
                Ln(46, 12, 44, 42, 3, wood);          // haft
                D(44, 36, 11, steel);                 // axe head
                R(41, 33, 8, 6, empty);               // notch
                Ln(36, 38, 52, 42, 3, new Color(0.9f, 0.93f, 1f));
            }

            // accents: belt, chest trim, glowing eyes
            R(17, 22, 14, 2, accent * 0.7f);
            Ln(20, 28, 30, 34, 2, accent);
            D(38, 50, 2, accent);
            D(34, 50, 4, accent * 0.5f);

            tex.Apply();
            var bytes = tex.EncodeToPNG();
            var path = Path.Combine(SpritesDir, name + ".png");
            File.WriteAllBytes(path, bytes);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var sprite = Sprite.Create(asset, new Rect(0f, 0f, asset.width, asset.height), new Vector2(0.5f, 0.15f), ppu);
            AssetDatabase.AddObjectToAsset(sprite, asset);
            AssetDatabase.SaveAssets();
            return sprite;
        }

        private static void SetField(Component target, string field, object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogWarning("[BootSceneBuilder] field not found: " + field + " on " + target.GetType().Name);
                return;
            }
            switch (value)
            {
                case float f: prop.floatValue = f; break;
                case int n: prop.intValue = n; break;
                case bool b: prop.boolValue = b; break;
                case string s: prop.stringValue = s; break;
                case Object o: prop.objectReferenceValue = o; break;
                case Transform[] ts: prop.arraySize = ts.Length; for (var i = 0; i < ts.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = ts[i]; break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- entities ----------

        private static GameObject BuildPlayer()
        {
            var go = new GameObject("Player");
            go.tag = "Player";
            go.transform.position = new Vector3(0f, 0.5f, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = MakeFighterSprite("Player", new Color(0.08f, 0.1f, 0.14f), new Color(0.95f, 0.16f, 0.22f), 62f);
            sr.sortingOrder = 10;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 2.2f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = go.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(0.8f, 1.5f);
            col.offset = new Vector2(0f, 0.5f);

            go.AddComponent<PlayerMarker>();

            var health = go.AddComponent<HealthComponent>();
            SetField(health, "maxHealth", 100f);
            SetField(health, "hitStunSeconds", 0.2f);
            SetField(health, "isPlayer", true);

            var controller = go.AddComponent<PlayerController>();

            var groundCheck = new GameObject("GroundCheck").transform;
            groundCheck.SetParent(go.transform);
            groundCheck.localPosition = new Vector3(0f, -0.45f, 0f);
            SetField(controller, "groundCheck", groundCheck);

            go.AddComponent<PlayerCombat>();

            go.AddComponent<TouchInput>();
            go.AddComponent<DesktopInput>();

            return go;
        }

        private static void BuildCamera(Vector3 at)
        {
            var cam = new GameObject("Main Camera");
            cam.tag = "MainCamera";
            cam.transform.position = new Vector3(at.x, at.y + 1f, -10f);
            var camera = cam.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8f;
            camera.backgroundColor = new Color(0.08f, 0.09f, 0.12f);
            cam.AddComponent<AudioListener>();
            cam.AddComponent<PlayerCameraFollow>();
        }

        private static void BuildWeapon(GameObject player)
        {
            var weapon = ScriptableObject.CreateInstance<WeaponData>();
            weapon.weaponName = "Starter Sword";
            weapon.weaponId = "sword";
            weapon.weaponClass = WeaponClass.Sword;
            weapon.baseDamage = 12f;
            weapon.baseKnockback = 4f;
            weapon.reachMultiplier = 1.1f;
            weapon.damageTag = "Enemy";

            var light = ScriptableObject.CreateInstance<AttackData>();
            light.startupSeconds = 0.08f;
            light.activeSeconds = 0.14f;
            light.recoverySeconds = 0.18f;
            light.damage = 10f;
            light.knockbackForce = 3f;
            light.reach = 1.3f;
            light.height = 1.0f;

            var heavy = ScriptableObject.CreateInstance<AttackData>();
            heavy.startupSeconds = 0.25f;
            heavy.activeSeconds = 0.18f;
            heavy.recoverySeconds = 0.35f;
            heavy.damage = 24f;
            heavy.knockbackForce = 8f;
            heavy.reach = 1.6f;
            heavy.height = 1.1f;
            heavy.isFinisher = true;

            var charged = ScriptableObject.CreateInstance<AttackData>();
            charged.startupSeconds = 0.5f;
            charged.activeSeconds = 0.22f;
            charged.recoverySeconds = 0.4f;
            charged.damage = 40f;
            charged.knockbackForce = 14f;
            charged.reach = 1.9f;
            charged.launches = true;
            charged.isFinisher = true;

            var ground = ScriptableObject.CreateInstance<ComboTree>();
            ground.attacks = new System.Collections.Generic.List<AttackData> { light, heavy };
            var chargeTree = ScriptableObject.CreateInstance<ComboTree>();
            chargeTree.attacks = new System.Collections.Generic.List<AttackData> { charged };

            weapon.groundCombos = ground;
            weapon.chargedCombos = chargeTree;
            weapon.airCombos = ground;

            // Save as ONE sub-asset root so references survive the editor reload.
            var weaponPath = WeaponsDir + "/StarterSword.asset";
            AssetDatabase.CreateAsset(weapon, weaponPath);
            AssetDatabase.AddObjectToAsset(light, weapon);
            AssetDatabase.AddObjectToAsset(heavy, weapon);
            AssetDatabase.AddObjectToAsset(charged, weapon);
            AssetDatabase.AddObjectToAsset(ground, weapon);
            AssetDatabase.AddObjectToAsset(chargeTree, weapon);
            AssetDatabase.SaveAssets();

            var combat = player.GetComponent<PlayerCombat>();
            combat.SetWeapon(weapon);

            // Hitbox GameObject saved as a prefab for ObjectPool.
            var hitbox = new GameObject("Hitbox", typeof(BoxCollider2D), typeof(Hitbox));
            var hitCol = hitbox.GetComponent<BoxCollider2D>();
            hitCol.size = new Vector2(1.4f, 1.1f);
            hitCol.offset = new Vector2(0.7f, 0.4f);
            hitCol.isTrigger = true;

            var hitboxPrefabPath = GeneratedDir + "/Hitbox.prefab";
            PrefabUtility.SaveAsPrefabAsset(hitbox, hitboxPrefabPath);
            Object.DestroyImmediate(hitbox);

            var so = new SerializedObject(combat);
            so.FindProperty("hitboxPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Hitbox>(hitboxPrefabPath);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildHud()
        {
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var textGo = new GameObject("Hint");
            textGo.transform.SetParent(canvasGo.transform, false);
            var text = textGo.AddComponent<UnityEngine.UI.Text>();
            text.text = "LEFT: move/jump   RIGHT: tap attack, hold heavy";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 22;
            text.color = Color.white;
            var rt = text.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(0f, 44f);
            rt.anchoredPosition = new Vector2(0f, 24f);
        }

        // ----- enemies + world -----

        private static void BuildWorld()
        {
            // Arena floor.
            var floor = new GameObject("Floor");
            var floorSr = floor.AddComponent<SpriteRenderer>();
            floorSr.sprite = MakeColoredSprite("Floor", new Color(0.25f, 0.28f, 0.33f), 16f);
            floorSr.drawMode = SpriteDrawMode.Tiled;
            floorSr.size = new Vector2(120f, 4f);
            floor.transform.position = new Vector3(0f, -3.5f, 0f);
            var floorCol = floor.AddComponent<BoxCollider2D>();
            floorCol.size = new Vector2(120f, 3f);
            floorCol.offset = new Vector2(0f, -1.2f);

            // Arena walls.
            var leftWall = new GameObject("LeftWall");
            leftWall.transform.position = new Vector3(-58f, 0f, 0f);
            leftWall.AddComponent<BoxCollider2D>().size = new Vector2(4f, 30f);

            var rightWall = new GameObject("RightWall");
            rightWall.transform.position = new Vector3(58f, 0f, 0f);
            rightWall.AddComponent<BoxCollider2D>().size = new Vector2(4f, 30f);

            // Enemy prefab kit.
            var lightSpr = MakeFighterSprite("Light", new Color(0.06f, 0.12f, 0.13f), new Color(1f, 0.8f, 0.25f), 52f, slender: true, weapon: 1);
            var heavySpr = MakeFighterSprite("Heavy", new Color(0.17f, 0.13f, 0.14f), new Color(0.95f, 0.4f, 0.12f), 42f, heavy: true, weapon: 2);
            var assassinSpr = MakeFighterSprite("Assassin", new Color(0.1f, 0.13f, 0.17f), new Color(0.4f, 0.9f, 0.5f), 56f, slender: true, hooded: true, weapon: 1);
            var bossSpr = MakeFighterSprite("Boss", new Color(0.22f, 0.11f, 0.13f), new Color(1f, 0.25f, 0.22f), 21f, heavy: true, horned: true, weapon: 2);
            var light = BuildEnemyPrefab("Light", EnemyArchetype.Light,
                lightSpr, 35f, 3.2f, 6f, 12f);
            var heavy = BuildEnemyPrefab("Heavy", EnemyArchetype.Heavy,
                heavySpr, 90f, 1.4f, 22f, 4f);
            var assassin = BuildEnemyPrefab("Assassin", EnemyArchetype.Assassin,
                assassinSpr, 40f, 4.4f, 10f, 3f);
            var boss = BuildBossPrefab("Boss", bossSpr);

            // Spawner.
            var spawnerGo = new GameObject("EnemySpawner");
            var spawner = spawnerGo.AddComponent<EnemySpawner>();
            SetField(spawner, "lightEnemyPrefab", light);
            SetField(spawner, "heavyEnemyPrefab", heavy);
            SetField(spawner, "assassinEnemyPrefab", assassin);
            SetField(spawner, "bossPrefab", boss);
            SetField(spawner, "maxAlive", 10);

            var p1 = new GameObject("SpawnL").transform;
            p1.SetParent(spawnerGo.transform);
            p1.localPosition = new Vector3(-30f, 0f, 0f);
            var p2 = new GameObject("SpawnR").transform;
            p2.SetParent(spawnerGo.transform);
            p2.localPosition = new Vector3(30f, 0f, 0f);
            SetField(spawner, "spawnPoints", new Transform[] { p1, p2 });

            // Mode binder configures the session (Endless) for the spawner.
            var binderGo = new GameObject("ModeBinder");
            var binder = binderGo.AddComponent<ModeBinder>();
            var mode = ScriptableObject.CreateInstance<ModeDefinition>();
            mode.mode = GameMode.Endless;
            mode.modeName = "Arena";
            mode.enemySpawnInterval = 2.2f;
            mode.enemyCap = 8;
            mode.wavesBeforeBoss = 4;
            mode.endlessScaling = true;
            AssetDatabase.CreateAsset(mode, GeneratedDir + "/ArenaMode.asset");
            SetField(binder, "definition", mode);
            SetField(binder, "spawner", spawner);
            AssetDatabase.SaveAssets();
        }

        private static EnemyController BuildEnemyPrefab(string name, EnemyArchetype archetype, Sprite sprite, float health, float speed, float dmg, float range)
        {
            var go = new GameObject(name);
            go.tag = "Enemy";
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 5;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 2.2f;
            rb.freezeRotation = true;
            var col = go.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(1f, 1.2f);
            col.offset = new Vector2(0f, 0.5f);
            var healthComp = go.AddComponent<HealthComponent>();
            SetField(healthComp, "maxHealth", health);
            var ctrl = go.AddComponent<EnemyController>();
            ctrl.archetype = archetype;
            SetField(ctrl, "moveSpeed", speed);
            SetField(ctrl, "attackDamage", dmg);
            SetField(ctrl, "attackRange", range);
            SetField(ctrl, "chaseRange", 50f);
            SetField(ctrl, "leashRange", 60f);
            var prefabPath = EnemiesDir + "/" + name + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<EnemyController>(prefabPath);
        }

        private static BossController BuildBossPrefab(string name, Sprite sprite)
        {
            var go = new GameObject(name);
            go.tag = "Enemy";
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 6;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 2.2f;
            rb.freezeRotation = true;
            var col = go.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(3f, 2.5f);
            col.offset = new Vector2(0f, 1f);
            var healthComp = go.AddComponent<HealthComponent>();
            SetField(healthComp, "maxHealth", 320f);
            var boss = go.AddComponent<BossController>();
            var prefabPath = EnemiesDir + "/" + name + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<BossController>(prefabPath);
        }

        // Forward declaration used by the scene flow: arena + spawners.
    }
}