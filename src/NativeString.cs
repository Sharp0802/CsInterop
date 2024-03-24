using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace CsInterop;

/// <summary>
/// Interop class for <c>std::string</c>
/// </summary>
[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct NativeString : IDisposable
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeStringImpl : IDisposable
    {
        [StructLayout(LayoutKind.Explicit)]
        public struct LocalBuffer
        {
            [FieldOffset(0)] public              ulong Capacity;
            [FieldOffset(0)] public unsafe fixed byte  Buffer[16];
        }

        public NativeStringImpl(string str)
        {
            unsafe
            {
                _length               = (ulong)Encoding.UTF8.GetByteCount(str);
                _localBuffer.Capacity = (_length * 2) | 1;
                _buffer               = Marshal.AllocHGlobal((IntPtr)_localBuffer.Capacity);

                fixed (byte* p = Encoding.UTF8.GetBytes(str))
                    Unsafe.CopyBlock((void*)_buffer, p, (uint)_length);
                ((byte*)_buffer)[_length] = 0;
            }
        }

        public IntPtr      _buffer;
        public ulong       _length;
        public LocalBuffer _localBuffer;

        public void Dispose()
        {
            Marshal.FreeHGlobal(_buffer);
        }
    }

    private IntPtr _impl;

    /// <summary>
    /// Create <c>std::string</c> from <see cref="string"/>
    /// </summary>
    /// <param name="str">A managed <see cref="string"/></param>
    public NativeString(string str)
    {
        unsafe
        {
            var size = Marshal.SizeOf<NativeStringImpl>();
            var impl = new NativeStringImpl(str);
            _impl = Marshal.AllocHGlobal((IntPtr)size);
            Unsafe.CopyBlock((void*)_impl, &impl, (uint)size);
        }
    }

    public static implicit operator NativeString(string str) => new(str);

    /// <summary>
    /// Create <see cref="string"/> from native <c>std::string</c>
    /// </summary>
    /// <returns>Converted <see cref="string"/></returns>
    /// <exception cref="InvalidOperationException">
    /// If <c>std::string</c> contains invalid data, <see cref="InvalidOperationException"/> will be thrown.
    /// </exception>
    public override string ToString()
    {
        unsafe
        {
            return Marshal.PtrToStringUTF8(((NativeStringImpl*)_impl)->_buffer)
                ?? throw new InvalidOperationException();
        }
    }

    /// <summary>
    /// Dispose <c>std::string</c>.
    /// Because of its implementation,
    /// All of value-copied objects from this will be disposed too.
    /// </summary>
    public void Dispose()
    {
        unsafe
        {
            var impl = Interlocked.Exchange(ref _impl, IntPtr.Zero);
            if (impl == IntPtr.Zero)
                return;
            
            ((NativeStringImpl*)impl)->Dispose();
            Marshal.FreeHGlobal(impl);
        }
    }
}
