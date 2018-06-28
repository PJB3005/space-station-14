using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Client.Scenes
{
    public class Node2D : GodotNode
    {
        new internal Godot.Node2D SceneNode { get; private set; }

        public Node2D(string name) : base(name)
        {

        }

        protected virtual void InstanceSceneControl(Godot.Node node = null)
        {
            base.InstanceSceneControl(SceneNode = (Godot.Node2D)node ?? new Godot.Node2D());
        }
    }
}
