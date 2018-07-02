using System;
using Godot;
using SS14.Client.Interfaces.Graphics.Overlays;
using SS14.Shared.IoC;
using VS = Godot.VisualServer;

namespace SS14.Client.Graphics.Overlays
{
    public abstract class BaseOverlay : IOverlay
    {
        protected IOverlayManager OverlayManager { get; }
        public string ID { get; }

        public virtual OverlaySpace Space => OverlaySpace.ScreenSpace;

        private int? _zIndex;
        public int? ZIndex
        {
            get => _zIndex;
            set
            {
                if (value != null && (_zIndex > VS.CanvasItemZMax || _zIndex < VS.CanvasItemZMin))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _zIndex = value;
                UpdateZIndex();
            }
        }

        protected Godot.CanvasItem MainCanvasItem { get; private set; }

        protected BaseOverlay(string id)
        {
            OverlayManager = IoCManager.Resolve<IOverlayManager>();
            ID = id;
        }

        public virtual void AssignCanvasItem(CanvasItem canvasItem)
        {
            MainCanvasItem = canvasItem;
            UpdateZIndex();
        }

        public virtual void ClearCanvasItem()
        {
            MainCanvasItem = null;
        }

        public virtual void FrameUpdate(RenderFrameEventArgs args)
        {
        }

        private void UpdateZIndex()
        {
            if (MainCanvasItem == null)
            {
                return;
            }

            if (Space != OverlaySpace.WorldSpace || ZIndex == null)
            {
                VS.CanvasItemSetZIndex(MainCanvasItem.GetCanvasItem(), 0);
                VS.CanvasItemSetZAsRelativeToParent(MainCanvasItem.GetCanvasItem(), true);
            }
            else
            {
                VS.CanvasItemSetZIndex(MainCanvasItem.GetCanvasItem(), ZIndex.Value);
                VS.CanvasItemSetZAsRelativeToParent(MainCanvasItem.GetCanvasItem(), false);
            }
        }
    }
}
