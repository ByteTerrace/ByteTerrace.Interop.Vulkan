public unsafe struct XcbSetup
{
    public byte status;
    public byte pad0;
    public ushort protocol_major_version;
    public ushort protocol_minor_version;
    public ushort length;
    public uint release_number;
    public uint resource_id_base;
    public uint resource_id_mask;
    public uint motion_buffer_size;
    public ushort vendor_len;
    public ushort maximum_request_length;
    public byte roots_len;
    public byte pixmap_formats_len;
    public byte image_byte_order;
    public byte bitmap_format_bit_order;
    public byte bitmap_format_scanline_unit;
    public byte bitmap_format_scanline_pad;
    public byte min_keycode;
    public byte max_keycode;
    public fixed byte pad1[4];
}
