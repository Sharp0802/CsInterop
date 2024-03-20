using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CsInterop;

/// <summary>
/// Interop class for <c>std::shared_ptr&lt;T&gt;</c>
/// </summary>
[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct SharedPointer<T> : IDisposable where T : unmanaged, IDisposable
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    private struct SharedCount
    {
        public int Count;
        public int WeakCount;
    }
    
    /// <summary>
    /// Create shared pointer from raw pointer
    /// </summary>
    /// <param name="t">
    /// Raw pointer;
    /// If T inherits <see cref="IDisposable"/> and If <see cref="SharedPointer{T}"/> disposed in CLR space,
    /// <see cref="IDisposable.Dispose()"/> will be called.
    /// For a pointer, will not be managed by <see cref="SharedPointer{T}"/>.
    /// </param>
    public unsafe SharedPointer(T* t)
    {
        _pointer          = t;
        _count            = (SharedCount*)Marshal.AllocHGlobal((IntPtr)Marshal.SizeOf<SharedCount>());
        _count->Count     = 1;
        _count->WeakCount = 0;
    }

    private unsafe T*           _pointer;
    private unsafe SharedCount* _count;

    /// <summary>
    /// Gets reference of associated object
    /// </summary>
    public ref T Value
    {
        get
        {
            unsafe
            {
                return ref Unsafe.AsRef<T>(_pointer);
            }
        }
    }

    /// <summary>
    /// Decrease reference count.
    /// If reference count goes down to zero,
    /// Object will be disposed in CLR space.
    /// This causes the leak of related <c>std::weak_ptr&lt;T&gt;</c>
    /// </summary>
    public void Dispose()
    {
        unsafe
        {
            if (Interlocked.Decrement(ref _count->Count) > 0)
                return;
            
            _pointer->Dispose();
            Marshal.FreeHGlobal((IntPtr)_count);
        }
    }
}
