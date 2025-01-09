using Microsoft.Win32.SafeHandles;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace ByteTerrace.Interop.Vulkan;

public sealed class SafeVulkanPipelineHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public unsafe static SafeVulkanPipelineHandle Create(
        VkGraphicsPipelineCreateInfo createInfo,
        SafeVulkanDeviceHandle logicalDeviceHandle,
        VkPipelineCache pipelineCache,
        out VkResult result,
        nint pAllocator = 0
    ) {
        result = VkResult.VK_ERROR_INITIALIZATION_FAILED;

        var addRefCountSuccess = false;
        var pipelineHandle = new SafeVulkanPipelineHandle(
            deviceHandle: logicalDeviceHandle,
            pAllocator: pAllocator
        );

        logicalDeviceHandle.DangerousAddRef(success: ref addRefCountSuccess);

        if (addRefCountSuccess) {
            VkPipeline pipeline;

            result = vkCreateGraphicsPipelines(
                createInfoCount: 1U,
                device: ((VkDevice)logicalDeviceHandle.DangerousGetHandle()),
                pAllocator: ((VkAllocationCallbacks*)pAllocator),
                pCreateInfos: &createInfo,
                pipelineCache: pipelineCache,
                pPipelines: &pipeline
            );

            if (VkResult.VK_SUCCESS == result) {
                pipelineHandle.SetHandle(handle: ((nint)pipeline));
            }
            else {
                logicalDeviceHandle.DangerousRelease();
            }
        }

        return pipelineHandle;
    }

    private readonly SafeVulkanDeviceHandle m_deviceHandle;
    private readonly nint m_pAllocator;

    private SafeVulkanPipelineHandle(
        SafeVulkanDeviceHandle deviceHandle,
        nint pAllocator
    ) : base(ownsHandle: true) {
        m_deviceHandle = deviceHandle;
        m_pAllocator = pAllocator;
    }

    protected unsafe override bool ReleaseHandle() {
        vkDestroyPipeline(
            device: ((VkDevice)m_deviceHandle.DangerousGetHandle()),
            pAllocator: ((VkAllocationCallbacks*)m_pAllocator),
            pipeline: ((VkPipeline)handle)
        );
        m_deviceHandle.DangerousRelease();

        return true;
    }
}
