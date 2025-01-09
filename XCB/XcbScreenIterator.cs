namespace XOrg.XCB;

public unsafe struct XcbScreenIterator
{
    public XcbScreen* data;
    public int rem;
    public int index;
}
