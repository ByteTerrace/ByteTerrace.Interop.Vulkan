namespace XOrg.XCB;

public struct XcbScreen
{
    public uint root;
    public uint default_colormap;
    public uint white_pixel;
    public uint black_pixel;
    public uint current_input_masks;
    public ushort width_in_pixels;
    public ushort height_in_pixels;
    public ushort width_in_millimeters;
    public ushort height_in_millimeters;
    public ushort min_installed_maps;
    public ushort max_installed_maps;
    public uint root_visual;
    public byte backing_stores;
    public byte save_unders;
    public byte root_depth;
    public byte allowed_depths_len;
}
