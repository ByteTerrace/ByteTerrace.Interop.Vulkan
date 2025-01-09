using System.Runtime.InteropServices;
using System.Text;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace ByteTerrace.Interop.Vulkan;

public static class VkHelpers
{
    public const nint VK_NULL_ALLOCATOR = 0;

    internal unsafe static SafeUnmanagedMemoryHandle GetUniqueStrings<T>(
        Encoding encoding,
        out uint foundCount,
        nuint stringsCount,
        SafeUnmanagedMemoryHandle stringsHandle,
        HashSet<string> searchValues
    ) where T : unmanaged {
        foundCount = uint.MinValue;

        var addRefCountSuccess = false;

        try {
            stringsHandle.DangerousAddRef(success: ref addRefCountSuccess);

            if (addRefCountSuccess) {
                var destinationHandle = SafeUnmanagedMemoryHandle.Create(size: ((nuint)(searchValues.Count * sizeof(nuint))));
                var destinationIndex = uint.MinValue;
                var destinationPointer = ((sbyte**)destinationHandle.DangerousGetHandle());
                var sourcePointer = stringsHandle.DangerousGetHandle();
                var uniqueNames = new HashSet<string>();

                for (var i = nuint.MinValue; (i < stringsCount); ++i) {
                    var name = encoding.GetString(bytes: MemoryMarshal.CreateReadOnlySpanFromNullTerminated(value: ((byte*)sourcePointer)));

                    if (searchValues.Contains(item: name) && uniqueNames.Add(item: name)) {
                        destinationPointer[destinationIndex++] = ((sbyte*)sourcePointer);
                        ++foundCount;
                    }

                    sourcePointer += sizeof(T);
                }

                return destinationHandle;
            }
        }
        finally {
            if (addRefCountSuccess) {
                stringsHandle.DangerousRelease();
            }
        }

        return SafeUnmanagedMemoryHandle.Create(size: nuint.MinValue);
    }

    public unsafe static SafeVulkanDeviceHandle GetDefaultLogicalGraphicsDeviceQueue(
        this VkPhysicalDevice physicalDevice,
        uint queueFamilyIndex,
        out VkQueue queue,
        HashSet<string> requestedExtensionNames,
        nint pAllocator = default
    ) {
        using var supportedExtensionPropertiesHandle = physicalDevice.GetSupportedDeviceExtensionProperties(
            count: out var supportedExtensionPropertyCount,
            result: out var _
        );
        using var enabledExtensionNamesHandle = GetUniqueStrings<VkExtensionProperties>(
            encoding: Encoding.UTF8,
            foundCount: out var enabledExtensionCount,
            searchValues: requestedExtensionNames,
            stringsHandle: supportedExtensionPropertiesHandle,
            stringsCount: supportedExtensionPropertyCount
        );

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
        var logicalDeviceCreateInfo = new VkDeviceCreateInfo {
            enabledExtensionCount = enabledExtensionCount,
            enabledLayerCount = uint.MinValue,
            flags = uint.MinValue,
            pEnabledFeatures = &enabledPhysicalDeviceFeatures,
            pNext = null,
            ppEnabledExtensionNames = ((sbyte**)enabledExtensionNamesHandle.DangerousGetHandle()),
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
    public unsafe static VkPhysicalDevice GetDefaultPhysicalGraphicsDeviceQueue(
        this SafeVulkanInstanceHandle instanceHandle,
        VkPhysicalDeviceType preferredDeviceType,
        out uint queueFamilyIndex
    ) {
        _ = instanceHandle.GetPhysicalDevices(out var physicalDevices);

        var physicalDevice = default(VkPhysicalDevice);
        var physicalDeviceCount = physicalDevices.Length;

        queueFamilyIndex = uint.MinValue;

        if (uint.MinValue < physicalDeviceCount) {
            VkPhysicalDevice? cpuPhysicalDevice = null;
            VkPhysicalDevice? discretePhysicalDevice = null;
            VkPhysicalDevice? integratedPhysicalDevice = null;
            VkPhysicalDevice? virtualPhysicalDevice = null;

            for (var i = uint.MinValue; (i < physicalDeviceCount); ++i) {
                var physicalDeviceProperties = new VkPhysicalDeviceProperties2 {
                    pNext = null,
                    sType = VkStructureType.VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_PROPERTIES_2,
                };

                physicalDevice = physicalDevices[i];

                vkGetPhysicalDeviceProperties2(
                    physicalDevice: physicalDevice,
                    pProperties: &physicalDeviceProperties
                );

                var physicalDeviceType = physicalDeviceProperties.properties.deviceType;

                if (physicalDeviceProperties.properties.deviceType == preferredDeviceType) {
                    cpuPhysicalDevice = null;
                    discretePhysicalDevice = null;
                    integratedPhysicalDevice = null;
                    virtualPhysicalDevice = null;

                    break;
                }
                else if ((cpuPhysicalDevice is null) && (VkPhysicalDeviceType.VK_PHYSICAL_DEVICE_TYPE_CPU == physicalDeviceType)) {
                    cpuPhysicalDevice = physicalDevice;
                }
                else if ((discretePhysicalDevice is null) && (VkPhysicalDeviceType.VK_PHYSICAL_DEVICE_TYPE_DISCRETE_GPU == physicalDeviceType)) {
                    discretePhysicalDevice = physicalDevice;
                }
                else if ((integratedPhysicalDevice is null) && (VkPhysicalDeviceType.VK_PHYSICAL_DEVICE_TYPE_INTEGRATED_GPU == physicalDeviceType)) {
                    integratedPhysicalDevice = physicalDevice;
                }
                else if ((virtualPhysicalDevice is null) && (VkPhysicalDeviceType.VK_PHYSICAL_DEVICE_TYPE_VIRTUAL_GPU == physicalDeviceType)) {
                    virtualPhysicalDevice = physicalDevice;
                }
            }

            if (discretePhysicalDevice.HasValue) {
                physicalDevice = discretePhysicalDevice.Value;
            }
            else if (integratedPhysicalDevice.HasValue) {
                physicalDevice = integratedPhysicalDevice.Value;
            }
            else if (cpuPhysicalDevice.HasValue) {
                physicalDevice = cpuPhysicalDevice.Value;
            }
            else if (virtualPhysicalDevice.HasValue) {
                physicalDevice = virtualPhysicalDevice.Value;
            }

            var queueFamilyProperties = physicalDevice.GetPhysicalDeviceQueueFamilyProperties();

            for (var j = uint.MinValue; (j < queueFamilyProperties.Length); ++j) {
                var properties = queueFamilyProperties[j];

                if (properties.queueFamilyProperties.queueFlags.HasFlag(flag: VkQueueFlags.VK_QUEUE_GRAPHICS_BIT)) {
                    queueFamilyIndex = j;

                    break;
                }
            }
        }

        return physicalDevice;
    }
    public unsafe static VkQueueFamilyProperties2[] GetPhysicalDeviceQueueFamilyProperties(this VkPhysicalDevice physicalDevice) {
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
    public unsafe static SafeUnmanagedMemoryHandle GetSwapchainImages(
        this VkDevice device,
        out uint count,
        out VkResult result,
        VkSwapchainKHR swapchain
    ) {
        var imageCount = uint.MinValue;

        result = vkGetSwapchainImagesKHR(
            device: device,
            swapchain: swapchain,
            pSwapchainImageCount: &imageCount,
            pSwapchainImages: null
        );

        if (VkResult.VK_SUCCESS == result) {
            var imagesHandle = SafeUnmanagedMemoryHandle.Create(size: (imageCount * ((uint)sizeof(VkImage))));

            result = vkGetSwapchainImagesKHR(
                device: device,
                swapchain: swapchain,
                pSwapchainImageCount: &imageCount,
                pSwapchainImages: ((VkImage*)imagesHandle.DangerousGetHandle())
            );

            if (VkResult.VK_SUCCESS == result) {
                count = imageCount;

                return imagesHandle;
            }
        }

        count = uint.MinValue;

        return SafeUnmanagedMemoryHandle.Create(size: nuint.MinValue);
    }
    public unsafe static SafeUnmanagedMemoryHandle GetSupportedDeviceExtensionProperties(
        this VkPhysicalDevice physicalDevice,
        out uint count,
        out VkResult result
    ) {
        var propertyCount = uint.MinValue;

        result = vkEnumerateDeviceExtensionProperties(
            physicalDevice: physicalDevice,
            pLayerName: null,
            pProperties: null,
            pPropertyCount: &propertyCount
        );

        if (VkResult.VK_SUCCESS == result) {
            var propertiesHandle = SafeUnmanagedMemoryHandle.Create(size: (propertyCount * ((uint)sizeof(VkExtensionProperties))));

            result = vkEnumerateDeviceExtensionProperties(
                physicalDevice: physicalDevice,
                pLayerName: null,
                pProperties: ((VkExtensionProperties*)propertiesHandle.DangerousGetHandle()),
                pPropertyCount: &propertyCount
            );

            if (VkResult.VK_SUCCESS == result) {
                count = propertyCount;

                return propertiesHandle;
            }
        }

        count = uint.MinValue;

        return SafeUnmanagedMemoryHandle.Create(size: nuint.MinValue);
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
