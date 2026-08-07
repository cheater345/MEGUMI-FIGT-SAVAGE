using System;
using UnityEngine;

namespace SteelTempest.Modes
{
    /// <summary>Playable game modes.</summary>
    public enum GameMode
    {
        Story, Survival, BossRush, Endless, Challenge, Training
    }

    /// <summary>Data-backed definition of a game mode's parameters.</summary>
    [CreateAssetMenu(menuName = "SteelTempest/Modes/Mode Definition", fileName = "GameMode")]
    public sealed class ModeDefinition : ScriptableObject
    {
        public GameMode mode;
        public string modeName;
        [TextArea] public string description;
        public float enemySpawnInterval = 3f;
        public int enemyCap = 10;
        public bool hasTimer;
        public float timerLimitSeconds = 120f;
        public int wavesBeforeBoss = 6;
        public bool endlessScaling = true;
        public bool bossLevelScaling = true;
        public bool allowCheckpoints = true;
        public bool allowDeathPenalty;
    }

    /// <summary>Shared game-mode state used by spawners and the HUD.</summary>
    public sealed class ModeSession
    {
        public ModeDefinition Definition;
        public int Wave = 1;
        public int DefeatedCount;
        public float TimeSurvived;
        public bool TimedOut;
        public bool BossDefeated;

        /// <summary>Scales enemy health/damage for endless/challenge modes.</summary>
        public float DifficultyScale =>
            Mathf.Pow(1f + (Wave - 1) * 0.08f, Definition == null ? 1f : (Definition.bossLevelScaling ? 1.1f : 1f));

        public event Action<int> OnWaveChanged;

        public void AdvanceWave()
        {
            Wave++;
            OnWaveChanged?.Invoke(Wave);
        }
    }
}