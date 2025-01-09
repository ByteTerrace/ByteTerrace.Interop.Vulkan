using Microsoft.Win32.SafeHandles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using TerraFX.Interop.Vulkan;
using Windows.Win32.Foundation;
using XOrg.XCB;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace ByteTerrace.Interop.Vulkan;

public sealed class SafeVulkanInstanceHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private unsafe static SafeUnmanagedMemoryHandle GetSupportedInstanceExtensionProperties(out uint count) {
        count = uint.MinValue;

        var propertyCount = uint.MinValue;

        if (VkResult.VK_SUCCESS == vkEnumerateInstanceExtensionProperties(
            pLayerName: null,
            pProperties: null,
            pPropertyCount: &propertyCount
        )) {
            var propertiesHandle = SafeUnmanagedMemoryHandle.Create(size: (propertyCount * ((uint)sizeof(VkExtensionProperties))));

            if (VkResult.VK_SUCCESS == vkEnumerateInstanceExtensionProperties(
                pLayerName: default,
                pProperties: ((VkExtensionProperties*)propertiesHandle.DangerousGetHandle()),
                pPropertyCount: &propertyCount
            )) {
                count = propertyCount;

                return propertiesHandle;
            }
        }

        return SafeUnmanagedMemoryHandle.Create(size: nuint.MinValue);
    }
    private unsafe static SafeUnmanagedMemoryHandle GetSupportedInstanceLayerProperties(out uint count) {
        count = uint.MinValue;

        var propertyCount = uint.MinValue;

        if (VkResult.VK_SUCCESS == vkEnumerateInstanceLayerProperties(
            pProperties: null,
            pPropertyCount: &propertyCount
        )) {
            var propertiesHandle = SafeUnmanagedMemoryHandle.Create(size: (propertyCount * ((uint)sizeof(VkLayerProperties))));

            if (VkResult.VK_SUCCESS == vkEnumerateInstanceLayerProperties(
                pProperties: ((VkLayerProperties*)propertiesHandle.DangerousGetHandle()),
                pPropertyCount: &propertyCount
            )) {
                count = propertyCount;

                return propertiesHandle;
            }
        }

        return SafeUnmanagedMemoryHandle.Create(size: nuint.MinValue);
    }

    public unsafe static SafeVulkanInstanceHandle Create(
        uint apiVersion,
        string applicationName,
        uint applicationVersion,
        string engineName,
        uint engineVersion,
        HashSet<string> requestedExtensionNames,
        HashSet<string> requestedLayerNames,
        out VkResult result,
        nint pAllocator = VkHelpers.VK_NULL_ALLOCATOR
    ) {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static sbyte* DangerousGetPointer(ReadOnlySpan<byte> span) =>
            ((sbyte*)Unsafe.AsPointer(value: ref MemoryMarshal.GetReference(span: span)));

        result = VkResult.VK_ERROR_INITIALIZATION_FAILED;

        using var applicationNameHandle = SafeUnmanagedMemoryHandle.Create(encoding: Encoding.UTF8, value: applicationName);
        using var engineNameHandle = SafeUnmanagedMemoryHandle.Create(encoding: Encoding.UTF8, value: engineName);
        using var supportedExtensionPropertiesHandle = GetSupportedInstanceExtensionProperties(count: out var supportedExtensionPropertyCount);
        using var supportedLayerPropertiesHandle = GetSupportedInstanceLayerProperties(count: out var supportedLayerPropertyCount);
        using var enabledExtensionNamesHandle = VkHelpers.GetUniqueStrings<VkExtensionProperties>(
            encoding: Encoding.UTF8,
            foundCount: out var enabledExtensionCount,
            searchValues: requestedExtensionNames,
            stringsCount: supportedExtensionPropertyCount,
            stringsHandle: supportedExtensionPropertiesHandle
        );
        using var enabledLayerNamesHandle = VkHelpers.GetUniqueStrings<VkLayerProperties>(
            encoding: Encoding.UTF8,
            foundCount: out var enabledLayerCount,
            searchValues: requestedLayerNames,
            stringsHandle: supportedLayerPropertiesHandle,
            stringsCount: supportedLayerPropertyCount
        );

        var applicationInfo = new VkApplicationInfo {
            apiVersion = apiVersion,
            applicationVersion = applicationVersion,
            engineVersion = engineVersion,
            pApplicationName = ((sbyte*)applicationNameHandle.DangerousGetHandle()),
            pEngineName = ((sbyte*)engineNameHandle.DangerousGetHandle()),
            pNext = null,
            sType = VkStructureType.VK_STRUCTURE_TYPE_APPLICATION_INFO,
        };
        var instanceCreateInfo = new VkInstanceCreateInfo {
            enabledExtensionCount = enabledExtensionCount,
            enabledLayerCount = enabledLayerCount,
            flags = uint.MinValue,
            pApplicationInfo = &applicationInfo,
            pNext = null,
            ppEnabledExtensionNames = ((sbyte**)enabledExtensionNamesHandle.DangerousGetHandle()),
            ppEnabledLayerNames = ((sbyte**)enabledLayerNamesHandle.DangerousGetHandle()),
            sType = VkStructureType.VK_STRUCTURE_TYPE_INSTANCE_CREATE_INFO,
        };

        VkInstance vkInstance;

        result = vkCreateInstance(
            pAllocator: ((VkAllocationCallbacks*)pAllocator),
            pCreateInfo: &instanceCreateInfo,
            pInstance: &vkInstance
        );

        if (VkResult.VK_SUCCESS == result) {
            var instanceSafeHandle = new SafeVulkanInstanceHandle(
                instanceManualImports: new() {
                    vkCreateAndroidSurfaceKHR = ((delegate* unmanaged<VkInstance, VkAndroidSurfaceCreateInfoKHR*, VkAllocationCallbacks*, VkSurfaceKHR*, VkResult>)vkGetInstanceProcAddr(
                        instance: vkInstance,
                        pName: DangerousGetPointer(span: "vkCreateAndroidSurfaceKHR\u0000"u8)
                    )),
                    vkCreateHeadlessSurfaceEXT = ((delegate* unmanaged<VkInstance, VkHeadlessSurfaceCreateInfoEXT*, VkAllocationCallbacks*, VkSurfaceKHR*, VkResult>)vkGetInstanceProcAddr(
                        instance: vkInstance,
                        pName: DangerousGetPointer(span: "vkCreateHeadlessSurfaceEXT\u0000"u8)
                    )),
                    vkCreateViSurfaceNN = ((delegate* unmanaged<VkInstance, VkViSurfaceCreateInfoNN*, VkAllocationCallbacks*, VkSurfaceKHR*, VkResult>)vkGetInstanceProcAddr(
                        instance: vkInstance,
                        pName: DangerousGetPointer(span: "vkCreateViSurfaceNN\u0000"u8)
                    )),
                    vkCreateWaylandSurfaceKHR = ((delegate* unmanaged<VkInstance, VkWaylandSurfaceCreateInfoKHR*, VkAllocationCallbacks*, VkSurfaceKHR*, VkResult>)vkGetInstanceProcAddr(
                        instance: vkInstance,
                        pName: DangerousGetPointer(span: "vkCreateWaylandSurfaceKHR\u0000"u8)
                    )),
                    vkCreateWin32SurfaceKHR = ((delegate* unmanaged<VkInstance, VkWin32SurfaceCreateInfoKHR*, VkAllocationCallbacks*, VkSurfaceKHR*, VkResult>)vkGetInstanceProcAddr(
                        instance: vkInstance,
                        pName: DangerousGetPointer(span: "vkCreateWin32SurfaceKHR\u0000"u8)
                    )),
                    vkCreateXcbSurfaceKHR = ((delegate* unmanaged<VkInstance, VkXcbSurfaceCreateInfoKHR*, VkAllocationCallbacks*, VkSurfaceKHR*, VkResult>)vkGetInstanceProcAddr(
                        instance: vkInstance,
                        pName: DangerousGetPointer(span: "vkCreateXcbSurfaceKHR\u0000"u8)
                    )),
                    vkCreateXlibSurfaceKHR = ((delegate* unmanaged<VkInstance, VkXlibSurfaceCreateInfoKHR*, VkAllocationCallbacks*, VkSurfaceKHR*, VkResult>)vkGetInstanceProcAddr(
                        instance: vkInstance,
                        pName: DangerousGetPointer(span: "vkCreateXlibSurfaceKHR\u0000"u8)
                    )),
                },
                instanceManualImports2: new() {
                    vkGetPhysicalDeviceWaylandPresentationSupportKHR = ((delegate* unmanaged<VkPhysicalDevice, uint, IntPtr, VkBool32>)vkGetInstanceProcAddr(
                        instance: vkInstance,
                        pName: DangerousGetPointer(span: "vkGetPhysicalDeviceWaylandPresentationSupportKHR\u0000"u8)
                    )),
                    vkGetPhysicalDeviceWin32PresentationSupportKHR = ((delegate* unmanaged<VkPhysicalDevice, uint, VkBool32>)vkGetInstanceProcAddr(
                        instance: vkInstance,
                        pName: DangerousGetPointer(span: "vkGetPhysicalDeviceWin32PresentationSupportKHR\u0000"u8)
                    )),
                    vkGetPhysicalDeviceXcbPresentationSupportKHR = ((delegate* unmanaged<VkPhysicalDevice, uint, IntPtr, uint, VkBool32>)vkGetInstanceProcAddr(
                        instance: vkInstance,
                        pName: DangerousGetPointer(span: "vkGetPhysicalDeviceXcbPresentationSupportKHR\u0000"u8)
                    )),
                    vkGetPhysicalDeviceXlibPresentationSupportKHR = ((delegate* unmanaged<VkPhysicalDevice, uint, IntPtr, nuint, VkBool32>)vkGetInstanceProcAddr(
                        instance: vkInstance,
                        pName: DangerousGetPointer(span: "vkGetPhysicalDeviceXlibPresentationSupportKHR\u0000"u8)
                    )),
                },
                pAllocator: pAllocator
            );

            instanceSafeHandle.SetHandle(handle: vkInstance);

            return instanceSafeHandle;
        }

        return new SafeVulkanInstanceHandle(
            instanceManualImports: default,
            instanceManualImports2: default,
            pAllocator: default
        );
    }

    private readonly nint m_pAllocator;
    private readonly VkInstanceManualImports m_instanceManualImports;
    private readonly VkInstanceManualImports2 m_instanceManualImports2;

    private SafeVulkanInstanceHandle(
        VkInstanceManualImports instanceManualImports,
        VkInstanceManualImports2 instanceManualImports2,
        nint pAllocator
    ) : base(ownsHandle: true) {
        m_instanceManualImports = instanceManualImports;
        m_instanceManualImports2 = instanceManualImports2;
        m_pAllocator = pAllocator;
    }

    protected unsafe override bool ReleaseHandle() {
        vkDestroyInstance(
            instance: ((VkInstance)handle),
            pAllocator: (VkAllocationCallbacks*)m_pAllocator
        );

        return true;
    }

    public unsafe SafeVulkanSurfaceHandle CreateSurface(
        nint pAllocator,
        out VkResult result
    ) =>
        SafeVulkanSurfaceHandle.Create(
            createInfo: new VkHeadlessSurfaceCreateInfoEXT {
                flags = uint.MinValue,
                pNext = null,
                sType = VkStructureType.VK_STRUCTURE_TYPE_WIN32_SURFACE_CREATE_INFO_KHR,
            },
            createMethod: m_instanceManualImports.vkCreateHeadlessSurfaceEXT,
            destroyMethod: vkDestroySurfaceKHR,
            instanceHandle: this,
            pAllocator: pAllocator,
            result: out result
        );
    public unsafe SafeVulkanSurfaceHandle CreateSurface(
        void* display,
        nint pAllocator,
        out VkResult result,
        void* surface
    ) =>
        SafeVulkanSurfaceHandle.Create(
            createInfo: new VkWaylandSurfaceCreateInfoKHR {
                display = display,
                flags = uint.MinValue,
                pNext = null,
                sType = VkStructureType.VK_STRUCTURE_TYPE_WAYLAND_SURFACE_CREATE_INFO_KHR,
                surface = surface,
            },
            createMethod: m_instanceManualImports.vkCreateWaylandSurfaceKHR,
            destroyMethod: vkDestroySurfaceKHR,
            instanceHandle: this,
            pAllocator: pAllocator,
            result: out result
        );
    public unsafe SafeVulkanSurfaceHandle CreateSurface(
        nint pAllocator,
        out VkResult result,
        HINSTANCE win32InstanceHandle,
        HWND win32WindowHandle
    ) =>
        SafeVulkanSurfaceHandle.Create(
            createInfo: new VkWin32SurfaceCreateInfoKHR {
                flags = uint.MinValue,
                hinstance = ((void*)(IntPtr)win32InstanceHandle),
                hwnd = ((void*)(IntPtr)win32WindowHandle),
                pNext = null,
                sType = VkStructureType.VK_STRUCTURE_TYPE_WIN32_SURFACE_CREATE_INFO_KHR,
            },
            createMethod: m_instanceManualImports.vkCreateWin32SurfaceKHR,
            destroyMethod: vkDestroySurfaceKHR,
            instanceHandle: this,
            pAllocator: pAllocator,
            result: out result
        );
    public unsafe SafeVulkanSurfaceHandle CreateSurface(
        XcbConnection connection,
        nint pAllocator,
        out VkResult result,
        uint window
    ) =>
        SafeVulkanSurfaceHandle.Create(
            createInfo: new VkXcbSurfaceCreateInfoKHR {
                connection = ((void*)(IntPtr)connection),
                flags = uint.MinValue,
                pNext = null,
                sType = VkStructureType.VK_STRUCTURE_TYPE_XCB_SURFACE_CREATE_INFO_KHR,
                window = window,
            },
            createMethod: m_instanceManualImports.vkCreateXcbSurfaceKHR,
            destroyMethod: vkDestroySurfaceKHR,
            instanceHandle: this,
            pAllocator: pAllocator,
            result: out result
        );
    public unsafe SafeVulkanSurfaceHandle CreateSurface(
        void* display,
        nint pAllocator,
        out VkResult result,
        nuint window
    ) =>
        SafeVulkanSurfaceHandle.Create(
            createInfo: new VkXlibSurfaceCreateInfoKHR {
                dpy = ((void*)(IntPtr)display),
                flags = uint.MinValue,
                pNext = null,
                sType = VkStructureType.VK_STRUCTURE_TYPE_XLIB_SURFACE_CREATE_INFO_KHR,
                window = ((nuint)(IntPtr)window),
            },
            createMethod: m_instanceManualImports.vkCreateXlibSurfaceKHR,
            destroyMethod: vkDestroySurfaceKHR,
            instanceHandle: this,
            pAllocator: pAllocator,
            result: out result
        );
    public unsafe VkBool32 GetPhysicalDeviceWin32PresentationSupport(
        VkPhysicalDevice physicalDevice,
        uint queueFamilyIndex
    ) =>
        m_instanceManualImports2.vkGetPhysicalDeviceWin32PresentationSupportKHR(
            physicalDevice,
            queueFamilyIndex
        );
    public unsafe VkBool32 GetPhysicalDeviceXcbPresentationSupport(
        VkPhysicalDevice physicalDevice,
        uint queueFamilyIndex,
        XcbConnection connection,
        uint visualId
    ) =>
        m_instanceManualImports2.vkGetPhysicalDeviceXcbPresentationSupportKHR(
            physicalDevice,
            queueFamilyIndex,
            connection,
            visualId
        );
    public unsafe VkBool32 GetPhysicalDeviceXlibPresentationSupport(
        VkPhysicalDevice physicalDevice,
        uint queueFamilyIndex,
        IntPtr displayHandle,
        nuint visualId
    ) =>
        m_instanceManualImports2.vkGetPhysicalDeviceXlibPresentationSupportKHR(
            physicalDevice,
            queueFamilyIndex,
            displayHandle,
            visualId
        );
}
