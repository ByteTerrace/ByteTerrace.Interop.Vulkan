using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop.Vulkan;

namespace ByteTerrace.Interop.Vulkan;

internal static class ThrowHelper
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ExternalException CreateExternalException(VkResult error) =>
        new(
            errorCode: ((int)error),
            message: null
        );

    [DoesNotReturn]
    internal static T ThrowExternalException<T>(VkResult error) =>
        throw CreateExternalException(error: error);
}
