using SS14.Client.Graphics.Drawing;
using SS14.Client.Graphics.Shaders;
using SS14.Client.Interfaces.Graphics.Overlays;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VS = Godot.VisualServer;

namespace SS14.Client.Graphics.Overlays
{
    /// <summary>
    ///     An overlay implementation that uses the standard 2D drawing API,
    ///     and does not preserve state between redraws.
    /// </summary>
    public abstract class ImmediateOverlay : BaseOverlay, IDisposable
    {
        public virtual bool AlwaysDirty => false;
        public bool IsDirty => AlwaysDirty || _isDirty;
        public bool Drawing { get; private set; } = false;

        private Shader _shader;
        /// <summary>
        ///     The shader applied to the main canvas item.
        /// </summary>
        /// <seealso cref="SubHandlesUseMainShader" />
        public Shader Shader
        {
            get => _shader;
            set
            {
                _shader = value;
                if (MainCanvasItem != null)
                {
                    MainCanvasItem.Material = value.GodotMaterial;
                }
            }
        }

        /// <summary>
        ///     If true, new drawing handles created while drawing will use the same shader as the main shader,
        ///     unless explicitly overriden.
        /// </summary>
        /// <seealso cref="Shader" />
        public virtual bool SubHandlesUseMainShader { get; } = true;

        private bool _isDirty = true;

        private readonly List<Godot.RID> CanvasItems = new List<Godot.RID>();
        private readonly List<DrawingHandle> TempHandles = new List<DrawingHandle>();

        private bool Disposed = false;

        protected ImmediateOverlay(string id) : base(id)
        {
        }

        public override void AssignCanvasItem(Godot.CanvasItem canvasItem)
        {
            base.AssignCanvasItem(canvasItem);
            if (Shader != null)
            {
                MainCanvasItem.Material = Shader.GodotMaterial;
            }
        }

        public void Dispose()
        {
            if (Disposed)
            {
                return;
            }
            Dispose(true);
            Disposed = true;
            GC.SuppressFinalize(this);
        }

        ~ImmediateOverlay()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && MainCanvasItem != null)
            {
                OverlayManager.RemoveOverlay(ID);
            }
        }

        protected abstract void Draw(DrawingHandle handle);

        protected DrawingHandle NewHandle(Shader shader = null)
        {
            if (!Drawing)
            {
                throw new InvalidOperationException("Can only allocate new handles while drawing.");
            }

            var item = VS.CanvasItemCreate();
            VS.CanvasItemSetParent(item, MainCanvasItem.GetCanvasItem());
            CanvasItems.Add(item);
            if (shader != null)
            {
                shader.ApplyToCanvasItem(item);
            }
            else
            {
                VS.CanvasItemSetUseParentMaterial(item, SubHandlesUseMainShader);
            }

            var handle = new DrawingHandle(item);
            TempHandles.Add(handle);
            return handle;
        }

        public void Dirty()
        {
            _isDirty = true;
        }

        public override void FrameUpdate(RenderFrameEventArgs args)
        {
            if (!IsDirty)
            {
                return;
            }

            ClearDraw();

            try
            {
                Drawing = true;
                Draw(new DrawingHandle(MainCanvasItem.GetCanvasItem()));
            }
            finally
            {
                Drawing = false;
                foreach (var handle in TempHandles)
                {
                    handle.Dispose();
                }
                TempHandles.Clear();
            }
        }

        public override void ClearCanvasItem()
        {
            ClearDraw();
            base.ClearCanvasItem();
        }

        private void ClearDraw()
        {
            foreach (var item in CanvasItems)
            {
                VS.FreeRid(item);
            }

            VS.CanvasItemClear(MainCanvasItem.GetCanvasItem());

            CanvasItems.Clear();
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
