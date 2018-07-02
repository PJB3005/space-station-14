using SS14.Client.Interfaces.Graphics.Overlays;
using SS14.Client.Scenes;
using SS14.Shared.IoC;

namespace SS14.Client.Graphics.Overlays
{
    /// <summary>
    ///     An overlay consisting of a 2D node-based scene.
    /// </summary>
    public abstract class SceneOverlay : BaseOverlay
    {
        public Node2D Scene { get; }

        protected SceneOverlay(string id) : base(id)
        {
            Scene = new Node2D("Scene");
        }

        public override void AssignCanvasItem(Godot.CanvasItem canvasItem)
        {
            base.AssignCanvasItem(canvasItem);
            MainCanvasItem.AddChild(Scene.SceneNode);
        }

        public override void ClearCanvasItem()
        {
            MainCanvasItem.RemoveChild(Scene.SceneNode);
            base.ClearCanvasItem();
        }
    }
}
