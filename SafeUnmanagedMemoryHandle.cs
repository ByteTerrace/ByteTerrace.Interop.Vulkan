using System.Runtime.InteropServices;
using System.Text;
using static TerraFX.Interop.Mimalloc.Mimalloc;

namespace ByteTerrace.Interop.Vulkan;

public sealed class SafeUnmanagedMemoryHandle : SafeBuffer
{
    public unsafe static SafeUnmanagedMemoryHandle Create(
        nuint size,
        nuint alignment = 16,
        nuint offset = 0,
        bool initializeWithZeros = false
    ) {
        var safeHandle = new SafeUnmanagedMemoryHandle();

        if (nuint.MinValue != size) {
            safeHandle.SetHandle(handle: ((nint)(
                initializeWithZeros
                ? mi_zalloc_aligned_at(
                      alignment: alignment,
                      offset: offset,
                      size: size
                  )
                : mi_malloc_aligned_at(
                      alignment: alignment,
                      offset: offset,
                      size: size
                  )
            )));
        }

        return safeHandle;
    }
    public unsafe static SafeUnmanagedMemoryHandle Create(Encoding encoding, string value, bool isNullTerminated = true) {
        var byteCount = encoding.GetByteCount(s: value);
        var byteHandle = Create(size: ((nuint)(byteCount + Convert.ToInt32(value: isNullTerminated))));
        var bytePointer = ((byte*)byteHandle.DangerousGetHandle());

        fixed (char* charPointer = value) {
            encoding.GetBytes(
                byteCount: byteCount,
                bytes: bytePointer,
                charCount: value.Length,
                chars: charPointer
            );
        }

        if (isNullTerminated) {
            bytePointer[byteCount] = (byte)'\0';
        }

        return byteHandle;
    }

    public SafeUnmanagedMemoryHandle() : base(ownsHandle: true) { }

    protected unsafe override bool ReleaseHandle() {
        mi_free(p: ((void*)handle));

        return true;
    }
}
