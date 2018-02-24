using System.IO;
using System.Runtime.InteropServices;

public static class Extensions
{
    public static T Read<T>(this BinaryReader bin)
    {
        var bytes = bin.ReadBytes(Marshal.SizeOf(typeof(T)));
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        T ret = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
        handle.Free();
        return ret;
    }
}