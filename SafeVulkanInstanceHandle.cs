using Microsoft.Win32.SafeHandles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace ByteTerrace.Interop.Vulkan;

public sealed class SafeVulkanInstanceHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private static ReadOnlySpan<byte> Utf8vkCreateAndroidSurfaceKHR => "vkCreateAndroidSurfaceKHR\u0000"u8;
    private static ReadOnlySpan<byte> Utf8vkCreateHeadlessSurfaceEXT => "vkCreateHeadlessSurfaceEXT\u0000"u8;
    private static ReadOnlySpan<byte> Utf8vkCreateViSurfaceNN => "vkCreateViSurfaceNN\u0000"u8;
    private static ReadOnlySpan<byte> Utf8vkCreateWaylandSurfaceKHR => "vkCreateWaylandSurfaceKHR\u0000"u8;
    private static ReadOnlySpan<byte> Utf8vkCreateWin32SurfaceKHR => "vkCreateWin32SurfaceKHR\u0000"u8;
    private static ReadOnlySpan<byte> Utf8vkCreateXcbSurfaceKHR => "vkCreateXcbSurfaceKHR\u0000"u8;
    private static ReadOnlySpan<byte> Utf8vkCreateXlibSurfaceKHR => "vkCreateXlibSurfaceKHR\u0000"u8;
    private static ReadOnlySpan<byte> Utf8vkGetPhysicalDeviceWaylandPresentationSupportKHR => "vkGetPhysicalDeviceWaylandPresentationSupportKHR\u0000"u8;
    private static ReadOnlySpan<byte> Utf8vkGetPhysicalDeviceWin32PresentationSupportKHR => "vkGetPhysicalDeviceWin32PresentationSupportKHR\u0000"u8;
    private static ReadOnlySpan<byte> Utf8vkGetPhysicalDeviceXcbPresentationSupportKHR => "vkGetPhysicalDeviceXcbPresentationSupportKHR\u0000"u8;
    private static ReadOnlySpan<byte> Utf8vkGetPhysicalDeviceXlibPresentationSupportKHR => "vkGetPhysicalDeviceXlibPresentationSupportKHR\u0000"u8;

    [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
    private unsafe static sbyte* DangerousGetUtf8Pointer(ReadOnlySpan<byte> span) =>
        ((sbyte*)Unsafe.AsPointer(value: ref MemoryMarshal.GetReference(span: span)));
    private unsafe static VkResult GetInstanceExtensionProperties(out VkExtensionProperties[] properties) {
        var count = uint.MinValue;
        var result = vkEnumerateInstanceExtensionProperties(
            pLayerName: null,
            pProperties: null,
            pPropertyCount: &count
        );

        if (VkResult.VK_SUCCESS != result) { goto error; }

        properties = new VkExtensionProperties[count];

        fixed (VkExtensionProperties* pProperties = properties) {
            result = vkEnumerateInstanceExtensionProperties(
                pLayerName: default,
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
    private unsafe static VkResult GetInstanceLayerProperties(out VkLayerProperties[] properties) {
        var array = Array.Empty<VkLayerProperties>();
        var count = uint.MinValue;
        var result = vkEnumerateInstanceLayerProperties(
            pProperties: null,
            pPropertyCount: &count
        );

        if (VkResult.VK_SUCCESS != result) { goto error; }

        properties = new VkLayerProperties[count];

        fixed (VkLayerProperties* pProperties = properties) {
            result = vkEnumerateInstanceLayerProperties(
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
    private unsafe static VkResult GetPhysicalDevices(
        VkInstance instance,
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

    public unsafe static SafeVulkanInstanceHandle Create(
        uint apiVersion,
        string applicationName,
        uint applicationVersion,
        string engineName,
        uint engineVersion,
        HashSet<string> requestedExtensionNames,
        HashSet<string> requestedLayerNames,
        out VkResult result,
        nint pAllocator = 0
    ) {
        result = GetInstanceExtensionProperties(properties: out var supportedExtensionProperties);

        if (VkResult.VK_SUCCESS != result) { goto error; }

        result = GetInstanceLayerProperties(properties: out var supportedLayerProperties);

        if (VkResult.VK_SUCCESS != result) { goto error; }

        fixed (VkExtensionProperties* pSupportedExtensionProperties = supportedExtensionProperties)
        fixed (VkLayerProperties* pSupportedLayerProperties = supportedLayerProperties) {
            var enabledExtensionNames = new List<IntPtr>();
            var enabledLayerNames = new List<IntPtr>();
            var uniqueNames = new HashSet<string>();

            for (var i = 0; (i < supportedExtensionProperties.Length); ++i) {
                var pName = &pSupportedExtensionProperties[i];
                var name = Encoding.UTF8.GetString(bytes: MemoryMarshal.CreateReadOnlySpanFromNullTerminated(value: ((byte*)pName)));

                if (requestedExtensionNames.Contains(item: name) && uniqueNames.Add(item: name)) {
                    enabledExtensionNames.Add(item: ((IntPtr)pName));
                }
            }

            uniqueNames.Clear();

            for (var i = 0; (i < supportedExtensionProperties.Length); ++i) {
                var pName = &pSupportedLayerProperties[i];
                var name = Encoding.UTF8.GetString(bytes: MemoryMarshal.CreateReadOnlySpanFromNullTerminated(value: ((byte*)pName)));

                if (requestedLayerNames.Contains(item: name) && uniqueNames.Add(item: name)) {
                    enabledLayerNames.Add(item: ((IntPtr)pName));
                }
            }

            fixed (IntPtr* pEnabledExtensionNames = CollectionsMarshal.AsSpan(list: enabledExtensionNames))
            fixed (IntPtr* pEnabledLayerNames = CollectionsMarshal.AsSpan(list: enabledLayerNames)) {
                var applicationInfo = new VkApplicationInfo {
                    apiVersion = apiVersion,
                    applicationVersion = applicationVersion,
                    engineVersion = engineVersion,
                    pApplicationName = DangerousGetUtf8Pointer(span: Encoding.UTF8.GetBytes(s: applicationName).AsSpan()),
                    pEngineName = DangerousGetUtf8Pointer(span: Encoding.UTF8.GetBytes(s: engineName).AsSpan()),
                    pNext = null,
                    sType = VkStructureType.VK_STRUCTURE_TYPE_APPLICATION_INFO,
                };
                var instanceCreateInfo = new VkInstanceCreateInfo {
                    enabledExtensionCount = ((uint)enabledExtensionNames.Count),
                    enabledLayerCount = ((uint)enabledLayerNames.Count),
                    flags = uint.MinValue,
                    pApplicationInfo = &applicationInfo,
                    pNext = null,
                    ppEnabledExtensionNames = ((sbyte**)pEnabledExtensionNames),
                    ppEnabledLayerNames = ((sbyte**)pEnabledLayerNames),
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
                                pName: DangerousGetUtf8Pointer(span: Utf8vkCreateAndroidSurfaceKHR)
                            )),
                            vkCreateHeadlessSurfaceEXT = ((delegate* unmanaged<VkInstance, VkHeadlessSurfaceCreateInfoEXT*, VkAllocationCallbacks*, VkSurfaceKHR*, VkResult>)vkGetInstanceProcAddr(
                                instance: vkInstance,
                                pName: DangerousGetUtf8Pointer(span: Utf8vkCreateHeadlessSurfaceEXT)
                            )),
                            vkCreateViSurfaceNN = ((delegate* unmanaged<VkInstance, VkViSurfaceCreateInfoNN*, VkAllocationCallbacks*, VkSurfaceKHR*, VkResult>)vkGetInstanceProcAddr(
                                instance: vkInstance,
                                pName: DangerousGetUtf8Pointer(span: Utf8vkCreateViSurfaceNN)
                            )),
                            vkCreateWaylandSurfaceKHR = ((delegate* unmanaged<VkInstance, VkWaylandSurfaceCreateInfoKHR*, VkAllocationCallbacks*, VkSurfaceKHR*, VkResult>)vkGetInstanceProcAddr(
                                instance: vkInstance,
                                pName: DangerousGetUtf8Pointer(span: Utf8vkCreateWaylandSurfaceKHR)
                            )),
                            vkCreateWin32SurfaceKHR = ((delegate* unmanaged<VkInstance, VkWin32SurfaceCreateInfoKHR*, VkAllocationCallbacks*, VkSurfaceKHR*, VkResult>)vkGetInstanceProcAddr(
                                instance: vkInstance,
                                pName: DangerousGetUtf8Pointer(span: Utf8vkCreateWin32SurfaceKHR)
                            )),
                            vkCreateXcbSurfaceKHR = ((delegate* unmanaged<VkInstance, VkXcbSurfaceCreateInfoKHR*, VkAllocationCallbacks*, VkSurfaceKHR*, VkResult>)vkGetInstanceProcAddr(
                                instance: vkInstance,
                                pName: DangerousGetUtf8Pointer(span: Utf8vkCreateXcbSurfaceKHR)
                            )),
                            vkCreateXlibSurfaceKHR = ((delegate* unmanaged<VkInstance, VkXlibSurfaceCreateInfoKHR*, VkAllocationCallbacks*, VkSurfaceKHR*, VkResult>)vkGetInstanceProcAddr(
                                instance: vkInstance,
                                pName: DangerousGetUtf8Pointer(span: Utf8vkCreateXlibSurfaceKHR)
                            )),
                        },
                        physicalDeviceManualImports: new() {
                            vkGetPhysicalDeviceWaylandPresentationSupportKHR = ((delegate* unmanaged<VkPhysicalDevice, uint, void*, uint>)vkGetInstanceProcAddr(
                                instance: vkInstance,
                                pName: DangerousGetUtf8Pointer(span: Utf8vkGetPhysicalDeviceWaylandPresentationSupportKHR)
                            )),
                            vkGetPhysicalDeviceWin32PresentationSupportKHR = ((delegate* unmanaged<VkPhysicalDevice, uint, uint>)vkGetInstanceProcAddr(
                                instance: vkInstance,
                                pName: DangerousGetUtf8Pointer(span: Utf8vkGetPhysicalDeviceWin32PresentationSupportKHR)
                            )),
                            vkGetPhysicalDeviceXcbPresentationSupportKHR = ((delegate* unmanaged<VkPhysicalDevice, uint, void*, uint, uint>)vkGetInstanceProcAddr(
                                instance: vkInstance,
                                pName: DangerousGetUtf8Pointer(span: Utf8vkGetPhysicalDeviceXcbPresentationSupportKHR)
                            )),
                            vkGetPhysicalDeviceXlibPresentationSupportKHR = ((delegate* unmanaged<VkPhysicalDevice, uint, void*, nuint, uint>)vkGetInstanceProcAddr(
                                instance: vkInstance,
                                pName: DangerousGetUtf8Pointer(span: Utf8vkGetPhysicalDeviceXlibPresentationSupportKHR)
                            )),
                        },
                        pAllocator: pAllocator
                    );

                    instanceSafeHandle.SetHandle(handle: vkInstance);

                    return instanceSafeHandle;
                }
            }
        }

    error:
        return new SafeVulkanInstanceHandle(
            instanceManualImports: default,
            physicalDeviceManualImports: default,
            pAllocator: default
        );
    }

    private readonly nint m_pAllocator;
    private readonly VkInstanceManualImports m_instanceManualImports;
    private readonly VkPhysicalDeviceManualImports m_physicalDeviceManualImports;

    private SafeVulkanInstanceHandle(
        VkInstanceManualImports instanceManualImports,
        VkPhysicalDeviceManualImports physicalDeviceManualImports,
        nint pAllocator
    ) : base(ownsHandle: true) {
        m_instanceManualImports = instanceManualImports;
        m_physicalDeviceManualImports = physicalDeviceManualImports;
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
    public unsafe SafeVulkanSurfaceHandle CreateWin32Surface(
        void* hinstance,
        void* hwnd,
        nint pAllocator,
        out VkResult result
    ) =>
        SafeVulkanSurfaceHandle.Create(
            createInfo: new VkWin32SurfaceCreateInfoKHR {
                flags = uint.MinValue,
                hinstance = hinstance,
                hwnd = hwnd,
                pNext = null,
                sType = VkStructureType.VK_STRUCTURE_TYPE_WIN32_SURFACE_CREATE_INFO_KHR,
            },
            createMethod: m_instanceManualImports.vkCreateWin32SurfaceKHR,
            destroyMethod: vkDestroySurfaceKHR,
            instanceHandle: this,
            pAllocator: pAllocator,
            result: out result
        );
    public unsafe SafeVulkanSurfaceHandle CreateXcbSurface(
        void* connection,
        nint pAllocator,
        out VkResult result,
        uint window
    ) =>
        SafeVulkanSurfaceHandle.Create(
            createInfo: new VkXcbSurfaceCreateInfoKHR {
                connection = connection,
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
    public unsafe SafeVulkanSurfaceHandle CreateXlibSurface(
        void* display,
        nint pAllocator,
        out VkResult result,
        nuint window
    ) =>
        SafeVulkanSurfaceHandle.Create(
            createInfo: new VkXlibSurfaceCreateInfoKHR {
                dpy = display,
                flags = uint.MinValue,
                pNext = null,
                sType = VkStructureType.VK_STRUCTURE_TYPE_XLIB_SURFACE_CREATE_INFO_KHR,
                window = window,
            },
            createMethod: m_instanceManualImports.vkCreateXlibSurfaceKHR,
            destroyMethod: vkDestroySurfaceKHR,
            instanceHandle: this,
            pAllocator: pAllocator,
            result: out result
        );
    public VkResult GetPhysicalDevices(out VkPhysicalDevice[] physicalDevices) {
        var addRefSuccessful = false;
        var result = VkResult.VK_ERROR_UNKNOWN;

        DangerousAddRef(success: ref addRefSuccessful);

        try {
            if (addRefSuccessful) {
                result = GetPhysicalDevices(
                    instance: ((VkInstance)handle),
                    physicalDevices: out physicalDevices
                );
            }
            else {
                physicalDevices = [];
            }
        }
        finally {
            if (addRefSuccessful) {
                DangerousRelease();
            }
        }

        return result;
    }
    public unsafe VkBool32 GetPhysicalDeviceWaylandPresentationSupport(
        void* display,
        VkPhysicalDevice physicalDevice,
        uint queueFamilyIndex
    ) =>
        m_physicalDeviceManualImports.vkGetPhysicalDeviceWaylandPresentationSupportKHR(
            physicalDevice,
            queueFamilyIndex,
            display
        );
    public unsafe VkBool32 GetPhysicalDeviceWin32PresentationSupport(
        VkPhysicalDevice physicalDevice,
        uint queueFamilyIndex
    ) =>
        m_physicalDeviceManualImports.vkGetPhysicalDeviceWin32PresentationSupportKHR(
            physicalDevice,
            queueFamilyIndex
        );
    public unsafe VkBool32 GetPhysicalDeviceXcbPresentationSupport(
        void* connection,
        VkPhysicalDevice physicalDevice,
        uint queueFamilyIndex,
        uint visualId
    ) =>
        m_physicalDeviceManualImports.vkGetPhysicalDeviceXcbPresentationSupportKHR(
            physicalDevice,
            queueFamilyIndex,
            connection,
            visualId
        );
    public unsafe VkBool32 GetPhysicalDeviceXlibPresentationSupport(
        void* displayHandle,
        VkPhysicalDevice physicalDevice,
        uint queueFamilyIndex,
        nuint visualId
    ) =>
        m_physicalDeviceManualImports.vkGetPhysicalDeviceXlibPresentationSupportKHR(
            physicalDevice,
            queueFamilyIndex,
            displayHandle,
            visualId
        );
}
