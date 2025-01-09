namespace XOrg.XCB;

public readonly partial struct XcbWindow(uint value)
{
    public readonly uint Value = value;

    public static implicit operator uint(XcbWindow value) => value.Value;

    public static explicit operator XcbWindow(uint value) => new(value: value);
    public static bool operator ==(XcbWindow left, XcbWindow right) => (left.Value == right.Value);

    public static bool operator !=(XcbWindow left, XcbWindow right) => !(left == right);

    public bool Equals(XcbWindow other) => (Value == other.Value);

    public override bool Equals(object? obj) => ((obj is XcbWindow other) && Equals(other));

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => $"0x{Value:x}";
}
