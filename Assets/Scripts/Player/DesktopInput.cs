using UnityEngine;

namespace SteelTempest.Player
{
    /// <summary>
    /// Desktop keyboard adapter feeding <see cref="Controls"/>.
    /// </summary>
    public sealed class DesktopInput : MonoBehaviour
    {
        private void Update()
        {
            Controls.MoveAxis = GetAxis();
            Controls.RunHeld = Input.GetKey(KeyCode.LeftShift);
            Controls.CrouchHeld = Input.GetKey(KeyCode.S);
            Controls.BlockHeld = Input.GetKey(KeyCode.V);
            Controls.JumpPressed = Input.GetKeyDown(KeyCode.Space);
            Controls.DashPressed = Input.GetKeyDown(KeyCode.X);
            Controls.DodgePressed = Input.GetKeyDown(KeyCode.C);
            Controls.LightPressed = Input.GetKeyDown(KeyCode.J);
            Controls.HeavyPressed = Input.GetKeyDown(KeyCode.K);
            Controls.HeavyHeld = Input.GetKey(KeyCode.K);
            Controls.HeavyReleased = Input.GetKeyUp(KeyCode.K);
            Controls.SkillPressed = Input.GetKeyDown(KeyCode.L);
        }

        private static float GetAxis()
        {
            var ax = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) ax -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) ax += 1f;
            return Mathf.Clamp(ax, -1f, 1f);
        }
    }
}