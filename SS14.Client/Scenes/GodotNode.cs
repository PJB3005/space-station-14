using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Client.Scenes
{
    public class GodotNode : Node
    {
        internal Godot.Node SceneNode { get; private set; }

        public GodotNode(string name) : base(name)
        {
            InstanceSceneNode();
        }

        public GodotNode() : base()
        {
            InstanceSceneNode();
        }

        protected virtual void InstanceSceneNode(Godot.Node node = null)
        {
            SceneNode = node ?? new Godot.Node();
        }

        public override void AddChild(Node child, bool autoRenameNode = true)
        {
            base.AddChild(child, autoRenameNode);

            if (child is GodotNode gdNode)
            {
                SceneNode.AddChild(gdNode.SceneNode);
            }
        }

        public override void RemoveChild(Node child)
        {
            base.RemoveChild(child);

            if (child is GodotNode gdNode)
            {
                SceneNode.RemoveChild(gdNode.SceneNode);
            }
        }
    }
}
