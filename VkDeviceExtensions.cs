using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

public static class VkDeviceExtensions
{
    public unsafe static VkResult GetSwapchainImages(
        this VkDevice device,
        out VkImage[] images,
        VkSwapchainKHR swapchain
    ) {
        var count = uint.MinValue;
        var result = vkGetSwapchainImagesKHR(
            device: device,
            pSwapchainImageCount: &count,
            pSwapchainImages: null,
            swapchain: swapchain
        );

        if (VkResult.VK_SUCCESS != result) { goto error; }

        images = new VkImage[count];

        fixed (VkImage* pImages = images) {
            result = vkGetSwapchainImagesKHR(
                device: device,
                pSwapchainImageCount: &count,
                pSwapchainImages: pImages,
                swapchain: swapchain
            );
        }

        if (VkResult.VK_SUCCESS != result) { goto error; }

        return result;
    error:
        images = [];

        return result;
    }
}
