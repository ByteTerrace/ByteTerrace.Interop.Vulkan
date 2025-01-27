﻿using Microsoft.Win32.SafeHandles;

namespace XOrg.XCB;

public sealed class SafeXcbWindowHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public unsafe static SafeXcbWindowHandle Create(
        ushort borderWidth,
        SafeXcbConnectionHandle connectionHandle,
        byte depth,
        ushort height,
        XcbScreen screen,
        ushort width,
        short x,
        short y
    ) {
        var addRefCountSuccess = false;

        try {
            connectionHandle.DangerousAddRef(success: ref addRefCountSuccess);

            var connection = ((XcbConnection)connectionHandle.DangerousGetHandle());
            var windowId = Interop.GenerateId(connection: connection);
            _ = Interop.CreateWindow(
                @class: 1,
                borderWidth: borderWidth,
                connection: connection,
                depth: depth,
                height: height,
                parent: screen.root,
                valueList: null,
                valueMask: 0,
                visualId: screen.root_visual,
                windowId: windowId,
                width: width,
                x: x,
                y: y
            );
            var windowHandle = new SafeXcbWindowHandle(connectionHandle: connectionHandle);

            windowHandle.SetHandle(handle: ((nint)windowId));

            return windowHandle;
        }
        catch {
            if (addRefCountSuccess) {
                connectionHandle.DangerousRelease();
            }

            throw;
        }
    }

    private readonly SafeXcbConnectionHandle m_connectionHandle;

    private SafeXcbWindowHandle(SafeXcbConnectionHandle connectionHandle) : base(ownsHandle: true) {
        m_connectionHandle = connectionHandle;
    }

    protected unsafe override bool ReleaseHandle() {
        var connectionHandle = m_connectionHandle;

        Interop.DestroyWindow(
            connection: ((XcbConnection)connectionHandle.DangerousGetHandle()),
            window: ((uint)handle)
        );

        try { connectionHandle.DangerousRelease(); } catch {}

        return true;
    }
}
