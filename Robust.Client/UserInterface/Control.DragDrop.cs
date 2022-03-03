namespace Robust.Client.UserInterface;

// TODO: Events need more useful fields.

public abstract class BaseDragEventArgs
{
    public DragDropOperation Operation { get; internal set; }

    public BaseDragEventArgs(DragDropOperation operation)
    {
        Operation = operation;
    }
}

public sealed class DragEnterEventArgs : BaseDragEventArgs
{
    public DragEnterEventArgs(DragDropOperation operation) : base(operation)
    {
    }
}

public sealed class DragLeaveEventArgs : BaseDragEventArgs
{
    public DragLeaveEventArgs(DragDropOperation operation) : base(operation)
    {
    }
}

public sealed class DragDropEventArgs : BaseDragEventArgs
{
    public DragDropEventArgs(DragDropOperation operation) : base(operation)
    {
    }

    public bool Handled { get; private set; }

    public void Handle()
    {
        Handled = true;
    }
}

public sealed class DragMoveEventArgs : BaseDragEventArgs
{
    public DragMoveEventArgs(DragDropOperation operation) : base(operation)
    {
    }
}

public partial class Control
{
    public virtual void DragEnter(DragEnterEventArgs eventArgs)
    {

    }

    public virtual void DragLeave(DragLeaveEventArgs eventArgs)
    {

    }

    public virtual void DragDrop(DragDropEventArgs eventArgs)
    {

    }

    public virtual void DragMove(DragMoveEventArgs eventArgs)
    {

    }
}
