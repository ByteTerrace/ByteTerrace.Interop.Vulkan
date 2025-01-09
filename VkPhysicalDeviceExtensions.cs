using System.Runtime.InteropServices;
using System.Text;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace ByteTerrace.Interop.Vulkan;

public static class VkPhysicalDeviceExtensions
{
    public unsafe static SafeVulkanDeviceHandle GetDefaultLogicalGraphicsDevice(
        this VkPhysicalDevice physicalDevice,
        uint queueFamilyIndex,
        out VkQueue queue,
        HashSet<string> requestedExtensionNames,
        out VkResult result,
        nint pAllocator = 0
    ) {
        result = GetExtensionProperties(
            physicalDevice: physicalDevice,
            properties: out var supportedExtensionProperties
        );

        var enabledExtensionNames = new List<IntPtr>();
        var uniqueNames = new HashSet<string>();

        fixed (VkExtensionProperties* pSupportedExtensionProperties = supportedExtensionProperties) {
            for (var i = 0; (i < supportedExtensionProperties.Length); ++i) {
                var pName = &pSupportedExtensionProperties[i];
                var name = Encoding.UTF8.GetString(bytes: MemoryMarshal.CreateReadOnlySpanFromNullTerminated(value: ((byte*)pName)));

                if (requestedExtensionNames.Contains(item: name) && uniqueNames.Add(item: name)) {
                    enabledExtensionNames.Add(item: ((IntPtr)pName));
                }
            }
        }

        var physicalDeviceSynchronization2Features = new VkPhysicalDeviceSynchronization2Features {
            pNext = null,
            sType = VkStructureType.VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_SYNCHRONIZATION_2_FEATURES,
        };
        var supportedPhysicalDeviceFeatures = new VkPhysicalDeviceFeatures2 {
            pNext = &physicalDeviceSynchronization2Features,
            sType = VkStructureType.VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_FEATURES_2,
        };

        vkGetPhysicalDeviceFeatures2(
            pFeatures: &supportedPhysicalDeviceFeatures,
            physicalDevice: physicalDevice
        );

        var enabledPhysicalDeviceFeatures = new VkPhysicalDeviceFeatures { };
        var logicalDeviceQueuePriorities = 1.0f;
        var logicalDeviceQueueCreateInfo = new VkDeviceQueueCreateInfo {
            flags = uint.MinValue,
            pNext = null,
            pQueuePriorities = &logicalDeviceQueuePriorities,
            queueCount = 1U,
            queueFamilyIndex = queueFamilyIndex,
            sType = VkStructureType.VK_STRUCTURE_TYPE_DEVICE_QUEUE_CREATE_INFO,
        };

        fixed (IntPtr* pEnabledExtensionNames = CollectionsMarshal.AsSpan(list: enabledExtensionNames)) {
            var logicalDeviceCreateInfo = new VkDeviceCreateInfo {
                enabledExtensionCount = ((uint)enabledExtensionNames.Count),
                enabledLayerCount = uint.MinValue,
                flags = uint.MinValue,
                pEnabledFeatures = &enabledPhysicalDeviceFeatures,
                pNext = null,
                ppEnabledExtensionNames = ((sbyte**)pEnabledExtensionNames),
                ppEnabledLayerNames = null,
                pQueueCreateInfos = &logicalDeviceQueueCreateInfo,
                queueCreateInfoCount = 1U,
                sType = VkStructureType.VK_STRUCTURE_TYPE_DEVICE_CREATE_INFO,
            };

            if (VK_TRUE == physicalDeviceSynchronization2Features.synchronization2) {
                logicalDeviceCreateInfo.pNext = &physicalDeviceSynchronization2Features;
            }

            var logicalDeviceHandle = SafeVulkanDeviceHandle.Create(
                createInfo: logicalDeviceCreateInfo,
                pAllocator: pAllocator,
                physicalDevice: physicalDevice
            );
            var logicalDeviceQueueInfo = new VkDeviceQueueInfo2 {
                flags = uint.MinValue,
                pNext = null,
                queueFamilyIndex = queueFamilyIndex,
                queueIndex = uint.MinValue,
                sType = VkStructureType.VK_STRUCTURE_TYPE_DEVICE_QUEUE_INFO_2,
            };

            VkQueue logicalDeviceQueue;

            vkGetDeviceQueue2(
                device: ((VkDevice)logicalDeviceHandle.DangerousGetHandle()),
                pQueue: &logicalDeviceQueue,
                pQueueInfo: &logicalDeviceQueueInfo
            );

            queue = logicalDeviceQueue;

            return logicalDeviceHandle;
        }
    }
    private unsafe static VkResult GetExtensionProperties(
        this VkPhysicalDevice physicalDevice,
        out VkExtensionProperties[] properties
    ) {
        var count = uint.MinValue;
        var result = vkEnumerateDeviceExtensionProperties(
            physicalDevice: physicalDevice,
            pLayerName: null,
            pProperties: null,
            pPropertyCount: &count
        );

        if (VkResult.VK_SUCCESS != result) { goto error; }

        properties = new VkExtensionProperties[count];

        fixed (VkExtensionProperties* pProperties = properties) {
            result = vkEnumerateDeviceExtensionProperties(
                physicalDevice: physicalDevice,
                pLayerName: null,
                pProperties: pProperties,
                pPropertyCount: &count
            );
        }

        if (VkResult.VK_SUCCESS != result) { goto error; }

        return result;
    error:
        properties = [];

        return result;
    }
    public unsafe static VkQueueFamilyProperties2[] GetQueueFamilyProperties(this VkPhysicalDevice physicalDevice) {
        var count = uint.MinValue;
        var properties = Array.Empty<VkQueueFamilyProperties2>();

        vkGetPhysicalDeviceQueueFamilyProperties2(
            physicalDevice: physicalDevice,
            pQueueFamilyProperties: null,
            pQueueFamilyPropertyCount: &count
        );

        properties = new VkQueueFamilyProperties2[count];

        for (var i = uint.MinValue; (i < properties.Length); ++i) {
            properties[i] = new VkQueueFamilyProperties2 {
                pNext = null,
                sType = VkStructureType.VK_STRUCTURE_TYPE_QUEUE_FAMILY_PROPERTIES_2,
            };
        }

        fixed (VkQueueFamilyProperties2* pProperties = properties) {
            vkGetPhysicalDeviceQueueFamilyProperties2(
                physicalDevice: physicalDevice,
                pQueueFamilyProperties: pProperties,
                pQueueFamilyPropertyCount: &count
            );
        }

        return properties;
    }
    public unsafe static VkResult IsImageFormatSupported(
        this VkPhysicalDevice physicalDevice,
        VkFormat imageFormat,
        VkImageUsageFlags imageUsageFlags
    ) {
        var deviceImageFormatInfo = new VkPhysicalDeviceImageFormatInfo2 {
            flags = uint.MinValue,
            format = imageFormat,
            pNext = null,
            sType = VkStructureType.VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_IMAGE_FORMAT_INFO_2,
            usage = imageUsageFlags,
        };
        var imageFormatProperties = new VkImageFormatProperties2 {
            pNext = null,
            sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_FORMAT_PROPERTIES_2,
        };

        return vkGetPhysicalDeviceImageFormatProperties2(
            physicalDevice: physicalDevice,
            pImageFormatInfo: &deviceImageFormatInfo,
            pImageFormatProperties: &imageFormatProperties
        );
    }
    public unsafe static VkResult IsSurfaceFormatSupported(
        this VkPhysicalDevice physicalDevice,
        VkColorSpaceKHR imageColorSpace,
        VkFormat imageFormat,
        VkSurfaceKHR surface
    ) {
        var count = uint.MinValue;
        var result = vkGetPhysicalDeviceSurfaceFormatsKHR(
            physicalDevice: physicalDevice,
            pSurfaceFormatCount: &count,
            pSurfaceFormats: null,
            surface: surface
        );

        if (VkResult.VK_SUCCESS != result) { goto error; }

        var formats = new VkSurfaceFormatKHR[count];

        fixed (VkSurfaceFormatKHR* pFormats = formats) {
            result = vkGetPhysicalDeviceSurfaceFormatsKHR(
                physicalDevice: physicalDevice,
                pSurfaceFormatCount: &count,
                pSurfaceFormats: pFormats,
                surface: surface
            );
        }

        if (VkResult.VK_SUCCESS != result) { goto error; }

        result = VkResult.VK_ERROR_UNKNOWN;

        for (var i = uint.MinValue; (i < count); ++i) {
            var format = formats[i];

            if ((imageColorSpace == format.colorSpace) && (imageFormat == format.format)) {
                result = VkResult.VK_SUCCESS;
                break;
            }
        }

    error:
        return result;
    }
    public unsafe static VkResult IsSurfacePresentModeSupported(
        this VkPhysicalDevice physicalDevice,
        VkPresentModeKHR presentMode,
        VkSurfaceKHR surface
    ) {
        var count = uint.MinValue;
        var result = vkGetPhysicalDeviceSurfacePresentModesKHR(
            physicalDevice: physicalDevice,
            pPresentModeCount: &count,
            pPresentModes: null,
            surface: surface
        );

        if (VkResult.VK_SUCCESS != result) { goto error; }

        var presentModes = new VkPresentModeKHR[count];

        fixed (VkPresentModeKHR* pPresentModes = presentModes) {
            result = vkGetPhysicalDeviceSurfacePresentModesKHR(
                physicalDevice: physicalDevice,
                pPresentModeCount: &count,
                pPresentModes: pPresentModes,
                surface: surface
            );
        }

        if (VkResult.VK_SUCCESS != result) { goto error; }

        result = VkResult.VK_ERROR_UNKNOWN;

        for (var i = uint.MinValue; (i < count); ++i) {
            if (presentMode == presentModes[i]) {
                result = VkResult.VK_SUCCESS;
                break;
            }
        }

    error:
        return result;
    }
}
