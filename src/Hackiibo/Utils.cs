using System;

namespace Hackiibo
{
    public class Util
    {
        private static char[] _hexArray = "0123456789ABCDEF".ToCharArray();

        public static String BytesToHex(byte[] bytes)
        {
            char[] hexChars = new char[bytes.Length * 2];
            for (int j = 0; j < bytes.Length; j++)
            {
                int v = bytes[j] & 0xFF;
                hexChars[j * 2] = _hexArray[v >> 4];
                hexChars[j * 2 + 1] = _hexArray[v & 0x0F];
            }
            return new String(hexChars);
        }
    }
}
