using System.Runtime.InteropServices;

namespace Hackiibo
{
    public class NativeMethods
    {
        [DllImport("Amiitool.dll", EntryPoint = "setKeysFixed",  CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SetKeysFixed(byte[] data, int length);
        [DllImport("Amiitool.dll", EntryPoint = "setKeysUnfixed", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SetKeysUnfixed(byte[] data, int length);
        [DllImport("Amiitool.dll", EntryPoint = "unpack", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Unpack(byte[] tag, int tagLength, byte[] unpackedTag, int unpackedTagLength);
        [DllImport("Amiitool.dll", EntryPoint = "pack", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Pack(byte[] tag, int tagLength, byte[] unpackedTag, int unpackedTagLength);
    }
}