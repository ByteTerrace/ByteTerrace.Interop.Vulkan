using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace XOrg.XCB;

public unsafe static partial class Interop
{
    const string DllName = "libxcb.so.1";

    [LibraryImport(libraryName: DllName, EntryPoint = "xcb_connect")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial XcbConnection Connect(
        byte* displayName,
        int* screen
    );
    [LibraryImport(libraryName: DllName, EntryPoint = "xcb_create_window")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial XcbVoidCookie CreateWindow(
        XcbConnection connection,
        byte depth,
        uint windowId,
        uint parent,
        short x,
        short y,
        ushort width,
        ushort height,
        ushort borderWidth,
        ushort @class,
        uint visualId,
        uint valueMask,
        void* valueList
    );
    [LibraryImport(libraryName: DllName, EntryPoint = "xcb_destroy_window")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial XcbVoidCookie DestroyWindow(
        XcbConnection connection,
        uint window
    );
    [LibraryImport(libraryName: DllName, EntryPoint = "xcb_disconnect")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void Disconnect(XcbConnection connection);
    [LibraryImport(libraryName: DllName, EntryPoint = "xcb_flush")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int Flush(XcbConnection connection);
    [LibraryImport(libraryName: DllName, EntryPoint = "xcb_generate_id")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial uint GenerateId(XcbConnection connection);
    [LibraryImport(libraryName: DllName, EntryPoint = "xcb_get_setup")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial XcbSetup* GetSetup(XcbConnection connection);
    [LibraryImport(libraryName: DllName, EntryPoint = "xcb_map_window")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial XcbVoidCookie MapWindow(
        XcbConnection connection,
        uint window
    );
    [LibraryImport(libraryName: DllName, EntryPoint = "xcb_setup_roots_iterator")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial XcbScreenIterator SetupRootsIterator(XcbSetup* setup);
}
