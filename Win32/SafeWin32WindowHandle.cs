using Microsoft.Win32.SafeHandles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Windows.Win32;

public sealed class SafeWin32WindowHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public unsafe static SafeWin32WindowHandle Create(
        WINDOW_EX_STYLE extendedStyle,
        int height,
        nint hInstance,
        WINDOW_STYLE style,
        int width,
        int x,
        int y,
        string windowName,
        SafeWin32WindowClassHandle classHandle
    ) {
        var windowClassSafeHandle = new SafeWin32WindowHandle(win32WindowClassHandle: classHandle);

        if (OperatingSystem.IsWindowsVersionAtLeast(
            build: 0,
            major: 5,
            minor: 0
        )) {
            var addRefCountSuccess = false;

            classHandle.DangerousAddRef(success: ref addRefCountSuccess);

            if (addRefCountSuccess) {
                var thisHwnd = PInvoke.CreateWindowEx(
                    dwExStyle: extendedStyle,
                    dwStyle: style,
                    hInstance: ((HINSTANCE)hInstance),
                    hMenu: HMENU.Null,
                    hWndParent: HWND.Null,
                    lpClassName: ((char*)classHandle.DangerousGetHandle()),
                    lpParam: null,
                    lpWindowName: ((char*)Unsafe.AsPointer(value: ref MemoryMarshal.GetReference(span: (windowName + '\0').AsSpan()))),
                    nHeight: height,
                    nWidth: width,
                    X: x,
                    Y: y
                );

                if (HWND.Null != thisHwnd) {
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
                    windowClassSafeHandle.SetHandle(handle: thisHwnd);
                }
                else {
                    classHandle.DangerousRelease();
                }
            }
        }

        return windowClassSafeHandle;
    }

    private readonly SafeWin32WindowClassHandle m_win32WindowClassHandle;

    private SafeWin32WindowHandle(SafeWin32WindowClassHandle win32WindowClassHandle) : base(ownsHandle: true) {
        m_win32WindowClassHandle = win32WindowClassHandle;
    }

    protected unsafe override bool ReleaseHandle() {
#pragma warning disable CA1416
        var destroyWindowResult = PInvoke.DestroyWindow(hWnd: (HWND)handle);
#pragma warning restore CA1416

        m_win32WindowClassHandle.DangerousRelease();

        return destroyWindowResult;
    }
}
