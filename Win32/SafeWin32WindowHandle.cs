using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Windows.Win32;

public sealed class SafeWin32WindowHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public unsafe static SafeWin32WindowHandle Create(
        SafeWin32WindowClassHandle @class,
        WINDOW_EX_STYLE extendedStyle,
        int height,
        SafeHandle instance,
        WINDOW_STYLE style,
        int width,
        int x,
        int y,
        string windowName
    ) {
        ArgumentNullException.ThrowIfNull(argument: @class, paramName: nameof(@class));
        ArgumentNullException.ThrowIfNull(argument: instance, paramName: nameof(instance));

        if (!OperatingSystem.IsWindowsVersionAtLeast(build: 0, major: 5, minor: 0)) {
            throw new PlatformNotSupportedException();
        }

        var instanceAddRef = false;

        try {
            instance.DangerousAddRef(success: ref instanceAddRef);

            var classAddRef = false;

            try {
                @class.DangerousAddRef(success: ref classAddRef);

                fixed (char* pWindowName = windowName) {
                    var windowSafeHandle = new SafeWin32WindowHandle(@class: @class);
                    var thisHwnd = PInvoke.CreateWindowEx(
                        dwExStyle: extendedStyle,
                        dwStyle: style,
                        hInstance: ((HINSTANCE)instance.DangerousGetHandle()),
                        hMenu: HMENU.Null,
                        hWndParent: HWND.Null,
                        lpClassName: ((char*)@class.DangerousGetHandle()),
                        lpParam: null,
                        lpWindowName: pWindowName,
                        nHeight: height,
                        nWidth: width,
                        X: x,
                        Y: y
                    );

                    if (HWND.Null == thisHwnd) {
                        throw new ExternalException(message: Marshal.GetLastPInvokeErrorMessage());
                    }
                    else {
                        var consoleHwnd = PInvoke.GetConsoleWindow();

                        if (HWND.Null != consoleHwnd) {
                            PInvoke.ShowWindow(
                                hWnd: consoleHwnd,
                                nCmdShow: SHOW_WINDOW_CMD.SW_HIDE
                            );
                        }

                        PInvoke.ShowWindow(
                            hWnd: thisHwnd,
                            nCmdShow: SHOW_WINDOW_CMD.SW_SHOWNORMAL
                        );
                        windowSafeHandle.SetHandle(handle: thisHwnd);
                    }

                    return windowSafeHandle;
                }
            }
            catch {
                if (classAddRef) {
                    @class.DangerousRelease();
                }

                throw;
            }
        }
        finally {
            if (instanceAddRef) {
                instance.DangerousRelease();
            }
        }
    }

    private readonly SafeWin32WindowClassHandle m_class;

    private SafeWin32WindowHandle(SafeWin32WindowClassHandle @class) : base(ownsHandle: true) {
        m_class = @class;
    }

    protected unsafe override bool ReleaseHandle() {
#pragma warning disable CA1416
        var @class = m_class;
        var result = PInvoke.DestroyWindow(hWnd: ((HWND)handle));
#pragma warning restore CA1416

        if ((@class is not null) && !@class.IsClosed && !@class.IsInvalid) {
            @class.DangerousRelease();
        }

        return result;
    }
}
