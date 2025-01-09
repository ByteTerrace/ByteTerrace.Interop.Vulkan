using ByteTerrace.Interop.Vulkan;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Windows.Win32;

public sealed class SafeWin32WindowClassHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate LRESULT DefWindowProcDelegate(HWND hWnd, uint Msg, WPARAM wParam, LPARAM lParam);

    [SupportedOSPlatform("windows5.0")]
    private static readonly DefWindowProcDelegate DefWindowProc = (hWnd, Msg, wParam, lParam) => {
        return Msg switch {
            _ => PInvoke.DefWindowProc(
                hWnd: hWnd,
                lParam: lParam,
                Msg: Msg,
                wParam: wParam
            ),
        };
    };

    public unsafe static SafeWin32WindowClassHandle Create(
        SafeUnmanagedMemoryHandle classNameHandle,
        nint hInstance
    ) {
        var windowClassSafeHandle = new SafeWin32WindowClassHandle(
            classNameHandle: classNameHandle,
            hInstance: hInstance
        );

        if (OperatingSystem.IsWindowsVersionAtLeast(
            build: 0,
            major: 5,
            minor: 0
        )) {
            var addRefCountSuccess = false;

            classNameHandle.DangerousAddRef(success: ref addRefCountSuccess);

            if (addRefCountSuccess) {
                var windowClassCreateInfo = new WNDCLASSEXW {
                    cbSize = (uint)sizeof(WNDCLASSEXW),
                    hInstance = (HINSTANCE)hInstance,
                    lpfnWndProc = (delegate* unmanaged[Stdcall]<HWND, uint, WPARAM, LPARAM, LRESULT>)Marshal.GetFunctionPointerForDelegate(d: DefWindowProc),
                    lpszClassName = (char*)classNameHandle.DangerousGetHandle(),
                };
                var windowClassHandle = PInvoke.RegisterClassEx(param0: &windowClassCreateInfo);

                if (0 != windowClassHandle) {
                    windowClassSafeHandle.SetHandle(handle: windowClassHandle);
                }
                else {
                    classNameHandle.DangerousRelease();
                }
            }
        }

        return windowClassSafeHandle;
    }

    private readonly SafeUnmanagedMemoryHandle m_classNameHandle;
    private readonly nint m_hInstance;

    private SafeWin32WindowClassHandle(
        SafeUnmanagedMemoryHandle classNameHandle,
        nint hInstance
    ) : base(ownsHandle: true) {
        m_classNameHandle = classNameHandle;
        m_hInstance = hInstance;
    }

    protected unsafe override bool ReleaseHandle() {
#pragma warning disable CA1416
        var unregisterClassResult = PInvoke.UnregisterClass(
            hInstance: (HINSTANCE)m_hInstance,
            lpClassName: (char*)m_classNameHandle.DangerousGetHandle()
        );

        m_classNameHandle.DangerousRelease();

        return unregisterClassResult;
#pragma warning restore CA1416
    }
}

