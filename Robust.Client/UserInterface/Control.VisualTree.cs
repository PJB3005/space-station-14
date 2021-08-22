using Robust.Client.UserInterface.Controls;
using Robust.Shared.ViewVariables;

namespace Robust.Client.UserInterface
{
    public partial class Control
    {
        /// <summary>
        ///     Whether or not this control is an (possibly indirect) child of
        ///     <see cref="IUserInterfaceManager.RootControl"/>
        /// </summary>
        [ViewVariables]
        public bool IsInsideTree => Root != null;

        [ViewVariables]
        public virtual UIRoot? Root { get; internal set; }

        private void _propagateExitTree()
        {
            Root = null;
            _exitedTree();

            foreach (var child in _orderedChildren)
            {
                child._propagateExitTree();
            }
        }

        /// <summary>
        ///     Called when the control is removed from the root control tree.
        /// </summary>
        /// <seealso cref="EnteredTree"/>
        protected virtual void ExitedTree()
        {
        }

        private void _exitedTree()
        {
            ExitedTree();
            UserInterfaceManagerInternal.ControlRemovedFromTree(this);
        }

        private void _propagateEnterTree(UIRoot root)
        {
            Root = root;
            _enteredTree();

            foreach (var child in _orderedChildren)
            {
                child._propagateEnterTree(root);
            }
        }

        /// <summary>
        ///     Called when the control enters the root control tree.
        /// </summary>
        /// <seealso cref="ExitedTree"/>
        protected virtual void EnteredTree()
        {
        }

        private void _enteredTree()
        {
            EnteredTree();
        }

    }
}
