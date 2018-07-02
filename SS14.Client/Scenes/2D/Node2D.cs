using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Client.Utility;
using SS14.Shared.Maths;

namespace SS14.Client.Scenes
{
    public class Node2D : GodotNode
    {
        new internal Godot.Node2D SceneNode { get; private set; }

        public Node2D(string name) : base(name) { }
        public Node2D() : base() { }

        public Vector2 Position
        {
            get => SceneNode.Position.Convert();
            set => SceneNode.Position = value.Convert();
        }

        protected override void InstanceSceneNode(Godot.Node node = null)
        {
            base.InstanceSceneNode(SceneNode = (Godot.Node2D)node ?? new Godot.Node2D());
        }
    }
}
