using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Client.UserInterface
{
    public partial class Control
    {
        private readonly List<Control> _orderedChildren = new();

        /// <summary>
        ///     Our parent inside the control tree.
        /// </summary>
        /// <remarks>
        ///     This cannot be changed directly. Use <see cref="AddChild" /> and such on the parent to change it.
        /// </remarks>
        [ViewVariables]
        public Control? Parent { get; private set; }

        /// <summary>
        ///     Gets an ordered enumerable over all the children of this control.
        /// </summary>
        [ViewVariables]
        public OrderedChildCollection Children { get; }

        [Content] public virtual ICollection<Control> XamlChildren { get; protected set; }


        /// <summary>
        ///     Dispose all children, but leave this one intact.
        /// </summary>
        public void DisposeAllChildren()
        {
            // Cache because the children modify the dictionary.
            var children = new List<Control>(Children);
            foreach (var child in children)
            {
                child.Dispose();
            }
        }

        /// <summary>
        ///     Remove all the children from this control.
        /// </summary>
        public void RemoveAllChildren()
        {
            DebugTools.Assert(!Disposed, "Control has been disposed.");

            foreach (var child in Children.ToArray())
            {
                RemoveChild(child);
            }
        }

        /// <summary>
        ///     Make this child an orphan. i.e. remove it from its parent if it has one.
        /// </summary>
        public void Orphan()
        {
            DebugTools.Assert(!Disposed, "Control has been disposed.");

            Parent?.RemoveChild(this);
        }

        /// <summary>
        ///     Make the provided control a parent of this control.
        /// </summary>
        /// <param name="child">The control to make a child of this control.</param>
        /// <exception cref="InvalidOperationException">
        ///     Thrown if we already have a component with the same name,
        ///     or the provided component is still parented to a different control.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="child" /> is <c>null</c>.
        /// </exception>
        public void AddChild(Control child)
        {
            DebugTools.Assert(!Disposed, "Control has been disposed.");

            if (child == null) throw new ArgumentNullException(nameof(child));
            if (child.Parent != null)
            {
                throw new InvalidOperationException("This component is still parented. Deparent it before adding it.");
            }

            DebugTools.Assert(!child.Disposed, "Child is disposed.");

            if (child == this)
            {
                throw new InvalidOperationException("You can't parent something to itself!");
            }

            // Ensure this control isn't a parent of ours.
            // Doesn't need to happen if the control has no children of course.
            if (child.ChildCount != 0)
            {
                for (var parent = Parent; parent != null; parent = parent.Parent)
                {
                    if (parent == child)
                    {
                        throw new ArgumentException("This control is one of our parents!", nameof(child));
                    }
                }
            }

            child.Parent = this;
            _orderedChildren.Add(child);

            child.Parented(this);
            if (Root != null)
            {
                child._propagateEnterTree(Root);
            }

            ChildAdded(child);
        }

        public event Action<Control>? OnChildAdded;

        /// <summary>
        ///     Called after a new child is added to this control.
        /// </summary>
        /// <param name="newChild">The new child.</param>
        protected virtual void ChildAdded(Control newChild)
        {
            OnChildAdded?.Invoke(newChild);
            InvalidateMeasure();
        }

        /// <summary>
        ///     Called when this control gets made a child of a different control.
        /// </summary>
        /// <param name="newParent">The new parent component.</param>
        protected virtual void Parented(Control newParent)
        {
            StylesheetUpdateRecursive();
            InvalidateMeasure();
        }

        /// <summary>
        ///     Removes the provided child from this control.
        /// </summary>
        /// <param name="child">The child to remove.</param>
        /// <exception cref="InvalidOperationException">
        ///     Thrown if the provided child is not one of this control's children.
        /// </exception>
        public void RemoveChild(Control child)
        {
            DebugTools.Assert(!Disposed, "Control has been disposed.");

            if (child.Parent != this)
            {
                throw new InvalidOperationException("The provided control is not a direct child of this control.");
            }

            _orderedChildren.Remove(child);

            child.Parent = null;

            child.Deparented();
            if (IsInsideTree)
            {
                child._propagateExitTree();
            }

            ChildRemoved(child);
        }

        public event Action<Control>? OnChildRemoved;

        /// <summary>
        ///     Called when a child is removed from this child.
        /// </summary>
        /// <param name="child">The former child.</param>
        protected virtual void ChildRemoved(Control child)
        {
            OnChildRemoved?.Invoke(child);
            InvalidateMeasure();
        }

        /// <summary>
        ///     Called when this control is removed as child from the former parent.
        /// </summary>
        protected virtual void Deparented()
        {
        }

        public event Action<ControlChildMovedEventArgs>? OnChildMoved;

        /// <summary>
        ///     Called when the order index of a child changes.
        /// </summary>
        /// <param name="child">The child that was changed.</param>
        /// <param name="oldIndex">The previous index of the child.</param>
        /// <param name="newIndex">The new index of the child.</param>
        protected virtual void ChildMoved(Control child, int oldIndex, int newIndex)
        {
            OnChildMoved?.Invoke(new ControlChildMovedEventArgs(child, oldIndex, newIndex));
        }

        /// <summary>
        ///     Gets the immediate child of this control with the specified index.
        /// </summary>
        /// <param name="index">The index of the child.</param>
        /// <returns>The child.</returns>
        public Control GetChild(int index)
        {
            return _orderedChildren[index];
        }

        /// <summary>
        ///     Gets the "index" in the parent.
        ///     This index is used for ordering of actions like input and drawing among siblings.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///     Thrown if this control has no parent.
        /// </exception>
        public int GetPositionInParent()
        {
            if (Parent == null)
            {
                throw new InvalidOperationException("This control has no parent!");
            }

            return Parent._orderedChildren.IndexOf(this);
        }

        /// <summary>
        ///     Sets the index of this control in the parent.
        ///     This pretty much corresponds to layout and drawing order in relation to its siblings.
        /// </summary>
        /// <param name="position"></param>
        /// <exception cref="InvalidOperationException">This control has no parent.</exception>
        public void SetPositionInParent(int position)
        {
            if (Parent == null)
            {
                throw new InvalidOperationException("No parent to change position in.");
            }

            var posInParent = GetPositionInParent();
            if (posInParent == position)
            {
                return;
            }

            Parent._orderedChildren.RemoveAt(posInParent);
            Parent._orderedChildren.Insert(position, this);
            Parent.ChildMoved(this, posInParent, position);
        }

        /// <summary>
        ///     Makes this the first control among its siblings,
        ///     So that it's first in things such as drawing order.
        /// </summary>
        /// <exception cref="InvalidOperationException">This control has no parent.</exception>
        public void SetPositionFirst()
        {
            SetPositionInParent(0);
        }

        /// <summary>
        ///     Makes this the last control among its siblings,
        ///     So that it's last in things such as drawing order.
        /// </summary>
        /// <exception cref="InvalidOperationException">This control has no parent.</exception>
        public void SetPositionLast()
        {
            if (Parent == null)
            {
                throw new InvalidOperationException("No parent to change position in.");
            }

            SetPositionInParent(Parent.ChildCount - 1);
        }

        public class OrderedChildCollection : ICollection<Control>, IReadOnlyCollection<Control>
        {
            private readonly Control Owner;

            public OrderedChildCollection(Control owner)
            {
                Owner = owner;
            }

            public Enumerator GetEnumerator()
            {
                return new(Owner);
            }

            IEnumerator<Control> IEnumerable<Control>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public void Add(Control item)
            {
                Owner.AddChild(item);
            }

            public void Clear()
            {
                Owner.RemoveAllChildren();
            }

            public bool Contains(Control item)
            {
                return item?.Parent == Owner;
            }

            public void CopyTo(Control[] array, int arrayIndex)
            {
                Owner._orderedChildren.CopyTo(array, arrayIndex);
            }

            public bool Remove(Control item)
            {
                if (item?.Parent != Owner)
                {
                    return false;
                }

                DebugTools.AssertNotNull(Owner);
                Owner.RemoveChild(item);

                return true;
            }

            int ICollection<Control>.Count => Owner.ChildCount;
            int IReadOnlyCollection<Control>.Count => Owner.ChildCount;

            public bool IsReadOnly => false;


            public struct Enumerator : IEnumerator<Control>
            {
                private List<Control>.Enumerator _enumerator;

                internal Enumerator(Control control)
                {
                    _enumerator = control._orderedChildren.GetEnumerator();
                }

                public bool MoveNext()
                {
                    return _enumerator.MoveNext();
                }

                public void Reset()
                {
                    ((IEnumerator)_enumerator).Reset();
                }

                public Control Current => _enumerator.Current;

                object IEnumerator.Current => Current;

                public void Dispose()
                {
                    _enumerator.Dispose();
                }
            }
        }
    }
}
