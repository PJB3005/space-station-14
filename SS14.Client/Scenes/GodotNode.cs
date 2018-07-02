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
    }
}
