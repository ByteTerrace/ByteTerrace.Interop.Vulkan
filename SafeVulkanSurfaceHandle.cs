using Microsoft.Win32.SafeHandles;
using TerraFX.Interop.Vulkan;

namespace ByteTerrace.Interop.Vulkan;

public sealed class SafeVulkanSurfaceHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public unsafe delegate void SurfaceDestroyMethod(
        VkInstance instance,
        VkSurfaceKHR surface,
        VkAllocationCallbacks* pAllocator
    );

    public unsafe static SafeVulkanSurfaceHandle Create<TCreateInfo>(
        TCreateInfo createInfo,
        delegate* unmanaged<VkInstance, TCreateInfo*, VkAllocationCallbacks*, VkSurfaceKHR*, VkResult> createMethod,
        SurfaceDestroyMethod destroyMethod,
        SafeVulkanInstanceHandle instanceHandle,
        nint pAllocator = 0
    ) where TCreateInfo : unmanaged {
        ArgumentNullException.ThrowIfNull(argument: destroyMethod, paramName: nameof(destroyMethod));
        ArgumentNullException.ThrowIfNull(argument: instanceHandle, paramName: nameof(instanceHandle));

        var addRefCountSuccess = false;

        try {
            instanceHandle.DangerousAddRef(success: ref addRefCountSuccess);

            VkSurfaceKHR surface;

            var result = createMethod(
                ((VkInstance)instanceHandle.DangerousGetHandle()),
                &createInfo,
                ((VkAllocationCallbacks*)pAllocator),
                &surface
            );

            if (VkResult.VK_SUCCESS == result) {
                var surfaceHandle = new SafeVulkanSurfaceHandle(
                    destroyMethod: destroyMethod,
                    instanceHandle: instanceHandle,
                    pAllocator: pAllocator
                );

                surfaceHandle.SetHandle(handle: ((nint)surface));

                return surfaceHandle;
            }

            return ThrowHelper.ThrowExternalException<SafeVulkanSurfaceHandle>(error: result);
        }
        catch {
            if (addRefCountSuccess) {
                instanceHandle.DangerousRelease();
            }

            throw;
        }
    }

    private readonly SurfaceDestroyMethod m_destroyMethod;
    private readonly SafeVulkanInstanceHandle m_instanceHandle;
    private readonly nint m_pAllocator;

    private SafeVulkanSurfaceHandle(
        SurfaceDestroyMethod destroyMethod,
        SafeVulkanInstanceHandle instanceHandle,
        nint pAllocator
    ) : base(ownsHandle: true) {
        m_destroyMethod = destroyMethod;
        m_instanceHandle = instanceHandle;
        m_pAllocator = pAllocator;
    }

    protected unsafe override bool ReleaseHandle() {
        var destroyMethod = m_destroyMethod;
        var instanceHandle = m_instanceHandle;
        var pAllocator = m_pAllocator;

        destroyMethod(
            instance: ((VkInstance)instanceHandle.DangerousGetHandle()),
            pAllocator: ((VkAllocationCallbacks*)pAllocator),
            surface: ((VkSurfaceKHR)handle)
        );

        if ((instanceHandle is not null) && !instanceHandle.IsClosed && !instanceHandle.IsInvalid) {
            instanceHandle.DangerousRelease();
        }

        return true;
    }
}
