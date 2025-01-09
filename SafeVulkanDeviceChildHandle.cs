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
        nint pAllocator = 0
    ) where TCreateInfo : unmanaged {
        ArgumentNullException.ThrowIfNull(argument: createMethod, paramName: nameof(createMethod));
        ArgumentNullException.ThrowIfNull(argument: destroyMethod, paramName: nameof(destroyMethod));
        ArgumentNullException.ThrowIfNull(argument: logicalDeviceHandle, paramName: nameof(logicalDeviceHandle));

        var addRefCountSuccess = false;

        try {
            logicalDeviceHandle.DangerousAddRef(success: ref addRefCountSuccess);

            THandle handle;

            var result = createMethod(
                device: ((VkDevice)logicalDeviceHandle.DangerousGetHandle()),
                pAllocator: ((VkAllocationCallbacks*)pAllocator),
                pCreateInfo: &createInfo,
                pHandle: &handle
            );

            if (VkResult.VK_SUCCESS == result) {
                var childHandle = new SafeVulkanDeviceChildHandle<THandle>(
                    destroyMethod: destroyMethod,
                    deviceHandle: logicalDeviceHandle,
                    pAllocator: pAllocator
                );

                childHandle.SetHandle(handle: *((nint*)&handle));

                return childHandle;
            }

            return ThrowHelper.ThrowExternalException<SafeVulkanDeviceChildHandle<THandle>>(error: result);
        }
        catch {
            if (addRefCountSuccess) {
                logicalDeviceHandle.DangerousRelease();
            }

            throw;
        }
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
        var destroyMethod = m_destroyMethod;
        var deviceHandle = m_deviceHandle;
        var pAllocator = m_pAllocator;

        fixed (nint* pHandle = &handle) {
            destroyMethod(
                device: ((VkDevice)deviceHandle.DangerousGetHandle()),
                handle: *((THandle*)pHandle),
                pAllocator: ((VkAllocationCallbacks*)pAllocator)
           );
        }

        if ((deviceHandle is not null) && !deviceHandle.IsClosed && !deviceHandle.IsInvalid) {
            deviceHandle.DangerousRelease();
        }

        return true;
    }
}
