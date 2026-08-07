using UnityEngine;

namespace SteelTempest.Player
{
    /// <summary>
    /// Central input state used by the player controller.
    /// Keyboard is bound on desktop; on-screen UI buttons set the
    /// same fields on mobile. Systems read, UI writes.
    /// </summary>
    public static class Controls
    {
        public static float MoveAxis { get; set; }
        public static bool RunHeld { get; set; }
        public static bool CrouchHeld { get; set; }
        public static bool BlockHeld { get; set; }

        public static bool JumpPressed { get; set; }
        public static bool DashPressed { get; set; }
        public static bool DodgePressed { get; set; }
        public static bool LightPressed { get; set; }
        public static bool HeavyPressed { get; set; }
        public static bool HeavyHeld { get; set; }
        public static bool HeavyReleased { get; set; }
        public static bool SkillPressed { get; set; }

        /// <summary>Clears all edge-triggered flags. Call once per frame after reading.</summary>
        public static void ConsumeFrame()
        {
            JumpPressed = false;
            DashPressed = false;
            DodgePressed = false;
            LightPressed = false;
            HeavyPressed = false;
            HeavyReleased = false;
            SkillPressed = false;
        }
    }
}