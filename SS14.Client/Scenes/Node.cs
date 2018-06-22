using System;
using System.Collections.Generic;

namespace SS14.Client.Scenes
{
    // Time to NIH the scene tree.
    /// <summary>
    ///     A node is the building block of Godot-like scene trees.
    /// </summary>
    public class Node : IDisposable
    {
        public string Name
        {
            get => _name;
            set
            {
                if (value == _name)
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("New name may not be null or whitespace.", nameof(value));
                }

                if (Parent != null)
                {
                    if (Parent.HasChild(value))
                    {
                        throw new ArgumentException($"Parent already has a child with name {value}.");
                    }

                    Parent._children.Remove(_name);
                }

                _name = value;

                if (Parent != null)
                {
                    Parent._children[_name] = this;
                }

                // TODO: On rename hook?
            }
        }

        public Node Parent { get; private set; }

        private readonly Dictionary<string, Node> _children = new Dictionary<string, Node>();
        private string _name;
        private static int UniqueNameCount = 0;

        public T GetChild<T>(string name) where T : Node
        {
            return (T)GetChild(name);
        }

        public Node GetChild(string name)
        {
            if (TryGetChild(name, out var node))
            {
                return node;
            }

            throw new KeyNotFoundException($"No child UI element {name}");
        }

        public bool TryGetChild<T>(string name, out T child) where T : Node
        {
            if (_children.TryGetValue(name, out var node))
            {
                child = (T)node;
                return true;
            }
            child = null;
            return false;
        }

        public bool TryGetChild(string name, out Node child)
        {
            return _children.TryGetValue(name, out child);
        }

        public bool HasChild(string name)
        {
            return _children.ContainsKey(name);
        }

        #region IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        ~Node()
        {
            Dispose(false);
        }
        #endregion
    }
}
