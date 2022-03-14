namespace Robust.Shared.Utility;

/// <summary>
/// Wrapper type around pointers so I can use them as generic args without erasing it to nint.
/// </summary>
internal unsafe struct Ptr<T> where T : unmanaged
{
    public T* P;

    public static implicit operator T*(Ptr<T> p) => p.P;
    public static implicit operator Ptr<T>(T* p) => new() { P = p };
}
