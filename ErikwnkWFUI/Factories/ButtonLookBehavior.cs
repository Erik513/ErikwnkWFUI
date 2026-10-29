using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    // What happens to the colors of a button while it lives: the text follows hover and press, a disabled button is dimmed
    // and gets its colors back, and a change of the theme or the accent gives the colors the factory chose their new value.
    // Where a caller changed a color by hand, that color stays.
    internal static class ButtonLookBehavior
    {
        // Disabled means a darker fill. A light fill (Standard on a light theme) only dims a little: darkening it as far as
        // a dark fill would make a heavy gray block that stands out more than the enabled button.
        private const double DisabledDarkenAmount = 0.35;
        private const double LightDisabledDarkenAmount = 0.1;

        public static void Attach(Button button, Func<ButtonLook> getLook)
        {
            ButtonLook[] current = { getLook() };

            // Pressed before enabled: both listen to EnabledChanged, and the
            // pressed text color has to be undone before the enabled style
            // remembers the colors.
            Action<ButtonLook, ButtonLook> followStateColors = SetStateForeColors(button, current);
            Action<ButtonLook, ButtonLook> followEnabledStyle = SetEnabledStyle(button, current);

            // The theme or the accent changed while the button is shown: what still has the color the factory gave
            // it takes the new one; a color set by hand stays.
            ThemeWatcher.Attach(button, () =>
            {
                ButtonLook old = current[0];
                ButtonLook next = getLook();
                current[0] = next;

                FlatButtonAppearance flat = button.FlatAppearance;

                if (flat.BorderColor == old.BorderColor)
                    flat.BorderColor = next.BorderColor;

                if (flat.MouseOverBackColor == old.MouseOverBackColor)
                    flat.MouseOverBackColor = next.MouseOverBackColor;

                if (flat.MouseDownBackColor == old.MouseDownBackColor)
                    flat.MouseDownBackColor = next.MouseDownBackColor;

                followStateColors(old, next);
                followEnabledStyle(old, next);
                button.Invalidate();
            });
        }

        // FlatAppearance can only change the fill per mouse state, not the text
        // color. So the text color follows the state by hand: pressed gets
        // whichever color reads best on the pressed fill (dark or light,
        // depending on the accent), and a look from a UIColorSet also has its
        // own text color for hovered.
        //
        // What the text is "at rest" is read when the button leaves rest, not
        // at creation - callers may recolor a button after creating it - and
        // that is what comes back. The pressed fill is read at press time,
        // because a theme change changes it later.
        private static Action<ButtonLook, ButtonLook> SetStateForeColors(Button button, ButtonLook[] current)
        {
            bool isHovered = false;
            bool isPressed = false;
            Color atRest = current[0].ForeColor;

            void Apply()
            {
                ButtonLook look = current[0];

                if (isPressed)
                {
                    button.ForeColor = look.PressedForeColor
                        ?? UIColors.GetContrastingForeColor(button.FlatAppearance.MouseDownBackColor);
                }
                else if (isHovered)
                {
                    button.ForeColor = look.HoverForeColor.Value;
                }
                else
                {
                    button.ForeColor = atRest;
                }
            }

            void LeaveRest()
            {
                if (!isHovered && !isPressed)
                    atRest = button.ForeColor;
            }

            void Press()
            {
                if (!button.Enabled || isPressed)
                    return;

                LeaveRest();
                isPressed = true;
                Apply();
            }

            void Release()
            {
                if (!isPressed)
                    return;

                isPressed = false;
                Apply();
            }

            void Enter()
            {
                if (!button.Enabled || isHovered || !current[0].HoverForeColor.HasValue)
                    return;

                LeaveRest();
                isHovered = true;
                Apply();
            }

            void Leave()
            {
                bool wasChanged = isHovered || isPressed;

                isHovered = false;
                isPressed = false;

                if (wasChanged)
                    Apply();
            }

            // Only the left button and the space bar press a button; the fill
            // changes for nothing else, so the text must not either.
            button.MouseDown += (sender, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    Press();
            };

            button.KeyDown += (sender, e) =>
            {
                if (e.KeyCode == Keys.Space)
                    Press();
            };

            button.MouseUp += delegate { Release(); };
            button.KeyUp += (sender, e) =>
            {
                if (e.KeyCode == Keys.Space)
                    Release();
            };
            button.MouseEnter += delegate { Enter(); };
            button.MouseLeave += delegate { Leave(); };
            button.LostFocus += delegate { Release(); };

            // A Click handler that disables the button runs before MouseUp,
            // while the pressed color is still in place. SetEnabledStyle must
            // see the color at rest when it remembers one, so this has to be
            // hooked first (see CreateButton).
            button.EnabledChanged += delegate
            {
                if (!button.Enabled)
                    Leave();
            };

            // The look changed: the text at rest is the new one if it was the old one; while hovered or pressed the text
            // of that state is worked out again.
            return (old, next) =>
            {
                if (atRest == old.ForeColor)
                    atRest = next.ForeColor;

                if (isHovered || isPressed)
                    Apply();
                else if (button.Enabled && button.ForeColor == old.ForeColor)
                    button.ForeColor = next.ForeColor;
            };
        }

        // Disabled means a darker fill and muted text. The colors in use right
        // before disabling are remembered and put back on enabling, so a
        // caller's own BackColor/ForeColor survives the round trip.
        private static Action<ButtonLook, ButtonLook> SetEnabledStyle(Button button, ButtonLook[] current)
        {
            Color disabledBackColor;
            Color disabledForeColor;

            void WorkOutDisabledColors(ButtonLook look)
            {
                bool lightFill = UIColors.IsLight(look.BackColor);
                disabledBackColor = look.DisabledBackColor
                    ?? UIColors.DarkenByShare(look.BackColor, lightFill ? LightDisabledDarkenAmount : DisabledDarkenAmount);

                // The text is the contrasting color blended 35% toward the disabled fill:
                // still readable, but visibly muted. One fixed gray did not work - on a
                // bright fill it came out at a contrast of about 1.0.
                disabledForeColor = look.DisabledForeColor
                    ?? UIColors.BlendToward(UIColors.GetContrastingForeColor(disabledBackColor), disabledBackColor, 0.35);
            }

            WorkOutDisabledColors(current[0]);

            Color restoreBackColor = current[0].BackColor;
            Color restoreForeColor = current[0].ForeColor;

            button.EnabledChanged += delegate
            {
                if (button.Enabled)
                {
                    button.BackColor = restoreBackColor;
                    button.ForeColor = restoreForeColor;
                }
                else
                {
                    restoreBackColor = button.BackColor;
                    restoreForeColor = button.ForeColor;
                    button.BackColor = disabledBackColor;
                    button.ForeColor = disabledForeColor;
                }
            };

            // The look changed: the fill (and what a disabled button gets back when it is enabled again) is the new one
            // if it was the old one; a disabled button gets the new disabled colors.
            return (old, next) =>
            {
                WorkOutDisabledColors(next);

                if (restoreBackColor == old.BackColor)
                    restoreBackColor = next.BackColor;

                if (restoreForeColor == old.ForeColor)
                    restoreForeColor = next.ForeColor;

                if (!button.Enabled)
                {
                    button.BackColor = disabledBackColor;
                    button.ForeColor = disabledForeColor;
                }
                else if (button.BackColor == old.BackColor)
                {
                    button.BackColor = next.BackColor;
                }
            };
        }
    }
}
