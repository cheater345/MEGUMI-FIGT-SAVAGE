using UnityEngine;

namespace SteelTempest.Player
{
    /// <summary>
    /// Touch adapter feeding the shared <see cref="Controls"/> state on mobile.
    /// Left half: drag to move horizontally, tap to jump.
    /// Right half: tap to light attack, hold to charge a heavy attack
    /// (release swings it), second finger on the right half blocks.
    /// </summary>
    public sealed class TouchInput : MonoBehaviour
    {
        private const int LeftZone = 0;
        private const int RightZone = 1;

        private int _leftFinger = -1;
        private int _rightFinger = -1;
        private Vector2 _leftStart;
        private Vector2 _rightStart;
        private bool _leftTapped;
        private bool _charged;
        private bool _blockArmed;

        private void Update()
        {
            if (Input.touchCount == 0)
            {
                if (_leftFinger >= 0) ReleaseLeftTap();
                if (_rightFinger >= 0) ReleaseRightHold();
                _leftFinger = -1;
                _rightFinger = -1;
                Controls.MoveAxis = 0f;
                return;
            }

            for (var i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                var zone = t.position.x < Screen.width * 0.5f ? LeftZone : RightZone;

                if (t.phase == TouchPhase.Began)
                {
                    if (zone == LeftZone && _leftFinger < 0)
                    {
                        _leftFinger = t.fingerId;
                        _leftStart = t.position;
                        _leftTapped = true;
                    }
                    else if (zone == RightZone)
                    {
                        if (_rightFinger < 0)
                        {
                            _rightFinger = t.fingerId;
                            _rightStart = t.position;
                            _charged = false;
                            _blockArmed = false;
                        }
                        else
                        {
                            Controls.BlockHeld = true;
                            _blockArmed = true;
                        }
                    }
                }
                else if (t.phase == TouchPhase.Moved && t.fingerId == _leftFinger)
                {
                    var delta = t.position.x - _leftStart.x;
                    if (Mathf.Abs(delta) > 24f)
                    {
                        _leftTapped = false;
                        Controls.MoveAxis = Mathf.Clamp(delta / 220f, -1f, 1f);
                    }
                }
                else if (t.phase == TouchPhase.Moved && t.fingerId == _rightFinger)
                {
                    if ((t.position - _rightStart).magnitude > 24f) _charged = true;
                }
                else if (t.phase == TouchPhase.Ended && t.fingerId == _leftFinger && _leftTapped)
                {
                    Controls.JumpPressed = true;
                    Controls.MoveAxis = 0f;
                    _leftFinger = -1;
                }
                else if (t.phase == TouchPhase.Ended && t.fingerId == _rightFinger)
                {
                    if (_charged)
                    {
                        Controls.HeavyHeld = false;
                        Controls.HeavyPressed = true;
                    }
                    else
                    {
                        Controls.LightPressed = true;
                        Controls.HeavyHeld = false;
                    }
                    _rightFinger = -1;
                    if (_blockArmed) Controls.BlockHeld = false;
                }
            }
        }

        private void ReleaseLeftTap()
        {
            if (_leftTapped)
            {
                Controls.JumpPressed = true;
            }
            Controls.MoveAxis = 0f;
            _leftTapped = false;
        }

        private void ReleaseRightHold()
        {
            if (Controls.HeavyHeld) Controls.HeavyHeld = false;
        }
    }
}