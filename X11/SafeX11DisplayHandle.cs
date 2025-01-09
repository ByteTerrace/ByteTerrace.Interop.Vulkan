using Microsoft.Win32.SafeHandles;
using TerraFX.Interop.Xlib;
using static TerraFX.Interop.Xlib.Xlib;

namespace XOrg.X11;

public sealed class SafeX11DisplayHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public unsafe static SafeX11DisplayHandle Create() {
        var display = XOpenDisplay(param0: null);
        var displayHandle = new SafeX11DisplayHandle();

        if (null != display) {
            displayHandle.SetHandle(handle: ((nint)display));
        }

        return displayHandle;
    }

    public SafeX11DisplayHandle() : base(ownsHandle: true) { }

    protected unsafe override bool ReleaseHandle() {
        return Convert.ToBoolean(value: XCloseDisplay(param0: ((Display*)handle)));
    }
}
