using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Windows.Win32;

public sealed class SafeWin32WindowClassHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate LRESULT WindowProcedureDelegate(HWND hWnd, uint Msg, WPARAM wParam, LPARAM lParam);

    public unsafe static SafeWin32WindowClassHandle Create(
        string className,
        SafeHandle instance,
        Action<HWND, uint, WPARAM, LPARAM> windowProcedure
    ) {
        ArgumentNullException.ThrowIfNull(argument: instance, paramName: nameof(instance));

        if (string.IsNullOrEmpty(value: className)) {
            throw new ArgumentNullException(paramName: nameof(className));
        }

        if (!OperatingSystem.IsWindowsVersionAtLeast(build: 0, major: 5, minor: 0)) {
            throw new PlatformNotSupportedException();
        }

        var instanceAddRef = false;
        var windowProcedureDelegate = ((WindowProcedureDelegate)((hWnd, Msg, wParam, lParam) => {
            windowProcedure(
                hWnd,
                Msg,
                wParam,
                lParam
            );

#pragma warning disable CA1416
            return PInvoke.DefWindowProc(
                hWnd: hWnd,
                lParam: lParam,
                Msg: Msg,
                wParam: wParam
            );
#pragma warning restore CA1416
        }));
        var windowProcedureGcHandle = GCHandle.Alloc(value: windowProcedureDelegate);

        try {
            instance.DangerousAddRef(success: ref instanceAddRef);

            fixed (char* pClassName = className) {
                var windowClassSafeHandle = new SafeWin32WindowClassHandle(
                    className: className,
                    instance: instance
                );
                var windowClassHandle = PInvoke.RegisterClassEx(param0: new WNDCLASSEXW {
                    cbSize = (uint)sizeof(WNDCLASSEXW),
                    hInstance = ((HINSTANCE)instance.DangerousGetHandle()),
                    lpfnWndProc = ((delegate* unmanaged[Stdcall]<HWND, uint, WPARAM, LPARAM, LRESULT>)Marshal.GetFunctionPointerForDelegate(d: windowProcedureDelegate)),
                    lpszClassName = pClassName,
                });

                if (ushort.MinValue == windowClassHandle) {
                    throw new ExternalException(message: Marshal.GetLastPInvokeErrorMessage());
                }
                else {
                    windowClassSafeHandle.SetHandle(handle: windowClassHandle);
                }

                return windowClassSafeHandle;
            }
        }
        catch {
            if (instanceAddRef) {
                instance.DangerousRelease();
            }

            throw;
        }
        finally {
            windowProcedureGcHandle.Free();
        }
    }

    private readonly string m_className;
    private readonly SafeHandle m_instance;

    private SafeWin32WindowClassHandle(
        string className,
        SafeHandle instance
    ) : base(ownsHandle: true) {
        m_className = className;
        m_instance = instance;
    }

    protected unsafe override bool ReleaseHandle() {
#pragma warning disable CA1416
        var @className = m_className;
        var instance = m_instance;
        var result = PInvoke.UnregisterClass(
            hInstance: instance,
            lpClassName: @className
        );

        if ((instance is not null) && !instance.IsClosed && !instance.IsInvalid) {
            instance.DangerousRelease();
        }

        return result;
#pragma warning restore CA1416
    }
}
