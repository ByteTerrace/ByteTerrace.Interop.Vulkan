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
        nint pAllocator = 0
    ) {
        ArgumentNullException.ThrowIfNull(argument: logicalDeviceHandle, paramName: nameof(logicalDeviceHandle));

        var addRefCountSuccess = false;

        try {
            logicalDeviceHandle.DangerousAddRef(success: ref addRefCountSuccess);

            VkPipeline pipeline;

            var result = vkCreateGraphicsPipelines(
                createInfoCount: 1U,
                device: ((VkDevice)logicalDeviceHandle.DangerousGetHandle()),
                pAllocator: ((VkAllocationCallbacks*)pAllocator),
                pCreateInfos: &createInfo,
                pipelineCache: pipelineCache,
                pPipelines: &pipeline
            );

            if (VkResult.VK_SUCCESS == result) {
                var pipelineHandle = new SafeVulkanPipelineHandle(
                    deviceHandle: logicalDeviceHandle,
                    pAllocator: pAllocator
                );

                pipelineHandle.SetHandle(handle: ((nint)pipeline));

                return pipelineHandle;
            }

            return ThrowHelper.ThrowExternalException<SafeVulkanPipelineHandle>(error: result);
        }
        catch {
            if (addRefCountSuccess) {
                logicalDeviceHandle.DangerousRelease();
            }

            throw;
        }
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
        var deviceHandle = m_deviceHandle;
        var pAllocator = m_pAllocator;

        vkDestroyPipeline(
            device: ((VkDevice)deviceHandle.DangerousGetHandle()),
            pAllocator: ((VkAllocationCallbacks*)pAllocator),
            pipeline: ((VkPipeline)handle)
        );

        if ((deviceHandle is not null) && !deviceHandle.IsClosed && !deviceHandle.IsInvalid) {
            try { deviceHandle.DangerousRelease(); } catch {}
        }

        return true;
    }
}
