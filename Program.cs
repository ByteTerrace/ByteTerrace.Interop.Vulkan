using ByteTerrace.Interop.Vulkan;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using TerraFX.Interop.Vulkan;
using TerraFX.Interop.Xlib;
using WaylandSharp;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using XOrg.X11;
using XOrg.XCB;
using static TerraFX.Interop.Mimalloc.Mimalloc;
using static TerraFX.Interop.Vulkan.Vulkan;

/*

export DOTNET_FILE=dotnet-sdk-9.0.101-linux-x64.tar.gz
export DOTNET_ROOT=$HOME/.dotnet
mkdir -p "$DOTNET_ROOT" && tar zxf "$DOTNET_FILE" -C "$DOTNET_ROOT"
flatpak override --user --env=PATH=/app/bin:/app/bin:/app/bin:/usr/bin:/home/deck/.var/app/com.visualstudio.code/data/node_modules/bin:/home/deck/.dotnet com.visualstudio.code

*/

VkAllocationCallbacks vulkanAllocationCallbacks;
nint vulkanAllocationCallbacksPointer;
VkResult vulkanResult;

unsafe {
#if DEBUG
    mi_register_deferred_free(
        arg: null,
        fn: (bool force, ulong heartbeat, void* _) => {
            mi_collect(force: force);
        }
    );
#endif

    [UnmanagedCallersOnly]
    static void* PfnAllocation(void* pUserData, nuint size, nuint alignment, VkSystemAllocationScope allocationScope) =>
        mi_malloc_aligned(alignment: alignment, size: size);
    [UnmanagedCallersOnly]
    static void PfnFree(void* pUserData, void* pMemory) =>
        mi_free(p: pMemory);
    [UnmanagedCallersOnly]
    static void* PfnReallocation(void* pUserData, void* pOriginal, nuint size, nuint alignment, VkSystemAllocationScope allocationScope) =>
        mi_realloc_aligned(alignment: alignment, newsize: size, p: pOriginal);

    vulkanAllocationCallbacks = new VkAllocationCallbacks {
        pfnAllocation = &PfnAllocation,
        pfnFree = &PfnFree,
        pfnReallocation = &PfnReallocation,
    };
    vulkanAllocationCallbacksPointer = ((nint)(&vulkanAllocationCallbacks));
}

using var vulkanInstanceHandle = SafeVulkanInstanceHandle.Create(
    apiVersion: VK_API_VERSION_1_3,
    applicationName: "BYTRCA",
    applicationVersion: VK_MAKE_VERSION(
        major: 0U,
        minor: 0U,
        patch: 0U
    ),
    engineName: "BYTRCE",
    engineVersion: VK_MAKE_VERSION(
        major: 0U,
        minor: 0U,
        patch: 0U
    ),
    pAllocator: vulkanAllocationCallbacksPointer,
    requestedExtensionNames: [
        "VK_EXT_direct_mode_display",
        "VK_EXT_headless_surface",
        "VK_EXT_surface_maintenance1",
        "VK_EXT_swapchain_colorspace",
        "VK_KHR_android_surface",
        "VK_KHR_display",
        "VK_KHR_get_surface_capabilities2",
        "VK_KHR_surface",
        "VK_KHR_wayland_surface",
        "VK_KHR_win32_surface",
        "VK_KHR_xcb_surface",
        "VK_KHR_xlib_surface",
        "VK_NN_vi_surface",
    ],
    requestedLayerNames: [
#if DEBUG
        "VK_LAYER_KHRONOS_profiles",
        "VK_LAYER_KHRONOS_shader_object",
        "VK_LAYER_KHRONOS_validation",
        "VK_LAYER_LUNARG_api_dump",
        "VK_LAYER_LUNARG_screenshot",
#endif
    ],
    result: out vulkanResult
);

var vulkanPhysicalDevice = vulkanInstanceHandle.GetDefaultPhysicalGraphicsDeviceQueue(
    preferredDeviceType: VkPhysicalDeviceType.VK_PHYSICAL_DEVICE_TYPE_DISCRETE_GPU,
    queueFamilyIndex: out var vulkanPhysicalGraphicsDeviceQueueFamilyIndex
);

using var vulkanLogicalGraphicsDeviceHandle = vulkanPhysicalDevice.GetDefaultLogicalGraphicsDeviceQueue(
    pAllocator: vulkanAllocationCallbacksPointer,
    queue: out var vulkanLogicalDeviceQueue,
    queueFamilyIndex: vulkanPhysicalGraphicsDeviceQueueFamilyIndex,
    requestedExtensionNames: [
#if DEBUG
        "VK_KHR_portability_subset",
#endif
        "VK_KHR_swapchain",
    ]
);

static uint GetDisplayType() {
    var displayType = uint.MaxValue;
    var xdgSessionType = Environment.GetEnvironmentVariable(variable: "XDG_SESSION_TYPE");

    if (OperatingSystem.IsLinux()) {
        if (("wayland" == xdgSessionType) || !string.IsNullOrEmpty(value: Environment.GetEnvironmentVariable(variable: "WAYLAND_DISPLAY"))) {
            displayType = 0U;
        }
        else if (("x11" == xdgSessionType) || !string.IsNullOrEmpty(value: Environment.GetEnvironmentVariable(variable: "DISPLAY"))) {
            using var xcbConnection = SafeXcbConnectionHandle.Create();

            displayType = (xcbConnection.IsInvalid ? 2U : 1U);
        }
    }
    else if (OperatingSystem.IsWindows()) {
        displayType = 3U;
    }

    return displayType;
}

var dependency0Handle = ((SafeHandle?)null);
var dependency1Handle = ((SafeHandle?)null);
var displayType = GetDisplayType();
var processBaseAddress = Process.GetCurrentProcess().MainModule!.BaseAddress;
var vulkanImageExtent = new VkExtent2D {
    height = 600,
    width = 800,
};
var vulkanImageOffset = new VkOffset2D {
    x = 0,
    y = 0,
};
var vulkanSurfaceHandle = ((SafeHandle?)null);
var windowHandle = ((SafeHandle?)null);

// TODO: Refactor Wayland nonsense...
var wlCompositor = ((WlCompositor?)null);
var wlDisplay = ((WlDisplay?)null);
var wlSurface = ((WlSurface?)null);
var wlRegistry = ((WlRegistry?)null);
var xdgShell = ((XdgWmBase?)null);
var xdgSurface = ((XdgSurface?)null);
var xdgToplevel = ((XdgToplevel?)null);

try {
    switch (displayType) {
        case 0U:
            unsafe {
                wlDisplay = WlDisplay.Connect();
                wlRegistry = wlDisplay.GetRegistry();

                wlRegistry.Global += (_, args) => {
                    if ("wl_compositor" == args.Interface) {
                        wlCompositor = wlRegistry.Bind<WlCompositor>(
                            args.Name,
                            args.Interface,
                            args.Version
                        );
                        wlSurface = wlCompositor.CreateSurface();
                    }
                    if ("xdg_wm_base" == args.Interface) {
                        xdgShell = wlRegistry.Bind<XdgWmBase>(
                            args.Name,
                            args.Interface,
                            args.Version
                        );
                    }
                };
                wlRegistry.GlobalRemove += (_, args) => { };

                _ = wlDisplay.Roundtrip();

                xdgSurface = xdgShell!.GetXdgSurface(surface: wlSurface!);
                xdgToplevel = xdgSurface.GetToplevel();

                xdgToplevel.SetAppId("BYTRCWLA");
                xdgToplevel.SetTitle("BYTRCWLT");
                wlSurface!.Commit();

                _ = wlDisplay.Dispatch();

                vulkanSurfaceHandle = vulkanInstanceHandle.CreateSurface(
                    display: ((void*)wlDisplay.RawPointer),
                    pAllocator: vulkanAllocationCallbacksPointer,
                    result: out vulkanResult,
                    surface: ((void*)wlSurface.RawPointer)
                );
            }
            break;
        case 1U:
            unsafe {
                dependency1Handle = SafeXcbConnectionHandle.Create();

                var xcbConnection = ((XcbConnection)dependency1Handle.DangerousGetHandle());
                var xcbSetupPointer = Interop.GetSetup(connection: xcbConnection);
                var xcbScreenIterator = Interop.SetupRootsIterator(xcbSetupPointer);
                var xcbScreen = *xcbScreenIterator.data;

                windowHandle = SafeXcbWindowHandle.Create(
                    borderWidth: 0,
                    connectionHandle: ((SafeXcbConnectionHandle)dependency1Handle),
                    depth: 0,
                    height: ((ushort)vulkanImageExtent.height),
                    screen: xcbScreen,
                    width: ((ushort)vulkanImageExtent.width),
                    x: ((short)vulkanImageOffset.x),
                    y: ((short)vulkanImageOffset.y)
                );

                var windowId = ((uint)windowHandle.DangerousGetHandle());

                _ = Interop.MapWindow(
                    connection: xcbConnection,
                    window: windowId
                );
                _ = Interop.Flush(connection: xcbConnection);

                vulkanResult = ((VK_TRUE == vulkanInstanceHandle.GetPhysicalDeviceXcbPresentationSupport(
                    connection: ((void*)(IntPtr)xcbConnection),
                    physicalDevice: vulkanPhysicalDevice,
                    queueFamilyIndex: vulkanPhysicalGraphicsDeviceQueueFamilyIndex,
                    visualId: xcbScreen.root_visual
                ) ? VkResult.VK_SUCCESS : VkResult.VK_ERROR_FORMAT_NOT_SUPPORTED));

                if (VkResult.VK_SUCCESS == vulkanResult) {
                    vulkanSurfaceHandle = vulkanInstanceHandle.CreateXcbSurface(
                        connection: ((void*)(IntPtr)xcbConnection),
                        pAllocator: vulkanAllocationCallbacksPointer,
                        result: out vulkanResult,
                        window: windowId
                    );
                }
            }
            break;
        case 2U:
            unsafe {
                dependency0Handle = SafeX11DisplayHandle.Create();
                windowHandle = SafeX11WindowHandle.Create(
                    background: nuint.Zero,
                    border: nuint.Zero,
                    borderWidth: 0U,
                    displayHandle: ((SafeX11DisplayHandle)dependency0Handle),
                    height: vulkanImageExtent.height,
                    parent: Xlib.DefaultRootWindow(dpy: ((Display*)dependency0Handle.DangerousGetHandle())),
                    width: vulkanImageExtent.width,
                    x: vulkanImageOffset.x,
                    y: vulkanImageOffset.y
                );

                unsafe {
                    var x11Display = ((Display*)dependency0Handle.DangerousGetHandle());
                    var x11Window = ((Window)windowHandle.DangerousGetHandle());

                    XWindowAttributes x11WindowAttributes;

                    _ = Xlib.XMapWindow(
                        param0: x11Display,
                        param1: x11Window
                    );
                    _ = Xlib.XFlush(param0: x11Display);
                    _ = Xlib.XGetWindowAttributes(
                        param0: x11Display,
                        param1: x11Window,
                        param2: &x11WindowAttributes
                    );

                    vulkanResult = ((VK_TRUE == vulkanInstanceHandle.GetPhysicalDeviceXlibPresentationSupport(
                        displayHandle: ((void*)(IntPtr)x11Display),
                        physicalDevice: vulkanPhysicalDevice,
                        queueFamilyIndex: vulkanPhysicalGraphicsDeviceQueueFamilyIndex,
                        visualId: (*x11WindowAttributes.visual).visualid
                    ) ? VkResult.VK_SUCCESS : VkResult.VK_ERROR_FORMAT_NOT_SUPPORTED));

                    if (VkResult.VK_SUCCESS == vulkanResult) {
                        vulkanSurfaceHandle = vulkanInstanceHandle.CreateXlibSurface(
                            pAllocator: vulkanAllocationCallbacksPointer,
                            result: out vulkanResult,
                            display: x11Display,
                            window: x11Window
                        );
                    }
                }
            }
            break;
        case 3U:
            dependency1Handle = SafeUnmanagedMemoryHandle.Create(
                encoding: Encoding.Unicode,
                value: "HELLO_TRIANGLE"
            );
            dependency0Handle = SafeWin32WindowClassHandle.Create(
                classNameHandle: ((SafeUnmanagedMemoryHandle)dependency1Handle),
                hInstance: processBaseAddress
            );
            windowHandle = SafeWin32WindowHandle.Create(
                classHandle: ((SafeWin32WindowClassHandle)dependency0Handle),
                extendedStyle: WINDOW_EX_STYLE.WS_EX_LEFT,
                height: ((int)vulkanImageExtent.height),
                hInstance: processBaseAddress,
                style: WINDOW_STYLE.WS_OVERLAPPED,
                width: ((int)vulkanImageExtent.width),
                windowName: "HELLO_TRIANGLE",
                x: vulkanImageOffset.x,
                y: vulkanImageOffset.y
            );

            vulkanResult = ((VK_TRUE == vulkanInstanceHandle.GetPhysicalDeviceWin32PresentationSupport(
                physicalDevice: vulkanPhysicalDevice,
                queueFamilyIndex: vulkanPhysicalGraphicsDeviceQueueFamilyIndex
            ) ? VkResult.VK_SUCCESS : VkResult.VK_ERROR_FORMAT_NOT_SUPPORTED));

            if (VkResult.VK_SUCCESS == vulkanResult) {
                unsafe {
                    vulkanSurfaceHandle = vulkanInstanceHandle.CreateWin32Surface(
                        hinstance: ((void*)processBaseAddress),
                        hwnd: ((void*)windowHandle.DangerousGetHandle()),
                        pAllocator: vulkanAllocationCallbacksPointer,
                        result: out vulkanResult
                    );
                }
            }
            break;
        default:
            vulkanSurfaceHandle = vulkanInstanceHandle.CreateSurface(
                pAllocator: vulkanAllocationCallbacksPointer,
                result: out vulkanResult
            );
            break;
    }

    if (VkResult.VK_SUCCESS != vulkanResult) { return; }

    var vulkanDevice = ((VkDevice)vulkanLogicalGraphicsDeviceHandle.DangerousGetHandle());
    var vulkanSurface = ((VkSurfaceKHR)vulkanSurfaceHandle!.DangerousGetHandle());

    VkSurfaceCapabilitiesKHR vulkanSurfaceCapabilities;

    unsafe {
        vulkanResult = vkGetPhysicalDeviceSurfaceCapabilitiesKHR(
            physicalDevice: vulkanPhysicalDevice,
            pSurfaceCapabilities: &vulkanSurfaceCapabilities,
            surface: vulkanSurface
        );

        if (VkResult.VK_SUCCESS != vulkanResult) { return; }

        if (((uint.MinValue < vulkanSurfaceCapabilities.currentExtent.height)
          && (uint.MaxValue > vulkanSurfaceCapabilities.currentExtent.height))
         || ((uint.MinValue < vulkanSurfaceCapabilities.currentExtent.width)
          && (uint.MaxValue > vulkanSurfaceCapabilities.currentExtent.width))
        ) {
            vulkanImageExtent = vulkanSurfaceCapabilities.currentExtent;
        }
    }

    var vulkanImageColorSpace = VkColorSpaceKHR.VK_COLOR_SPACE_SRGB_NONLINEAR_KHR;
    var vulkanImageCount = vulkanSurfaceCapabilities.minImageCount;
    var vulkanImageFormat = VkFormat.VK_FORMAT_B8G8R8A8_SRGB;
    var vulkanImageScissor = new VkRect2D {
        extent = vulkanImageExtent,
        offset = vulkanImageOffset,
    };
    var vulkanImageUsageFlags = VkImageUsageFlags.VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT;
    var vulkanImageViewport = new VkViewport {
        height = vulkanImageExtent.height,
        maxDepth = 1.0f,
        minDepth = 0.0f,
        width = vulkanImageExtent.width,
        x = 0.0f,
        y = 0.0f,
    };
    var vulkanPresentMode = VkPresentModeKHR.VK_PRESENT_MODE_IMMEDIATE_KHR;

    vulkanResult &= vulkanPhysicalDevice.IsImageFormatSupported(
        imageFormat: vulkanImageFormat,
        imageUsageFlags: vulkanImageUsageFlags
    );
    vulkanResult &= vulkanPhysicalDevice.IsSurfaceFormatSupported(
        imageColorSpace: vulkanImageColorSpace,
        imageFormat: vulkanImageFormat,
        surface: vulkanSurface
    );
    vulkanResult &= vulkanPhysicalDevice.IsSurfacePresentModeSupported(
        presentMode: vulkanPresentMode,
        surface: vulkanSurface
    );

    if (VkResult.VK_SUCCESS != vulkanResult) { return; }

    using var vulkanSwapchainHandle = vulkanLogicalGraphicsDeviceHandle.CreateHandle(
        createInfo: new VkSwapchainCreateInfoKHR {
            clipped = VK_TRUE,
            compositeAlpha = VkCompositeAlphaFlagsKHR.VK_COMPOSITE_ALPHA_OPAQUE_BIT_KHR,
            flags = uint.MinValue,
            imageArrayLayers = 1U,
            imageColorSpace = vulkanImageColorSpace,
            imageExtent = vulkanImageExtent,
            imageFormat = vulkanImageFormat,
            imageSharingMode = VkSharingMode.VK_SHARING_MODE_EXCLUSIVE,
            imageUsage = vulkanImageUsageFlags,
            minImageCount = vulkanImageCount,
            oldSwapchain = VK_NULL_HANDLE,
            pNext = null,
            pQueueFamilyIndices = null,
            presentMode = vulkanPresentMode,
            preTransform = vulkanSurfaceCapabilities.currentTransform,
            queueFamilyIndexCount = uint.MinValue,
            sType = VkStructureType.VK_STRUCTURE_TYPE_SWAPCHAIN_CREATE_INFO_KHR,
            surface = vulkanSurface,
        },
        pAllocator: vulkanAllocationCallbacksPointer,
        result: out vulkanResult
    );

    if (VkResult.VK_SUCCESS != vulkanResult) { return; }

    var vulkanSwapchain = ((VkSwapchainKHR)vulkanSwapchainHandle.DangerousGetHandle());

    using var vulkanSwapchainImagesHandle = vulkanDevice.GetSwapchainImages(
        count: out var vulkanSwapchainImageCount,
        result: out vulkanResult,
        swapchain: vulkanSwapchain
    );

    if (VkResult.VK_SUCCESS != vulkanResult) { return; }

    using var vulkanFenceInFlightHandle = vulkanLogicalGraphicsDeviceHandle.CreateHandle(
        createInfo: new VkFenceCreateInfo {
            flags = VkFenceCreateFlags.VK_FENCE_CREATE_SIGNALED_BIT,
            pNext = null,
            sType = VkStructureType.VK_STRUCTURE_TYPE_FENCE_CREATE_INFO,
        },
        pAllocator: vulkanAllocationCallbacksPointer,
        result: out vulkanResult
    );

    if (VkResult.VK_SUCCESS != vulkanResult) { return; }

    using var vulkanSemaphoreImageAvailableHandle = vulkanLogicalGraphicsDeviceHandle.CreateHandle(
        createInfo: new VkSemaphoreCreateInfo {
            flags = uint.MinValue,
            pNext = null,
            sType = VkStructureType.VK_STRUCTURE_TYPE_SEMAPHORE_CREATE_INFO,
        },
        pAllocator: vulkanAllocationCallbacksPointer,
        result: out vulkanResult
    );

    if (VkResult.VK_SUCCESS != vulkanResult) { return; }

    using var vulkanSemaphoreRenderFinishedHandle = vulkanLogicalGraphicsDeviceHandle.CreateHandle(
        createInfo: new VkSemaphoreCreateInfo {
            flags = uint.MinValue,
            pNext = null,
            sType = VkStructureType.VK_STRUCTURE_TYPE_SEMAPHORE_CREATE_INFO,
        },
        pAllocator: vulkanAllocationCallbacksPointer,
        result: out vulkanResult
    );

    if (VkResult.VK_SUCCESS != vulkanResult) { return; }

    unsafe {
        var vulkanColorAttachmentDescription = new VkAttachmentDescription2 {
            initialLayout = VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
            finalLayout = VkImageLayout.VK_IMAGE_LAYOUT_PRESENT_SRC_KHR,
            flags = uint.MinValue,
            format = vulkanImageFormat,
            loadOp = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_CLEAR,
            pNext = null,
            samples = VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT,
            stencilLoadOp = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_DONT_CARE,
            stencilStoreOp = VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_DONT_CARE,
            storeOp = VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_STORE,
            sType = VkStructureType.VK_STRUCTURE_TYPE_ATTACHMENT_DESCRIPTION_2,
        };
        var vulkanColorAttachmentReference = new VkAttachmentReference2 {
            aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_NONE,
            attachment = uint.MinValue,
            layout = VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL,
            pNext = null,
            sType = VkStructureType.VK_STRUCTURE_TYPE_ATTACHMENT_REFERENCE_2,
        };
        var vulkanSubpassDependency = new VkSubpassDependency2 {
            dependencyFlags = uint.MinValue,
            dstAccessMask = (VkAccessFlags.VK_ACCESS_COLOR_ATTACHMENT_READ_BIT | VkAccessFlags.VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT),
            dstStageMask = VkPipelineStageFlags.VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT,
            dstSubpass = uint.MinValue,
            srcAccessMask = uint.MinValue,
            pNext = null,
            srcStageMask = VkPipelineStageFlags.VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT,
            srcSubpass = uint.MaxValue,
            sType = VkStructureType.VK_STRUCTURE_TYPE_SUBPASS_DEPENDENCY_2,
            viewOffset = 0,
        };
        var vulkanSubpassDescription = new VkSubpassDescription2 {
            colorAttachmentCount = 1U,
            inputAttachmentCount = uint.MinValue,
            flags = uint.MinValue,
            pColorAttachments = &vulkanColorAttachmentReference,
            pDepthStencilAttachment = null,
            pInputAttachments = null,
            pipelineBindPoint = VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS,
            pNext = null,
            pPreserveAttachments = null,
            preserveAttachmentCount = uint.MinValue,
            pResolveAttachments = null,
            sType = VkStructureType.VK_STRUCTURE_TYPE_SUBPASS_DESCRIPTION_2,
            viewMask = uint.MinValue,
        };

        using var vulkanRenderPassHandle = vulkanLogicalGraphicsDeviceHandle.CreateHandle(
            createInfo: new VkRenderPassCreateInfo2 {
                attachmentCount = 1U,
                correlatedViewMaskCount = uint.MinValue,
                dependencyCount = 1U,
                flags = uint.MinValue,
                pAttachments = &vulkanColorAttachmentDescription,
                pCorrelatedViewMasks = null,
                pDependencies = &vulkanSubpassDependency,
                pNext = null,
                pSubpasses = &vulkanSubpassDescription,
                sType = VkStructureType.VK_STRUCTURE_TYPE_RENDER_PASS_CREATE_INFO_2,
                subpassCount = 1U,
            },
            pAllocator: vulkanAllocationCallbacksPointer,
            result: out vulkanResult
        );

        var vulkanRenderPass = ((VkRenderPass)vulkanRenderPassHandle.DangerousGetHandle());
        var vulkanSwapchainImagesPointer = ((VkImage*)vulkanSwapchainImagesHandle.DangerousGetHandle());

        using var vulkanImageViewPrimaryHandle = vulkanLogicalGraphicsDeviceHandle.CreateHandle(
            createInfo: new VkImageViewCreateInfo {
                components = new() {
                    a = VkComponentSwizzle.VK_COMPONENT_SWIZZLE_IDENTITY,
                    b = VkComponentSwizzle.VK_COMPONENT_SWIZZLE_IDENTITY,
                    g = VkComponentSwizzle.VK_COMPONENT_SWIZZLE_IDENTITY,
                    r = VkComponentSwizzle.VK_COMPONENT_SWIZZLE_IDENTITY,
                },
                flags = uint.MinValue,
                format = vulkanImageFormat,
                image = vulkanSwapchainImagesPointer[0],
                pNext = null,
                sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO,
                subresourceRange = new() {
                    aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT,
                    baseArrayLayer = uint.MinValue,
                    baseMipLevel = uint.MinValue,
                    layerCount = 1U,
                    levelCount = 1U,
                },
                viewType = VkImageViewType.VK_IMAGE_VIEW_TYPE_2D,
            },
            pAllocator: vulkanAllocationCallbacksPointer,
            result: out vulkanResult
        );
        using var vulkanImageViewSecondaryHandle = vulkanLogicalGraphicsDeviceHandle.CreateHandle(
            createInfo: new VkImageViewCreateInfo {
                components = new() {
                    a = VkComponentSwizzle.VK_COMPONENT_SWIZZLE_IDENTITY,
                    b = VkComponentSwizzle.VK_COMPONENT_SWIZZLE_IDENTITY,
                    g = VkComponentSwizzle.VK_COMPONENT_SWIZZLE_IDENTITY,
                    r = VkComponentSwizzle.VK_COMPONENT_SWIZZLE_IDENTITY,
                },
                flags = uint.MinValue,
                format = vulkanImageFormat,
                image = vulkanSwapchainImagesPointer[1],
                pNext = null,
                sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO,
                subresourceRange = new() {
                    aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT,
                    baseArrayLayer = uint.MinValue,
                    baseMipLevel = uint.MinValue,
                    layerCount = 1U,
                    levelCount = 1U,
                },
                viewType = VkImageViewType.VK_IMAGE_VIEW_TYPE_2D,
            },
            pAllocator: vulkanAllocationCallbacksPointer,
            result: out vulkanResult
        );

        var vulkanImageViewPrimary = ((VkImageView)vulkanImageViewPrimaryHandle.DangerousGetHandle());
        var vulkanImageViewSecondary = ((VkImageView)vulkanImageViewSecondaryHandle.DangerousGetHandle());

        using var vulkanFrameBufferPrimary = vulkanLogicalGraphicsDeviceHandle.CreateHandle(
            createInfo: new VkFramebufferCreateInfo {
                attachmentCount = 1U,
                flags = uint.MinValue,
                height = vulkanImageExtent.height,
                layers = 1U,
                pAttachments = &vulkanImageViewPrimary,
                pNext = null,
                renderPass = vulkanRenderPass,
                sType = VkStructureType.VK_STRUCTURE_TYPE_FRAMEBUFFER_CREATE_INFO,
                width = vulkanImageExtent.width,
            },
            pAllocator: vulkanAllocationCallbacksPointer,
            result: out vulkanResult
        );
        using var vulkanFrameBufferSecondary = vulkanLogicalGraphicsDeviceHandle.CreateHandle(
            createInfo: new VkFramebufferCreateInfo {
                attachmentCount = 1U,
                flags = uint.MinValue,
                height = vulkanImageExtent.height,
                layers = 1U,
                pAttachments = &vulkanImageViewPrimary,
                pNext = null,
                renderPass = vulkanRenderPass,
                sType = VkStructureType.VK_STRUCTURE_TYPE_FRAMEBUFFER_CREATE_INFO,
                width = vulkanImageExtent.width,
            },
            pAllocator: vulkanAllocationCallbacksPointer,
            result: out vulkanResult
        );

        using var shaderFragFileStream = new FileStream(
            options: new() {
                Access = FileAccess.Read,
                Mode = FileMode.Open,
                Options = FileOptions.RandomAccess,
                Share = FileShare.Read,
            },
            path: "Shaders/frag.spv"
        );
        using var shaderFragMemoryMappedFile = MemoryMappedFile.CreateFromFile(
            access: MemoryMappedFileAccess.Read,
            capacity: 0,
            fileStream: shaderFragFileStream,
            inheritability: HandleInheritability.None,
            leaveOpen: true,
            mapName: null
        );
        using var shaderFragMemoryMappedViewAccessor = shaderFragMemoryMappedFile.CreateViewAccessor(
            access: MemoryMappedFileAccess.Read,
            offset: 0,
            size: shaderFragFileStream.Length
        );
        using var shaderVertFileStream = new FileStream(
            options: new() {
                Access = FileAccess.Read,
                Mode = FileMode.Open,
                Options = FileOptions.RandomAccess,
                Share = FileShare.Read,
            },
            path: "Shaders/vert.spv"
        );
        using var shaderVertMemoryMappedFile = MemoryMappedFile.CreateFromFile(
            access: MemoryMappedFileAccess.Read,
            capacity: 0,
            fileStream: shaderVertFileStream,
            inheritability: HandleInheritability.None,
            leaveOpen: true,
            mapName: null
        );
        using var shaderVertMemoryMappedViewAccessor = shaderVertMemoryMappedFile.CreateViewAccessor(
            access: MemoryMappedFileAccess.Read,
            offset: 0,
            size: shaderVertFileStream.Length
        );

        byte* shaderFragPointer = null;
        byte* shaderVertPointer = null;

        shaderFragMemoryMappedViewAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref shaderFragPointer);
        shaderVertMemoryMappedViewAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref shaderVertPointer);

        using var vulkanShaderModuleFragHandle = vulkanLogicalGraphicsDeviceHandle.CreateHandle(
            createInfo: new VkShaderModuleCreateInfo {
                codeSize = ((uint)shaderFragMemoryMappedViewAccessor.Capacity),
                flags = uint.MinValue,
                pCode = ((uint*)shaderFragPointer),
                pNext = null,
                sType = VkStructureType.VK_STRUCTURE_TYPE_SHADER_MODULE_CREATE_INFO,
            },
            pAllocator: vulkanAllocationCallbacksPointer,
            result: out vulkanResult
        );
        using var vulkanShaderModuleVertHandle = vulkanLogicalGraphicsDeviceHandle.CreateHandle(
            createInfo: new VkShaderModuleCreateInfo {
                codeSize = ((uint)shaderVertMemoryMappedViewAccessor.Capacity),
                flags = uint.MinValue,
                pCode = ((uint*)shaderVertPointer),
                pNext = null,
                sType = VkStructureType.VK_STRUCTURE_TYPE_SHADER_MODULE_CREATE_INFO,
            },
            pAllocator: vulkanAllocationCallbacksPointer,
            result: out vulkanResult
        );
        using var vulkanPipelineCacheHandle = vulkanLogicalGraphicsDeviceHandle.CreateHandle(
            createInfo: new VkPipelineCacheCreateInfo {
                flags = uint.MinValue,
                initialDataSize = uint.MinValue,
                pInitialData = null,
                pNext = null,
                sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_CACHE_CREATE_INFO,
            },
            pAllocator: vulkanAllocationCallbacksPointer,
            result: out vulkanResult
        );
        using var vulkanPipelineLayoutHandle = vulkanLogicalGraphicsDeviceHandle.CreateHandle(
            createInfo: new VkPipelineLayoutCreateInfo {
                flags = uint.MinValue,
                pNext = null,
                pPushConstantRanges = null,
                pSetLayouts = null,
                pushConstantRangeCount = uint.MinValue,
                setLayoutCount = uint.MinValue,
                sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_LAYOUT_CREATE_INFO,
            },
            pAllocator: vulkanAllocationCallbacksPointer,
            result: out vulkanResult
        );

        var vulkanPipelineColorBlendAttachmentState = new VkPipelineColorBlendAttachmentState {
            alphaBlendOp = VkBlendOp.VK_BLEND_OP_ADD,
            blendEnable = VK_FALSE,
            colorBlendOp = VkBlendOp.VK_BLEND_OP_ADD,
            colorWriteMask = (
                VkColorComponentFlags.VK_COLOR_COMPONENT_A_BIT
              | VkColorComponentFlags.VK_COLOR_COMPONENT_B_BIT
              | VkColorComponentFlags.VK_COLOR_COMPONENT_G_BIT
              | VkColorComponentFlags.VK_COLOR_COMPONENT_R_BIT
            ),
            dstAlphaBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ZERO,
            dstColorBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ZERO,
            srcAlphaBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ONE,
            srcColorBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ONE,
        };
        var vulkanPipelineColorBlendStateCreateInfo = new VkPipelineColorBlendStateCreateInfo {
            attachmentCount = 1U,
            flags = uint.MinValue,
            logicOp = VkLogicOp.VK_LOGIC_OP_COPY,
            logicOpEnable = VK_FALSE,
            pAttachments = &vulkanPipelineColorBlendAttachmentState,
            pNext = null,
            sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_COLOR_BLEND_STATE_CREATE_INFO,
        };

        vulkanPipelineColorBlendStateCreateInfo.blendConstants[0] = 0.0f;
        vulkanPipelineColorBlendStateCreateInfo.blendConstants[1] = 0.0f;
        vulkanPipelineColorBlendStateCreateInfo.blendConstants[2] = 0.0f;
        vulkanPipelineColorBlendStateCreateInfo.blendConstants[3] = 0.0f;

        var vulkanPipelineDepthStencilStateCreateInfo = new VkPipelineDepthStencilStateCreateInfo {
            flags = uint.MinValue,
            pNext = null,
            sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_DEPTH_STENCIL_STATE_CREATE_INFO,
        };
        var vulkanPipelineDynamicStates = stackalloc[] {
            VkDynamicState.VK_DYNAMIC_STATE_SCISSOR,
            VkDynamicState.VK_DYNAMIC_STATE_VIEWPORT,
        };
        var vulkanPipelineDynamicStateCreateInfo = new VkPipelineDynamicStateCreateInfo {
            dynamicStateCount = 2U,
            flags = uint.MinValue,
            pDynamicStates = vulkanPipelineDynamicStates,
            pNext = null,
            sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_DYNAMIC_STATE_CREATE_INFO,
        };
        var vulkanPipelineInputAssemblyStateCreateInfo = new VkPipelineInputAssemblyStateCreateInfo {
            flags = uint.MinValue,
            pNext = null,
            primitiveRestartEnable = VK_FALSE,
            sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_INPUT_ASSEMBLY_STATE_CREATE_INFO,
            topology = VkPrimitiveTopology.VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST,
        };
        var vulkanPipelineMultisampleStateCreateInfo = new VkPipelineMultisampleStateCreateInfo {
            alphaToCoverageEnable = VK_FALSE,
            alphaToOneEnable = VK_FALSE,
            flags = uint.MinValue,
            minSampleShading = 1.0f,
            pNext = null,
            pSampleMask = null,
            rasterizationSamples = VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT,
            sampleShadingEnable = VK_FALSE,
            sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_MULTISAMPLE_STATE_CREATE_INFO,
        };
        var vulkanPipelineRasterizationStateCreateInfo = new VkPipelineRasterizationStateCreateInfo {
            cullMode = VkCullModeFlags.VK_CULL_MODE_BACK_BIT,
            depthBiasClamp = 0.0f,
            depthBiasConstantFactor = 0.0f,
            depthBiasEnable = VK_FALSE,
            depthBiasSlopeFactor = 0.0f,
            depthClampEnable = VK_FALSE,
            flags = uint.MinValue,
            frontFace = VkFrontFace.VK_FRONT_FACE_CLOCKWISE,
            lineWidth = 1.0f,
            pNext = null,
            polygonMode = VkPolygonMode.VK_POLYGON_MODE_FILL,
            rasterizerDiscardEnable = VK_FALSE,
            sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_RASTERIZATION_STATE_CREATE_INFO,
        };
        var vulkanPipelineShaderStages = stackalloc[] {
            new VkPipelineShaderStageCreateInfo {
                flags = uint.MinValue,
                module = ((VkShaderModule)vulkanShaderModuleFragHandle.DangerousGetHandle()),
                pName = ((sbyte*)Unsafe.AsPointer(value: ref MemoryMarshal.GetReference(span: "main\u0000"u8))),
                pNext = null,
                pSpecializationInfo = null,
                stage = VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT,
                sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,
            },
            new VkPipelineShaderStageCreateInfo {
                flags = uint.MinValue,
                module = ((VkShaderModule)vulkanShaderModuleVertHandle.DangerousGetHandle()),
                pName = ((sbyte*)Unsafe.AsPointer(value: ref MemoryMarshal.GetReference(span: "main\u0000"u8))),
                pNext = null,
                pSpecializationInfo = null,
                stage = VkShaderStageFlags.VK_SHADER_STAGE_VERTEX_BIT,
                sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,
            },
        };
        var vulkanPipelineTessellationStateCreateInfo = new VkPipelineTessellationStateCreateInfo {
            flags = uint.MinValue,
            patchControlPoints = 1U,
            pNext = null,
            sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_TESSELLATION_STATE_CREATE_INFO,
        };
        var vulkanPipelineVertexInputStateCreateInfo = new VkPipelineVertexInputStateCreateInfo {
            flags = uint.MinValue,
            pNext = null,
            pVertexAttributeDescriptions = null,
            pVertexBindingDescriptions = null,
            sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_VERTEX_INPUT_STATE_CREATE_INFO,
            vertexAttributeDescriptionCount = uint.MinValue,
            vertexBindingDescriptionCount = uint.MinValue,
        };
        var vulkanPipelineViewportStateCreateInfo = new VkPipelineViewportStateCreateInfo {
            flags = uint.MinValue,
            pNext = null,
            pScissors = &vulkanImageScissor,
            pViewports = &vulkanImageViewport,
            scissorCount = 1U,
            sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_VIEWPORT_STATE_CREATE_INFO,
            viewportCount = 1U,
        };

        using var vulkanGraphicsPipeline = SafeVulkanPipelineHandle.Create(
            createInfo: new VkGraphicsPipelineCreateInfo {
                basePipelineHandle = VK_NULL_HANDLE,
                basePipelineIndex = -1,
                flags = uint.MinValue,
                layout = ((VkPipelineLayout)vulkanPipelineLayoutHandle.DangerousGetHandle()),
                pColorBlendState = &vulkanPipelineColorBlendStateCreateInfo,
                pDepthStencilState = &vulkanPipelineDepthStencilStateCreateInfo,
                pDynamicState = &vulkanPipelineDynamicStateCreateInfo,
                pInputAssemblyState = &vulkanPipelineInputAssemblyStateCreateInfo,
                pMultisampleState = &vulkanPipelineMultisampleStateCreateInfo,
                pNext = null,
                pRasterizationState = &vulkanPipelineRasterizationStateCreateInfo,
                pStages = vulkanPipelineShaderStages,
                pTessellationState = &vulkanPipelineTessellationStateCreateInfo,
                pVertexInputState = &vulkanPipelineVertexInputStateCreateInfo,
                pViewportState = &vulkanPipelineViewportStateCreateInfo,
                renderPass = vulkanRenderPass,
                stageCount = 2U,
                sType = VkStructureType.VK_STRUCTURE_TYPE_GRAPHICS_PIPELINE_CREATE_INFO,
                subpass = uint.MinValue,
            },
            logicalDeviceHandle: vulkanLogicalGraphicsDeviceHandle,
            pAllocator: vulkanAllocationCallbacksPointer,
            pipelineCache: ((VkPipelineCache)vulkanPipelineCacheHandle.DangerousGetHandle()),
            result: out vulkanResult
        );
        using var vulkanCommandPoolHandle = vulkanLogicalGraphicsDeviceHandle.CreateHandle(
            createInfo: new VkCommandPoolCreateInfo {
                flags = VkCommandPoolCreateFlags.VK_COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER_BIT,
                pNext = null,
                queueFamilyIndex = vulkanPhysicalGraphicsDeviceQueueFamilyIndex,
                sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO,
            },
            pAllocator: vulkanAllocationCallbacksPointer,
            result: out vulkanResult
        );

        var vulkanCommandPool = ((VkCommandPool)vulkanCommandPoolHandle.DangerousGetHandle());
        var vulkanCommandBufferAllocateInfo = new VkCommandBufferAllocateInfo {
            commandBufferCount = 1U,
            commandPool = vulkanCommandPool,
            level = VkCommandBufferLevel.VK_COMMAND_BUFFER_LEVEL_PRIMARY,
            pNext = null,
            sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO,
        };

        VkCommandBuffer vulkanCommandBuffer;

        if (VkResult.VK_SUCCESS == vkAllocateCommandBuffers(
            device: vulkanDevice,
            pAllocateInfo: &vulkanCommandBufferAllocateInfo,
            pCommandBuffers: &vulkanCommandBuffer
        )) {
            var vulkanFenceInFlight = ((VkFence)vulkanFenceInFlightHandle.DangerousGetHandle());
            var vulkanFrameBuffers = stackalloc[] {
                ((VkFramebuffer)vulkanFrameBufferPrimary.DangerousGetHandle()),
                ((VkFramebuffer)vulkanFrameBufferSecondary.DangerousGetHandle()),
            };
            var vulkanImageIndex = 0U;
            var vulkanSemaphoreImageAvailable = ((VkSemaphore)vulkanSemaphoreImageAvailableHandle.DangerousGetHandle());
            var vulkanSemaphoreRenderFinished = ((VkSemaphore)vulkanSemaphoreRenderFinishedHandle.DangerousGetHandle());

            vulkanResult = vkWaitForFences(
                device: vulkanDevice,
                fenceCount: 1U,
                pFences: &vulkanFenceInFlight,
                timeout: ulong.MaxValue,
                waitAll: VK_TRUE
            );
            vulkanResult = vkResetFences(
                device: vulkanDevice,
                fenceCount: 1U,
                pFences: &vulkanFenceInFlight
            );
            vulkanResult = vkAcquireNextImageKHR(
                device: vulkanDevice,
                fence: VK_NULL_HANDLE,
                pImageIndex: &vulkanImageIndex,
                semaphore: vulkanSemaphoreImageAvailable,
                swapchain: vulkanSwapchain,
                timeout: ulong.MaxValue
            );
            vulkanResult = vkResetCommandBuffer(
                commandBuffer: vulkanCommandBuffer,
                flags: uint.MinValue
            );

            var commandBufferBeginInfo = new VkCommandBufferBeginInfo {
                flags = uint.MinValue,
                pInheritanceInfo = null,
                pNext = null,
                sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO,
            };

            if (VkResult.VK_SUCCESS == vkBeginCommandBuffer(
                commandBuffer: vulkanCommandBuffer,
                pBeginInfo: &commandBufferBeginInfo
            )) {
                var vulkanClearColor = new VkClearValue { };

                vulkanClearColor.color.float32[0] = 0.0f;
                vulkanClearColor.color.float32[1] = 0.0f;
                vulkanClearColor.color.float32[2] = 0.0f;
                vulkanClearColor.color.float32[3] = 1.0f;

                var vulkanRenderPassBeginInfo = new VkRenderPassBeginInfo {
                    clearValueCount = 1U,
                    framebuffer = vulkanFrameBuffers[vulkanImageIndex],
                    pClearValues = &vulkanClearColor,
                    pNext = null,
                    renderArea = new() {
                        extent = vulkanImageExtent,
                        offset = vulkanImageOffset,
                    },
                    renderPass = vulkanRenderPass,
                    sType = VkStructureType.VK_STRUCTURE_TYPE_RENDER_PASS_BEGIN_INFO,
                };
                var vulkanSubpassBeginInfo = new VkSubpassBeginInfo {
                    contents = VkSubpassContents.VK_SUBPASS_CONTENTS_INLINE,
                    pNext = null,
                    sType = VkStructureType.VK_STRUCTURE_TYPE_SUBPASS_BEGIN_INFO,
                };

                vkCmdBeginRenderPass2(
                    commandBuffer: vulkanCommandBuffer,
                    pRenderPassBegin: &vulkanRenderPassBeginInfo,
                    pSubpassBeginInfo: &vulkanSubpassBeginInfo
                );
                vkCmdBindPipeline(
                    commandBuffer: vulkanCommandBuffer,
                    pipeline: ((VkPipeline)vulkanGraphicsPipeline.DangerousGetHandle()),
                    pipelineBindPoint: VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS
                );
                vkCmdSetViewport(
                    commandBuffer: vulkanCommandBuffer,
                    firstViewport: uint.MinValue,
                    pViewports: &vulkanImageViewport,
                    viewportCount: 1U
                );
                vkCmdSetScissor(
                    commandBuffer: vulkanCommandBuffer,
                    firstScissor: uint.MinValue,
                    pScissors: &vulkanImageScissor,
                    scissorCount: 1U
                );
                vkCmdDraw(
                    commandBuffer: vulkanCommandBuffer,
                    instanceCount: 1,
                    firstInstance: uint.MinValue,
                    firstVertex: uint.MinValue,
                    vertexCount: 3
                );
                vkCmdEndRenderPass(commandBuffer: vulkanCommandBuffer);

                if (VkResult.VK_SUCCESS == vkEndCommandBuffer(commandBuffer: vulkanCommandBuffer)) {
                    var vulkanCommandBufferSubmitInfo = new VkCommandBufferSubmitInfo {
                        commandBuffer = vulkanCommandBuffer,
                        deviceMask = uint.MinValue,
                        pNext = null,
                        sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_SUBMIT_INFO,
                    };
                    var vulkanSignalSemaphoreSubmitInfo = new VkSemaphoreSubmitInfo {
                        deviceIndex = uint.MinValue,
                        pNext = null,
                        semaphore = vulkanSemaphoreRenderFinished,
                        stageMask = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_BOTTOM_OF_PIPE_BIT,
                        sType = VkStructureType.VK_STRUCTURE_TYPE_SEMAPHORE_SUBMIT_INFO,
                        value = ulong.MinValue,
                    };
                    var vulkanWaitSemaphoreSubmitInfo = new VkSemaphoreSubmitInfo {
                        deviceIndex = uint.MinValue,
                        pNext = null,
                        semaphore = vulkanSemaphoreImageAvailable,
                        stageMask = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_COLOR_ATTACHMENT_OUTPUT_BIT,
                        sType = VkStructureType.VK_STRUCTURE_TYPE_SEMAPHORE_SUBMIT_INFO,
                        value = ulong.MinValue,
                    };
                    var vulkanSubmitInfo = new VkSubmitInfo2 {
                        commandBufferInfoCount = 1U,
                        flags = uint.MinValue,
                        pCommandBufferInfos = &vulkanCommandBufferSubmitInfo,
                        pNext = null,
                        pSignalSemaphoreInfos = &vulkanSignalSemaphoreSubmitInfo,
                        pWaitSemaphoreInfos = &vulkanWaitSemaphoreSubmitInfo,
                        signalSemaphoreInfoCount = 1U,
                        sType = VkStructureType.VK_STRUCTURE_TYPE_SUBMIT_INFO_2,
                        waitSemaphoreInfoCount = 1U,
                    };

                    vulkanResult = vkQueueSubmit2(
                        fence: vulkanFenceInFlight,
                        pSubmits: &vulkanSubmitInfo,
                        queue: vulkanLogicalDeviceQueue,
                        submitCount: 1U
                    );

                    var vulkanPresentInfo = new VkPresentInfoKHR {
                        pImageIndices = &vulkanImageIndex,
                        pNext = null,
                        pResults = null,
                        pSwapchains = &vulkanSwapchain,
                        pWaitSemaphores = &vulkanSemaphoreRenderFinished,
                        sType = VkStructureType.VK_STRUCTURE_TYPE_PRESENT_INFO_KHR,
                        swapchainCount = 1U,
                        waitSemaphoreCount = 1U,
                    };

                    vulkanResult = vkQueuePresentKHR(
                        queue: vulkanLogicalDeviceQueue,
                        pPresentInfo: &vulkanPresentInfo
                    );
                    vulkanResult = vkDeviceWaitIdle(device: vulkanDevice);

                    Thread.Sleep(millisecondsTimeout: 3000);
                }

                vkFreeCommandBuffers(
                    commandBufferCount: 1U,
                    commandPool: vulkanCommandPool,
                    device: vulkanDevice,
                    pCommandBuffers: &vulkanCommandBuffer
                );
            }
        }
    }
}
finally {
    vulkanSurfaceHandle?.Dispose();
    windowHandle?.Dispose();
    dependency0Handle?.Dispose();
    dependency1Handle?.Dispose();

    wlDisplay?.Dispose();
    wlRegistry?.Dispose();
    wlCompositor?.Dispose();
    wlSurface?.Dispose();
    xdgToplevel?.Dispose();
    xdgSurface?.Dispose();
    xdgShell?.Dispose();
}
