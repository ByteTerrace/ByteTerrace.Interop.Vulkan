using Microsoft.Win32.SafeHandles;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace ByteTerrace.Interop.Vulkan;

public sealed class SafeVulkanDeviceHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public unsafe static SafeVulkanDeviceHandle Create(
        VkDeviceCreateInfo createInfo,
        VkPhysicalDevice physicalDevice,
        nint pAllocator = default
    ) {
        var deviceHandle = new SafeVulkanDeviceHandle(pAllocator: pAllocator);

        VkDevice device;

        if (VkResult.VK_SUCCESS == vkCreateDevice(
            pAllocator: ((VkAllocationCallbacks*)pAllocator),
            pCreateInfo: &createInfo,
            pDevice: &device,
            physicalDevice: physicalDevice
        )) {
            deviceHandle.SetHandle(handle: device);
        }

        return deviceHandle;
    }

    private readonly nint m_pAllocator;

    private SafeVulkanDeviceHandle(nint pAllocator) : base(ownsHandle: true) {
        m_pAllocator = pAllocator;
    }

    protected unsafe override bool ReleaseHandle() {
        vkDestroyDevice(
            device: ((VkDevice)handle),
            pAllocator: ((VkAllocationCallbacks*)m_pAllocator)
        );

        return true;
    }
}
