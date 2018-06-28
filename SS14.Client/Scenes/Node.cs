using System;
using System.Collections.Generic;
using System.Linq;

namespace SS14.Client.Scenes
{
    // FIXME: This currently doesn't preserve order like Godot's scene tree does.
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

                if (!IsLegalName(value))
                {
                    throw new ArgumentException("New name is illegal.", nameof(value));
                }

                if (Parent != null)
                {
                    if (Parent.HasChild(value))
                    {
                        throw new ArgumentException($"Parent already has a child with name {value}.", nameof(value));
                    }

                    Parent._children.Remove(_name);
                }

                _name = value;

                if (Parent != null)
                {
                    Parent._children[_name] = this;
                }
            }
        }

        public bool IsParented => Parent != null;
        public Node Parent { get; private set; }

        private readonly Dictionary<string, Node> _children = new Dictionary<string, Node>();
        private string _name;

        private static int UniqueNameCount = 0;

        public Node()
        {
            _name = GenUniqueName();
        }

        public Node(string name)
        {
            if (!IsLegalName(name))
            {
                throw new ArgumentException($"Illegal name: {name}", nameof(name));
            }
            _name = name;
        }

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

            throw new KeyNotFoundException($"No child with name {name}");
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

        public void AddChild(Node child, bool autoRenameNode=true)
        {
            if (child.IsParented)
            {
                throw new InvalidOperationException("Child already has a parent! Deparent it first.");
            }

            if (HasChild(child.Name))
            {
                if (!autoRenameNode)
                {
                    throw new InvalidOperationException("We already have a child with that name!");
                }

                child._name = GenUniqueName(child.Name);
            }

            _children[child.Name] = child;
            child.Parent = this;
        }

        public void RemoveChild(Node child)
        {
            if (!TryGetChild(child.Name, out var trychild) || trychild != child)
            {
                throw new InvalidOperationException("This node is not a child of us!");
            }

            _children.Remove(child.Name);
            child.Parent = null;
        }

        public IEnumerable<Node> GetChildren()
        {
            return _children.Values;
        }

        public IEnumerable<T> GetChildren<T>() where T : Node
        {
            return _children.Values.OfType<T>();
        }

        #region IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (IsParented)
                {
                    Parent.RemoveChild(this);
                }

                foreach (var child in GetChildren().ToList())
                {
                    child.Dispose();
                }
            }
        }

        ~Node()
        {
            Dispose(false);
        }
        #endregion

        private static readonly char[] IllegalCharacters = new char[]
        {
            '.',
            '/',
            ':',
            '@'
        };

        private static bool IsLegalName(string name)
        {
            return name.IndexOfAny(IllegalCharacters) == -1;
        }

        private static string GenUniqueName()
        {
            return $"@{UniqueNameCount++}";
        }

        private static string GenUniqueName(string name)
        {
            return $"@{UniqueNameCount++}@{name}";
        }
    }
}
