using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace ByteTerrace.Interop.Vulkan;
public static class SafeVulkanDeviceHandleExtensions
{
    public unsafe static SafeVulkanDeviceChildHandle<VkCommandPool> CreateHandle(
        this SafeVulkanDeviceHandle logicalDeviceHandle,
        VkCommandPoolCreateInfo createInfo,
        nint pAllocator,
        out VkResult result
    ) =>
        SafeVulkanDeviceChildHandle<VkCommandPool>.Create(
            createInfo: createInfo,
            createMethod: vkCreateCommandPool,
            destroyMethod: vkDestroyCommandPool,
            logicalDeviceHandle: logicalDeviceHandle,
            pAllocator: pAllocator,
            result: out result
        );
    public unsafe static SafeVulkanDeviceChildHandle<VkFence> CreateHandle(
        this SafeVulkanDeviceHandle logicalDeviceHandle,
        VkFenceCreateInfo createInfo,
        nint pAllocator,
        out VkResult result
    ) =>
        SafeVulkanDeviceChildHandle<VkFence>.Create(
            createInfo: createInfo,
            createMethod: vkCreateFence,
            destroyMethod: vkDestroyFence,
            logicalDeviceHandle: logicalDeviceHandle,
            pAllocator: pAllocator,
            result: out result
        );
    public unsafe static SafeVulkanDeviceChildHandle<VkFramebuffer> CreateHandle(
        this SafeVulkanDeviceHandle logicalDeviceHandle,
        VkFramebufferCreateInfo createInfo,
        nint pAllocator,
        out VkResult result
    ) =>
        SafeVulkanDeviceChildHandle<VkFramebuffer>.Create(
            createInfo: createInfo,
            createMethod: vkCreateFramebuffer,
            destroyMethod: vkDestroyFramebuffer,
            logicalDeviceHandle: logicalDeviceHandle,
            pAllocator: pAllocator,
            result: out result
        );
    public unsafe static SafeVulkanDeviceChildHandle<VkImageView> CreateHandle(
        this SafeVulkanDeviceHandle logicalDeviceHandle,
        VkImageViewCreateInfo createInfo,
        nint pAllocator,
        out VkResult result
    ) =>
        SafeVulkanDeviceChildHandle<VkImageView>.Create(
            createInfo: createInfo,
            createMethod: vkCreateImageView,
            destroyMethod: vkDestroyImageView,
            logicalDeviceHandle: logicalDeviceHandle,
            pAllocator: pAllocator,
            result: out result
        );
    public unsafe static SafeVulkanDeviceChildHandle<VkPipelineCache> CreateHandle(
        this SafeVulkanDeviceHandle logicalDeviceHandle,
        VkPipelineCacheCreateInfo createInfo,
        nint pAllocator,
        out VkResult result
    ) =>
        SafeVulkanDeviceChildHandle<VkPipelineCache>.Create(
            createInfo: createInfo,
            createMethod: vkCreatePipelineCache,
            destroyMethod: vkDestroyPipelineCache,
            logicalDeviceHandle: logicalDeviceHandle,
            pAllocator: pAllocator,
            result: out result
        );
    public unsafe static SafeVulkanDeviceChildHandle<VkPipelineLayout> CreateHandle(
        this SafeVulkanDeviceHandle logicalDeviceHandle,
        VkPipelineLayoutCreateInfo createInfo,
        nint pAllocator,
        out VkResult result
    ) =>
        SafeVulkanDeviceChildHandle<VkPipelineLayout>.Create(
            createInfo: createInfo,
            createMethod: vkCreatePipelineLayout,
            destroyMethod: vkDestroyPipelineLayout,
            logicalDeviceHandle: logicalDeviceHandle,
            pAllocator: pAllocator,
            result: out result
        );
    public unsafe static SafeVulkanDeviceChildHandle<VkRenderPass> CreateHandle(
        this SafeVulkanDeviceHandle logicalDeviceHandle,
        VkRenderPassCreateInfo2 createInfo,
        nint pAllocator,
        out VkResult result
    ) =>
        SafeVulkanDeviceChildHandle<VkRenderPass>.Create(
            createInfo: createInfo,
            createMethod: vkCreateRenderPass2,
            destroyMethod: vkDestroyRenderPass,
            logicalDeviceHandle: logicalDeviceHandle,
            pAllocator: pAllocator,
            result: out result
        );
    public unsafe static SafeVulkanDeviceChildHandle<VkSemaphore> CreateHandle(
        this SafeVulkanDeviceHandle logicalDeviceHandle,
        VkSemaphoreCreateInfo createInfo,
        nint pAllocator,
        out VkResult result
    ) =>
        SafeVulkanDeviceChildHandle<VkSemaphore>.Create(
            createInfo: createInfo,
            createMethod: vkCreateSemaphore,
            destroyMethod: vkDestroySemaphore,
            logicalDeviceHandle: logicalDeviceHandle,
            pAllocator: pAllocator,
            result: out result
        );
    public unsafe static SafeVulkanDeviceChildHandle<VkShaderModule> CreateHandle(
        this SafeVulkanDeviceHandle logicalDeviceHandle,
        VkShaderModuleCreateInfo createInfo,
        nint pAllocator,
        out VkResult result
    ) =>
        SafeVulkanDeviceChildHandle<VkShaderModule>.Create(
            createInfo: createInfo,
            createMethod: vkCreateShaderModule,
            destroyMethod: vkDestroyShaderModule,
            logicalDeviceHandle: logicalDeviceHandle,
            pAllocator: pAllocator,
            result: out result
        );
    public unsafe static SafeVulkanDeviceChildHandle<VkSwapchainKHR> CreateHandle(
        this SafeVulkanDeviceHandle logicalDeviceHandle,
        VkSwapchainCreateInfoKHR createInfo,
        nint pAllocator,
        out VkResult result
    ) =>
        SafeVulkanDeviceChildHandle<VkSwapchainKHR>.Create(
            createInfo: createInfo,
            createMethod: vkCreateSwapchainKHR,
            destroyMethod: vkDestroySwapchainKHR,
            logicalDeviceHandle: logicalDeviceHandle,
            pAllocator: pAllocator,
            result: out result
        );
}
