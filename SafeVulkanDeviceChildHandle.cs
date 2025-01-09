using Microsoft.Win32.SafeHandles;
using TerraFX.Interop.Vulkan;

namespace ByteTerrace.Interop.Vulkan;

public sealed class SafeVulkanDeviceChildHandle<THandle> : SafeHandleZeroOrMinusOneIsInvalid where THandle : unmanaged
{
    public unsafe delegate VkResult DeviceChildCreateMethod<TCreateInfo>(
        VkDevice device,
        TCreateInfo* pCreateInfo,
        VkAllocationCallbacks* pAllocator,
        THandle* pHandle
    ) where TCreateInfo : unmanaged;
    public unsafe delegate void DeviceChildDestroyMethod(
        VkDevice device,
        THandle handle,
        VkAllocationCallbacks* pAllocator
    );

    public unsafe static SafeVulkanDeviceChildHandle<THandle> Create<TCreateInfo>(
        TCreateInfo createInfo,
        DeviceChildCreateMethod<TCreateInfo> createMethod,
        DeviceChildDestroyMethod destroyMethod,
        SafeVulkanDeviceHandle logicalDeviceHandle,
        out VkResult result,
        nint pAllocator = VkHelpers.VK_NULL_ALLOCATOR
    ) where TCreateInfo : unmanaged {
        result = VkResult.VK_ERROR_INITIALIZATION_FAILED;

        var addRefCountSuccess = false;
        var childHandle = new SafeVulkanDeviceChildHandle<THandle>(
            destroyMethod: destroyMethod,
            deviceHandle: logicalDeviceHandle,
            pAllocator: pAllocator
        );

        logicalDeviceHandle.DangerousAddRef(success: ref addRefCountSuccess);

        if (addRefCountSuccess) {
            THandle handle;

            result = createMethod(
                device: ((VkDevice)logicalDeviceHandle.DangerousGetHandle()),
                pAllocator: ((VkAllocationCallbacks*)pAllocator),
                pCreateInfo: &createInfo,
                pHandle: &handle
            );

            if (VkResult.VK_SUCCESS == result) {
                childHandle.SetHandle(handle: *((nint*)&handle));
            }
            else {
                logicalDeviceHandle.DangerousRelease();
            }
        }

        return childHandle;
    }

    private readonly DeviceChildDestroyMethod m_destroyMethod;
    private readonly SafeVulkanDeviceHandle m_deviceHandle;
    private readonly nint m_pAllocator;

    private SafeVulkanDeviceChildHandle(
        DeviceChildDestroyMethod destroyMethod,
        SafeVulkanDeviceHandle deviceHandle,
        nint pAllocator
    ) : base(ownsHandle: true) {
        m_destroyMethod = destroyMethod;
        m_deviceHandle = deviceHandle;
        m_pAllocator = pAllocator;
    }

    protected unsafe override bool ReleaseHandle() {
        fixed (nint* pHandle = &handle) {
            m_destroyMethod(
                device: ((VkDevice)m_deviceHandle.DangerousGetHandle()),
                handle: *((THandle*)pHandle),
                pAllocator: ((VkAllocationCallbacks*)m_pAllocator)
           );
        }

        m_deviceHandle.DangerousRelease();

        return true;
    }
}
