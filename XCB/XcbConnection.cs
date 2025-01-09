namespace XOrg.XCB;

public readonly partial struct XcbConnection(IntPtr value)
{
    public readonly IntPtr Value = value;

    public static implicit operator IntPtr(XcbConnection value) => value.Value;

    public static explicit operator XcbConnection(IntPtr value) => new(value: value);
    public static bool operator ==(XcbConnection left, XcbConnection right) => (left.Value == right.Value);

    public static bool operator !=(XcbConnection left, XcbConnection right) => !(left == right);

    public bool Equals(XcbConnection other) => (Value == other.Value);

    public override bool Equals(object? obj) => ((obj is XcbConnection other) && Equals(other));

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => $"0x{Value:x}";
}
