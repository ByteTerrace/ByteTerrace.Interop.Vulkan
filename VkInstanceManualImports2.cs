using TerraFX.Interop.Vulkan;

namespace ByteTerrace.Interop.Vulkan;

public unsafe partial struct VkInstanceManualImports2
{
    public delegate* unmanaged<VkPhysicalDevice, uint, IntPtr, VkBool32> vkGetPhysicalDeviceWaylandPresentationSupportKHR;
    public delegate* unmanaged<VkPhysicalDevice, uint, VkBool32> vkGetPhysicalDeviceWin32PresentationSupportKHR;
    public delegate* unmanaged<VkPhysicalDevice, uint, IntPtr, uint, VkBool32> vkGetPhysicalDeviceXcbPresentationSupportKHR;
    public delegate* unmanaged<VkPhysicalDevice, uint, IntPtr, nuint, VkBool32> vkGetPhysicalDeviceXlibPresentationSupportKHR;
}
