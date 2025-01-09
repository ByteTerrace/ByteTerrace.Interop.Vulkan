using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

public static class VkInstanceExtensions
{
    public unsafe static VkResult GetPhysicalDevices(
        this VkInstance instance,
        out VkPhysicalDevice[] physicalDevices
    ) {
        var count = uint.MinValue;
        var result = vkEnumeratePhysicalDevices(
            instance: instance,
            pPhysicalDeviceCount: &count,
            pPhysicalDevices: null
        );

        if (VkResult.VK_SUCCESS != result) { goto error; }

        physicalDevices = new VkPhysicalDevice[count];

        fixed (VkPhysicalDevice* pPhysicalDevices = physicalDevices) {
            result = vkEnumeratePhysicalDevices(
                instance: instance,
                pPhysicalDeviceCount: &count,
                pPhysicalDevices: pPhysicalDevices
            );
        }

        if (VkResult.VK_SUCCESS != result) { goto error; }

        return result;
    error:
        physicalDevices = [];

        return result;
    }
}
