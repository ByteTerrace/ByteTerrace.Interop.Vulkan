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

        try {
            displayHandle.DangerousAddRef(success: ref addRefCountSuccess);

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
            var windowHandle = new SafeX11WindowHandle(displayHandle: displayHandle);

            windowHandle.SetHandle(handle: window);

            return windowHandle;
        }
        catch {
            if (addRefCountSuccess) {
                displayHandle.DangerousRelease();
            }

            throw;
        }
    }

    private readonly SafeX11DisplayHandle m_displayHandle;

    private SafeX11WindowHandle(SafeX11DisplayHandle displayHandle) : base(ownsHandle: true) {
        m_displayHandle = displayHandle;
    }

    protected unsafe override bool ReleaseHandle() {
        var displayHandle = m_displayHandle;

        _ = XDestroyWindow(
            param0: ((Display*)displayHandle.DangerousGetHandle()),
            param1: ((Window)handle)
        );

        if ((displayHandle is not null) && !displayHandle.IsClosed && !displayHandle.IsInvalid) {
            try { displayHandle.DangerousRelease(); } catch {}
        }

        return true;
    }
}
