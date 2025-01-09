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
        out VkResult result,
        nint pAllocator = 0
    ) where TCreateInfo : unmanaged {
        result = VkResult.VK_ERROR_UNKNOWN;

        var addRefCountSuccess = false;
        var surfaceHandle = new SafeVulkanSurfaceHandle(
            destroyMethod: destroyMethod,
            instanceHandle: instanceHandle,
            pAllocator: pAllocator
        );

        instanceHandle.DangerousAddRef(success: ref addRefCountSuccess);

        if (addRefCountSuccess) {
            VkSurfaceKHR surface;

            result = createMethod(
                ((VkInstance)instanceHandle.DangerousGetHandle()),
                &createInfo,
                ((VkAllocationCallbacks*)pAllocator),
                &surface
            );

            if (VkResult.VK_SUCCESS == result) {
                surfaceHandle.SetHandle(handle: ((nint)surface));
            }
            else {
                instanceHandle.DangerousRelease();
            }
        }

        return surfaceHandle;
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
        m_destroyMethod(
            instance: ((VkInstance)m_instanceHandle.DangerousGetHandle()),
            pAllocator: ((VkAllocationCallbacks*)m_pAllocator),
            surface: ((VkSurfaceKHR)handle)
        );
        m_instanceHandle.DangerousRelease();

        return true;
    }
}
