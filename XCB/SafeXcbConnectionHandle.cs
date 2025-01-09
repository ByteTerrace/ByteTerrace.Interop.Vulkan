using Microsoft.Win32.SafeHandles;

namespace XOrg.XCB;

public sealed class SafeXcbConnectionHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public unsafe static SafeXcbConnectionHandle Create(

    ) {
        var connection = Interop.Connect(
            displayName: null,
            screen: null
        );
        var connectionHandle = new SafeXcbConnectionHandle();

        if (IntPtr.Zero != connection) {
            connectionHandle.SetHandle(handle: connection);
        }

        return connectionHandle;
    }

    public SafeXcbConnectionHandle() : base(ownsHandle: true) { }

    protected unsafe override bool ReleaseHandle() {
        Interop.Disconnect(connection: ((XcbConnection)handle));

        return true;
    }
}
