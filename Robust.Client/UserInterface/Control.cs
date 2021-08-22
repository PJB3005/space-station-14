using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Animations;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Client.UserInterface
{
    /// <summary>
    ///     A node in the GUI system.
    ///     See https://hackmd.io/@ss14/ui-system-tutorial for some basic concepts.
    /// </summary>
    [PublicAPI]
    public partial class Control : IDisposable
    {
        private bool _visible = true;

        // _marginSetSize is the size calculated by the margins,
        // but it's different from _size if min size is higher.

        private bool _canKeyboardFocus;

        public event Action<Control>? OnVisibilityChanged;

        /// <summary>
        ///     The name of this control.
        ///     Names must be unique between the siblings of the control.
        /// </summary>
        [ViewVariables]
        public string? Name { get; set; }

        /// <summary>
        ///     If true, this control will always be rendered, even if other UI rendering is disabled.
        /// </summary>
        /// <remarks>
        ///     Useful for e.g. primary viewports.
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)] public bool AlwaysRender { get; set; }

        public NameScope? NameScope;

        //public void AttachNameScope(Dictionary<string, Control> nameScope)
        //{
        //    _nameScope = nameScope;
        //}

        public NameScope? FindNameScope()
        {
            foreach (var control in this.GetSelfAndLogicalAncestors())
            {
                if (control.NameScope != null) return control.NameScope;
            }

            return null;
        }

        public T FindControl<T>(string name) where T : Control
        {
            var nameScope = FindNameScope();
            if (nameScope == null)
            {
                throw new ArgumentException("No Namespace found for Control");
            }

            var value = nameScope.Find(name);
            if (value == null)
            {
                throw new ArgumentException($"No Control with the name {name} found");
            }

            if (value is not T ret)
            {
                throw new ArgumentException($"Control with name {name} had invalid type {value.GetType()}");
            }

            return ret;
        }

        internal IUserInterfaceManagerInternal UserInterfaceManagerInternal { get; }

        /// <summary>
        ///     The UserInterfaceManager we belong to, for convenience.
        /// </summary>
        public IUserInterfaceManager UserInterfaceManager => UserInterfaceManagerInternal;

        [ViewVariables] public int ChildCount => _orderedChildren.Count;

        /// <summary>
        ///     Gets whether this control is at all visible.
        ///     This means the control is part of the tree of the root control, and all of its parents are visible.
        /// </summary>
        /// <seealso cref="Visible"/>
        [ViewVariables]
        public bool VisibleInTree
        {
            get
            {
                for (var parent = this; parent != null; parent = parent.Parent)
                {
                    if (!parent.Visible)
                    {
                        return false;
                    }

                    if (parent == UserInterfaceManager.RootControl)
                    {
                        return true;
                    }
                }

                return false;
            }
        }


        /// <summary>
        ///     Whether or not this control and its children are visible.
        /// </summary>
        /// <seealso cref="VisibleInTree"/>
        [ViewVariables(VVAccess.ReadWrite)]
        [Animatable]
        public bool Visible
        {
            get => _visible;
            set
            {
                if (_visible == value)
                {
                    return;
                }

                _visible = value;

                _propagateVisibilityChanged(value);
                // TODO: unhardcode this.
                // Many containers ignore children if they're invisible, so that's why we're replicating that ehre.
                Parent?.InvalidateMeasure();
                InvalidateMeasure();
            }
        }

        private void _propagateVisibilityChanged(bool newVisible)
        {
            OnVisibilityChanged?.Invoke(this);
            if (!VisibleInTree)
            {
                UserInterfaceManagerInternal.ControlHidden(this);
            }

            foreach (var child in _orderedChildren)
            {
                if (newVisible || child._visible)
                {
                    child._propagateVisibilityChanged(newVisible);
                }
            }
        }

        /// <summary>
        /// Simple text tooltip that is shown when the mouse is hovered over this control for a bit.
        /// See <see cref="TooltipSupplier"/> or <see cref="OnShowTooltip"/> for a more customizable alternative.
        /// No effect when TooltipSupplier is specified.
        /// </summary>
        /// <remarks>
        /// If empty or null, no tooltip is shown in the first place (but OnShowTooltip and OnHideTooltip
        /// events are still fired).
        /// </remarks>
        public string? ToolTip { get; set; }

        /// <summary>
        /// Overrides the global tooltip delay, showing the tooltip for this
        /// control within the specified number of seconds.
        /// </summary>
        public float? TooltipDelay { get; set; }

        /// <summary>
        /// When a tooltip should be shown for this control, this will be invoked to
        /// produce a control which will serve as the tooltip (doing nothing if null is returned).
        /// This is the generally recommended way to implement custom tooltips for controls, as it takes
        /// care of the various edge cases for showing / hiding the tooltip.
        /// For an even more customizable approach, <see cref="OnShowTooltip"/>
        ///
        /// The returned control will be added to PopupRoot, and positioned
        /// within the user interface under the current mouse position to avoid going off the edge of the
        /// screen. When the tooltip should be hidden, the control will be hidden by removing it from the tree.
        ///
        /// It is expected that the returned control remains within PopupRoot. Other classes should
        /// not move it around in the tree or move it out of PopupRoot, but may access and modify
        /// the control and its children via <see cref="SuppliedTooltip"/>.
        /// </summary>
        /// <remarks>
        /// Returning a new instance of a tooltip control every time is usually fine. If for some
        /// reason constructing the tooltip control is expensive, it MAY be fine to cache + reuse a single instance but this
        /// approach has not yet been tested.
        /// </remarks>
        public TooltipSupplier? TooltipSupplier { get; set; }

        /// <summary>
        /// Invoked when the mouse is hovered over this control for a bit and a tooltip
        /// should be shown. Can be used as an alternative to ToolTip or TooltipSupplier to perform custom tooltip
        /// logic such as showing a more complex tooltip control.
        ///
        /// Any custom tooltip controls should typically be added
        /// as a child of UserInterfaceManager.PopupRoot
        /// Handlers can use <see cref="Tooltips.PositionTooltip(Control)"/> to assist with positioning
        /// custom tooltip controls.
        /// </summary>
        public event EventHandler? OnShowTooltip;



        /// <summary>
        /// If this control is currently showing a tooltip provided via TooltipSupplier,
        /// returns that tooltip. Do not move this control within the tree, it should remain in PopupRoot.
        /// Also, as it may be hidden (removed from tree) at any time, saving a reference to this is a Bad Idea.
        /// </summary>
        public Control? SuppliedTooltip => UserInterfaceManagerInternal.GetSuppliedTooltipFor(this);

        /// <summary>
        /// Manually hide the tooltip currently being shown for this control, if there is one.
        /// </summary>
        public void HideTooltip()
        {
            UserInterfaceManagerInternal.HideTooltipFor(this);
        }

        internal void PerformShowTooltip()
        {
            OnShowTooltip?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Invoked when this control is showing a tooltip which should now be hidden.
        /// </summary>
        public event EventHandler? OnHideTooltip;

        internal void PerformHideTooltip()
        {
            OnHideTooltip?.Invoke(this, EventArgs.Empty);
        }


        /// <summary>
        ///     The mode that controls how mouse filtering works. See the enum for how it functions.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public MouseFilterMode MouseFilter { get; set; } = MouseFilterMode.Ignore;

        /// <summary>
        ///     Whether this control can take keyboard focus.
        ///     Keyboard focus is necessary for the control to receive keyboard events.
        /// </summary>
        /// <seealso cref="KeyboardFocusOnClick"/>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanKeyboardFocus
        {
            get => _canKeyboardFocus;
            set
            {
                if (_canKeyboardFocus == value)
                {
                    return;
                }

                _canKeyboardFocus = value;

                if (!value)
                {
                    ReleaseKeyboardFocus();
                }
            }
        }

        /// <summary>
        ///     Whether the control will automatically receive keyboard focus (if possible) when clicked on.
        /// </summary>
        /// <remarks>
        ///     Obviously, <see cref="CanKeyboardFocus"/> must be set to true for this to work.
        /// </remarks>
        public bool KeyboardFocusOnClick { get; set; }

        /// <summary>
        ///     Whether to clip drawing of this control and its children to its rectangle.
        /// </summary>
        /// <remarks>
        ///     By default, controls (and their children) can render outside their rectangle.
        ///     If this is set, rendering is hard clipped to it.
        /// </remarks>
        /// <seealso cref="RectDrawClipMargin"/>
        [ViewVariables]
        public bool RectClipContent { get; set; }

        /// <summary>
        ///     A margin around this control. If this control + this margin is outside its parent's <see cref="RectClipContent" />,
        ///     it will not be drawn.
        /// </summary>
        /// <remarks>
        ///     A control rectangle does not necessarily have to be listened to for drawing.
        ///     So the problem is, how do we know where to stop trying to draw the control if it's clipped away?
        /// </remarks>
        /// <seealso cref="RectClipContent"/>
        [ViewVariables(VVAccess.ReadWrite)]
        public int RectDrawClipMargin { get; set; } = 10;

        // You may wonder why Modulate isn't stylesheet controlled, but ModulateSelf is.
        // Reason is simple: I'm fucking lazy.
        // I'm expecting this comment to last much longer than the problem it's pointing out.

        /// <summary>
        ///     An override for the modulate self from the style sheet.
        /// </summary>
        /// <seealso cref="ActualModulateSelf" />
        [ViewVariables(VVAccess.ReadWrite)]
        public Color? ModulateSelfOverride { get; set; }

        /// <summary>
        ///     Modulates the color of this control and all its children when drawing.
        /// </summary>
        /// <remarks>
        ///     Modulation is multiplying or tinting the color basically.
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)]
        [Animatable]
        public Color Modulate { get; set; } = Color.White;

        /// <summary>
        ///     The value used to modulate this control (and not its siblings) with on top of <see cref="Modulate"/>
        ///     when drawing.
        /// </summary>
        /// <remarks>
        ///     By default this value is pulled from CSS, or <see cref="ModulateSelfOverride"/> if available.
        ///
        ///     Modulation is multiplying or tinting the color basically.
        /// </remarks>
        public Color ActualModulateSelf
        {
            get
            {
                if (ModulateSelfOverride.HasValue)
                {
                    return ModulateSelfOverride.Value;
                }

                if (TryGetStyleProperty(StylePropertyModulateSelf, out Color modulate))
                {
                    return modulate;
                }

                return Color.White;
            }
        }

        /// <summary>
        ///     Default constructor.
        ///     The name of the control is decided based on type.
        /// </summary>
        public Control()
        {
            UserInterfaceManagerInternal = IoCManager.Resolve<IUserInterfaceManagerInternal>();
            StyleClasses = new StyleClassCollection(this);
            Children = new OrderedChildCollection(this);
            XamlChildren = Children;
        }

        /// <summary>
        ///     Called to render this control.
        /// </summary>
        /// <remarks>
        ///     Drawing is done relative to the position of the control.
        ///     It is also done in pixel space, so you should not directly use properties such as <see cref="Size"/>.
        /// </remarks>
        /// <param name="handle">A handle that can be used to draw.</param>
        protected internal virtual void Draw(DrawingHandleScreen handle)
        {
        }

        internal virtual void DrawInternal(IRenderHandle renderHandle)
        {
            Draw(renderHandle.DrawingHandleScreen);
        }

        public void UpdateDraw()
        {
        }

        /// <summary>
        ///     Called when this modal control is closed.
        ///     Only used for controls that are actually modals.
        /// </summary>
        protected internal virtual void ModalRemoved()
        {
        }

        public bool Disposed { get; private set; }

        /// <summary>
        ///     Dispose this control, its own scene control, and all its children.
        ///     Basically the big delete button.
        /// </summary>
        public void Dispose()
        {
            if (Disposed)
            {
                return;
            }

            Dispose(true);
            Disposed = true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            UserInterfaceManagerInternal.HideTooltipFor(this);

            DisposeAllChildren();
            Parent?.RemoveChild(this);

            OnKeyBindDown = null;
        }

        /// <summary>
        ///     Called to test whether this control has a certain point,
        ///     for the purposes of finding controls under the cursor.
        /// </summary>
        /// <param name="point">The relative point, in virtual pixels.</param>
        /// <returns>True if this control does have the point and should be counted as a hit.</returns>
        protected internal virtual bool HasPoint(Vector2 point)
        {
            var size = Size;
            return point.X >= 0 && point.X <= size.X && point.Y >= 0 && point.Y <= size.Y;
        }

        /// <summary>
        ///     Called when this control receives keyboard focus.
        /// </summary>
        protected internal virtual void KeyboardFocusEntered()
        {
        }

        /// <summary>
        ///     Called when this control loses keyboard focus (corresponds to UserInterfaceManager.KeyboardFocused).
        /// </summary>
        protected internal virtual void KeyboardFocusExited()
        {
        }

        /// <summary>
        ///     Fired when a control loses control focus for any reason. See <see cref="IUserInterfaceManager.ControlFocused"/>.
        /// </summary>
        /// <remarks>
        ///     Controls which have some sort of drag / drop behavior should usually implement this method (typically by cancelling the drag drop).
        ///     Otherwise, if a user clicks down LMB over one control to initiate a drag, then clicks RMB down
        ///     over a different control while still holding down LMB, the control being dragged will now lose focus
        ///     and will no longer receive the keyup for the LMB, thus won't cancel the drag.
        ///     This should also be considered for controls which have any special KeyBindUp behavior - consider
        ///     what would happen if the control lost focus and never received the KeyBindUp.
        ///
        ///     There is no corresponding ControlFocusEntered - if a control wants to handle that situation they should simply
        ///     handle KeyBindDown as that's the only way a control would gain focus.
        /// </remarks>
        protected internal virtual void ControlFocusExited()
        {
        }

        /// <summary>
        ///     Check if this control currently has keyboard focus.
        /// </summary>
        /// <returns></returns>
        public virtual bool HasKeyboardFocus()
        {
            return UserInterfaceManager.KeyboardFocused == this;
        }

        /// <summary>
        ///     Grab keyboard focus if this control doesn't already have it.
        /// </summary>
        /// <remarks>
        ///     <see cref="CanKeyboardFocus"/> must be true for this to work.
        /// </remarks>
        public void GrabKeyboardFocus()
        {
            UserInterfaceManager.GrabKeyboardFocus(this);
        }

        /// <summary>
        ///     Release keyboard focus from this control if it has it.
        ///     If a different control has keyboard focus, nothing happens.
        /// </summary>
        public void ReleaseKeyboardFocus()
        {
            UserInterfaceManager.ReleaseKeyboardFocus(this);
        }

        public event Action? OnResized;

        /// <summary>
        ///     Called when the size of the control changes.
        /// </summary>
        protected virtual void Resized()
        {
            OnResized?.Invoke();
        }

        internal void DoFrameUpdate(FrameEventArgs args)
        {
            FrameUpdate(args);
            foreach (var child in Children)
            {
                child.DoFrameUpdate(args);
            }
        }

        /// <summary>
        ///     This is called before every render frame.
        /// </summary>
        protected virtual void FrameUpdate(FrameEventArgs args)
        {
            ProcessAnimations(args);
        }

        // These are separate from StandardCursorShape so that
        // in the future we could have an API to override the styling.

        public override string ToString()
        {
            return $"{Name} ({GetType().Name})";
        }

        /// <summary>
        ///     Mode that will be tested when testing controls to invoke mouse button events on.
        /// </summary>
        public enum MouseFilterMode : byte
        {
            /// <summary>
            ///     The control will be able to receive mouse buttons events.
            ///     Furthermore, if a control with this mode does get clicked,
            ///     the event automatically gets marked as handled after every other candidate has been tried,
            ///     so that the rest of the game does not receive it.
            /// </summary>
            Pass = 1,

            /// <summary>
            ///     The control will be able to receive mouse button events like <see cref="Pass" />,
            ///     but the event will be stopped and handled even if the relevant events do not handle it.
            /// </summary>
            Stop = 0,

            /// <summary>
            ///     The control will not be considered at all, and will not have any effects.
            /// </summary>
            Ignore = 2,
        }
    }

    public delegate Control? TooltipSupplier(Control sender);

    public readonly struct ControlChildMovedEventArgs
    {
        public ControlChildMovedEventArgs(Control control, int oldIndex, int newIndex)
        {
            Control = control;
            OldIndex = oldIndex;
            NewIndex = newIndex;
        }

        public readonly Control Control;
        public readonly int OldIndex;
        public readonly int NewIndex;
    }
}
