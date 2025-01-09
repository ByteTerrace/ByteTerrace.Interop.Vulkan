using Microsoft.Win32.SafeHandles;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace ByteTerrace.Interop.Vulkan;

public sealed class SafeVulkanDeviceHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public unsafe static SafeVulkanDeviceHandle Create(
        VkDeviceCreateInfo createInfo,
        VkPhysicalDevice physicalDevice,
        nint pAllocator = 0
    ) {
        VkDevice device;

        var result = vkCreateDevice(
            pAllocator: ((VkAllocationCallbacks*)pAllocator),
            pCreateInfo: &createInfo,
            pDevice: &device,
            physicalDevice: physicalDevice
        );

        if (VkResult.VK_SUCCESS == result) {
            var deviceHandle = new SafeVulkanDeviceHandle(pAllocator: pAllocator);

            deviceHandle.SetHandle(handle: device);

            return deviceHandle;
        }

        return ThrowHelper.ThrowExternalException<SafeVulkanDeviceHandle>(error: result);
    }

    private readonly nint m_pAllocator;

    private SafeVulkanDeviceHandle(nint pAllocator) : base(ownsHandle: true) {
        m_pAllocator = pAllocator;
    }

    protected unsafe override bool ReleaseHandle() {
        var pAllocator = m_pAllocator;

        vkDestroyDevice(
            device: ((VkDevice)handle),
            pAllocator: ((VkAllocationCallbacks*)pAllocator)
        );

        return true;
    }
}
