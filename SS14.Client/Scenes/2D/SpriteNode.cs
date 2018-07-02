using SS14.Client.Graphics;

namespace SS14.Client.Scenes
{
    public class SpriteNode : Node2D
    {
        new internal Godot.Sprite SceneNode;

        public SpriteNode(string name) : base(name) { }
        public SpriteNode() : base() { }

        private Texture _texture;
        public Texture Texture
        {
            get => _texture;
            set => SceneNode.Texture = _texture = value;
        }

        protected override void InstanceSceneNode(Godot.Node node = null)
        {
            base.InstanceSceneNode(SceneNode = (Godot.Sprite)node ?? new Godot.Sprite());
        }
    }
}
