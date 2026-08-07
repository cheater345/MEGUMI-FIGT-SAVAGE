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
            sr.sprite = MakeColoredSprite("Player", new Color(0.6f, 0.85f, 1f));
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
            var light = BuildEnemyPrefab("Light", EnemyArchetype.Light,
                new Color(0.95f, 0.55f, 0.35f), 35f, 3.2f, 6f, 12f);
            var heavy = BuildEnemyPrefab("Heavy", EnemyArchetype.Heavy,
                new Color(0.75f, 0.35f, 0.6f), 90f, 1.4f, 22f, 4f);
            var assassin = BuildEnemyPrefab("Assassin", EnemyArchetype.Assassin,
                new Color(0.7f, 0.9f, 0.5f), 40f, 4.4f, 10f, 3f);
            var boss = BuildBossPrefab("Boss", new Color(1f, 0.3f, 0.35f));

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

        private static EnemyController BuildEnemyPrefab(string name, EnemyArchetype archetype, Color color, float health, float speed, float dmg, float range)
        {
            var go = new GameObject(name);
            go.tag = "Enemy";
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = MakeColoredSprite(name, color, 28f);
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

        private static BossController BuildBossPrefab(string name, Color color)
        {
            var go = new GameObject(name);
            go.tag = "Enemy";
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = MakeColoredSprite(name, color, 40f);
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