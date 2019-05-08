using System;
using System.Drawing;
using Console = Colorful.Console;

namespace Hackiibo
{
    class Program
    {
        static void Main(string[] args)
        {
            var keyManager = new KeyManager();

            keyManager.LoadKeys("keys/");

            if (!keyManager.HasFixedKey())
                throw new Exception("Fixed key is not initialized properly.");
            if (!keyManager.HasUnFixedKey())
                throw new Exception("UnFixed key is not initialized properly.");

            var tagData = TagUtil.ReadTag("amiibos/amiibo.bin");

            MifareUltralight mifare = MifareUltralight.GetTagInfo();
            if (mifare == null)
                throw new Exception("Error getting tag data. Possibly not a NTAG215");

            Console.WriteLineFormatted("Creating an amiibo NTAG is {0}, press a key to continue.", Color.Red, Color.White, "not reversable");
            Console.ReadLine();

            TagWriter.WriteToTagAuto(mifare, tagData, keyManager);

            mifare.Close();
        }
    }
}
