using Microsoft.Win32.SafeHandles;
using TerraFX.Interop.Xlib;
using static TerraFX.Interop.Xlib.Xlib;

namespace XOrg.X11;

public sealed class SafeX11WindowHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public unsafe static SafeX11WindowHandle Create(
        nuint background,
        nuint border,
        uint borderWidth,
        SafeX11DisplayHandle displayHandle,
        uint height,
        uint width,
        Window parent,
        int x,
        int y
    ) {
        var addRefCountSuccess = false;
        var windowHandle = new SafeX11WindowHandle(displayHandle: displayHandle);

        displayHandle.DangerousAddRef(success: ref addRefCountSuccess);

        if (addRefCountSuccess) {
            var window = XCreateSimpleWindow(
                param0: ((Display*)displayHandle.DangerousGetHandle()),
                param1: parent,
                param2: x,
                param3: y,
                param4: width,
                param5: height,
                param6: borderWidth,
                param7: border,
                param8: background
            );

            if (IntPtr.Zero != window) {
                windowHandle.SetHandle(handle: window);
            }
            else {
                displayHandle.DangerousRelease();
            }
        }

        return windowHandle;
    }

    private readonly SafeX11DisplayHandle m_displayHandle;

    private SafeX11WindowHandle(SafeX11DisplayHandle displayHandle) : base(ownsHandle: true) {
        m_displayHandle = displayHandle;
    }

    protected unsafe override bool ReleaseHandle() {
        return Convert.ToBoolean(value: XDestroyWindow(
            param0: ((Display*)m_displayHandle.DangerousGetHandle()),
            param1: ((Window)handle))
        );
    }
}
