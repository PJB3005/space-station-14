﻿using SS14.Client.GodotGlue;
using System;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.BaseButton))]
    public abstract class BaseButton : Control
    {
        public BaseButton() : base()
        {
        }

        public BaseButton(string name) : base(name)
        {
        }

        internal BaseButton(Godot.BaseButton button) : base(button)
        {
        }

        new private Godot.BaseButton SceneControl;

        private protected override void SetSceneControl(Godot.Control control)
        {
            base.SetSceneControl(control);
            SceneControl = (Godot.BaseButton) control;
        }

        public ActionMode Mode
        {
            get => GameController.OnGodot ? (ActionMode) SceneControl.ActionMode : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.ActionMode = (Godot.BaseButton.ActionModeEnum) value;
                }
            }
        }

        public bool Disabled
        {
            get => GameController.OnGodot ? SceneControl.Disabled : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Disabled = value;
                }
            }
        }

        public bool Pressed
        {
            get => GameController.OnGodot ? SceneControl.Pressed : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Pressed = value;
                }
            }
        }

        public bool ToggleMode
        {
            get => GameController.OnGodot ? SceneControl.ToggleMode : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.ToggleMode = value;
                }
            }
        }

        public enum ActionMode
        {
            Press = 0,
            Release = 1,
        }

        public bool IsHovered => GameController.OnGodot ? SceneControl.IsHovered() : default;
        public DrawModeEnum DrawMode => GameController.OnGodot ? (DrawModeEnum) SceneControl.GetDrawMode() : default;

        public ButtonGroup ButtonGroup
        {
            get => GameController.OnGodot ? new ButtonGroup(SceneControl.GetButtonGroup()) : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.SetButtonGroup(value?.GodotGroup);
                }
            }
        }

        public event Action<ButtonEventArgs> OnButtonDown;
        public event Action<ButtonEventArgs> OnButtonUp;
        public event Action<ButtonEventArgs> OnPressed;
        public event Action<ButtonToggledEventArgs> OnToggled;

        public enum DrawModeEnum
        {
            Normal = 0,
            Pressed = 1,
            Hover = 2,
            Disabled = 3,
        }

        public class ButtonEventArgs : EventArgs
        {
            /// <summary>
            ///     The button this event originated from.
            /// </summary>
            public BaseButton Button { get; }

            public ButtonEventArgs(BaseButton button)
            {
                Button = button;
            }
        }

        public class ButtonToggledEventArgs : ButtonEventArgs
        {
            /// <summary>
            ///     The new pressed state of the button.
            /// </summary>
            public bool Pressed { get; }

            public ButtonToggledEventArgs(bool pressed, BaseButton button) : base(button)
            {
                Pressed = pressed;
            }
        }

        private GodotSignalSubscriber0 __buttonDownSubscriber;
        private GodotSignalSubscriber0 __buttonUpSubscriber;
        private GodotSignalSubscriber1 __toggledSubscriber;
        private GodotSignalSubscriber0 __pressedSubscriber;

        protected override void SetupSignalHooks()
        {
            base.SetupSignalHooks();

            __buttonDownSubscriber = new GodotSignalSubscriber0();
            __buttonDownSubscriber.Connect(SceneControl, "button_down");
            __buttonDownSubscriber.Signal += __buttonDownHook;

            __buttonUpSubscriber = new GodotSignalSubscriber0();
            __buttonUpSubscriber.Connect(SceneControl, "button_up");
            __buttonUpSubscriber.Signal += __buttonUpHook;

            __toggledSubscriber = new GodotSignalSubscriber1();
            __toggledSubscriber.Connect(SceneControl, "toggled");
            __toggledSubscriber.Signal += __toggledHook;

            __pressedSubscriber = new GodotSignalSubscriber0();
            __pressedSubscriber.Connect(SceneControl, "pressed");
            __pressedSubscriber.Signal += __pressedHook;
        }

        protected override void DisposeSignalHooks()
        {
            base.DisposeSignalHooks();

            if (__buttonDownSubscriber != null)
            {
                __buttonDownSubscriber.Disconnect(SceneControl, "button_down");
                __buttonDownSubscriber.Dispose();
                __buttonDownSubscriber = null;
            }

            if (__buttonUpSubscriber != null)
            {
                __buttonUpSubscriber.Disconnect(SceneControl, "button_up");
                __buttonUpSubscriber.Dispose();
                __buttonUpSubscriber = null;
            }

            if (__toggledSubscriber != null)
            {
                __toggledSubscriber.Disconnect(SceneControl, "toggled");
                __toggledSubscriber.Dispose();
                __toggledSubscriber = null;
            }

            if (__pressedSubscriber != null)
            {
                __pressedSubscriber.Disconnect(SceneControl, "pressed");
                __pressedSubscriber.Dispose();
                __pressedSubscriber = null;
            }
        }

        private void __buttonDownHook()
        {
            OnButtonDown?.Invoke(new ButtonEventArgs(this));
        }

        private void __buttonUpHook()
        {
            OnButtonUp?.Invoke(new ButtonEventArgs(this));
        }

        private void __pressedHook()
        {
            OnPressed?.Invoke(new ButtonEventArgs(this));
        }

        private void __toggledHook(object state)
        {
            OnToggled?.Invoke(new ButtonToggledEventArgs((bool) state, this));
        }
    }

    public class ButtonGroup
    {
        public Godot.ButtonGroup GodotGroup { get; }

        public ButtonGroup()
        {
            if (GameController.OnGodot)
            {
                GodotGroup = new Godot.ButtonGroup();
            }
        }

        public ButtonGroup(Godot.ButtonGroup group)
        {
            GodotGroup = group;
        }
    }
}
