using System.Runtime.InteropServices;

namespace CsInterop;

/// <summary>
/// Interop class for <c>std::initializer_list&lt;T&gt;</c>
/// </summary>
/// <typeparam name="T">An element type</typeparam>
[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct InitializerList<T>(Span<T> span) where T : unmanaged
{
    private IntPtr _pointer = Marshal.AllocHGlobal(Marshal.SizeOf<T>() * span.Length);
    private ulong  _length  = (ulong)span.Length;

    /// <summary>
    /// Convert <c>std::initializer_list&lt;T&gt;</c> to <see cref="Span{T}"/>
    /// </summary>
    /// <returns>Converted span</returns>
    public Span<T> AsSpan()
    {
        unsafe
        {
            return new Span<T>((void*)_pointer, (int)_length);
        }
    }
}