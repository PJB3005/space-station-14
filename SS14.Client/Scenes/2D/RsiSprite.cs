namespace SS14.Client.Scenes
{
    public class RsiSprite : Node2D
    {
        new internal Godot.Sprite SceneNode;

        public RsiSprite(string name) : base(name) { }
        public RsiSprite() : base() { }

        protected override void InstanceSceneNode(Godot.Node node = null)
        {
            base.InstanceSceneNode(SceneNode = (Godot.Sprite)node ?? new Godot.Sprite());
        }
    }
}
