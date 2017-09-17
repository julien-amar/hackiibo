using System;
using System.Drawing;
using Console = Colorful.Console;

namespace Hackiibo
{
    public class TagWriter
    {
        public static bool WriteToTagAuto(MifareUltralight mifare, byte[] tagData, KeyManager keyManager)
        {
            tagData = AdjustTag(keyManager, tagData, mifare);

            if (!Validate(mifare, tagData) || !ValidateBlankTag(mifare))
            {
                return false;
            }

            try
            {
                byte[][] pages = TagUtil.SplitPages(tagData);
                WritePages(mifare, 3, 129, pages);
                Console.WriteLine("Wrote main data", Color.Green);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while writing main data (stage 1)", Color.Red);
                return false;
            }

            try
            {
                WritePassword(mifare);
                Console.WriteLine("Wrote password", Color.Green);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while setting password (stage 2)", Color.Red);
                return false;
            }

            try
            {
                WriteLockInfo(mifare);
                Console.WriteLine("Wrote lock info", Color.Green);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while setting lock info (stage 3)", Color.Red);
                return false;
            }

            return true;
        }

        private static bool ValidateBlankTag(MifareUltralight mifare)
        {
            byte[] lockPage = mifare.ReadPages(0x02);

            if (lockPage[2] == (byte)0x0F && lockPage[3] == (byte)0xE0)
            {
                Console.WriteLine("Tag already an amiibo, it can not be overwritten.", Color.Red);
                return false;
            }

            Console.WriteLine("Tag is not locked.", Color.Green);

            return true;
        }

        static bool CompareRange(byte[] data, byte[] data2, int data2Offset, int len)
        {
            for (int i = data2Offset, j = 0; j < len; i++, j++)
            {
                if (data[j] != data2[i])
                    return false;
            }
            return true;
        }

        static byte[] AdjustTag(KeyManager keyManager, byte[] tagData, MifareUltralight mifare)
        {
            byte[] pages = mifare.ReadPages(0);
            if (pages == null || pages.Length != TagUtil.PAGE_SIZE * 4)
                throw new Exception("Read failed! Unexpected read size.");

            return TagUtil.PatchUid(pages, tagData, keyManager, true);
        }

        static bool Validate(MifareUltralight mifare, byte[] tagData)
        {
            if (tagData == null)
            {
                Console.WriteLine("Cannot validate: no source data loaded to compare.", Color.Red);
                return false;
            }

            byte[] pages = mifare.ReadPages(0);
            if (pages == null || pages.Length != TagUtil.PAGE_SIZE * 4)
            {
                Console.WriteLine("Read failed! Unexpected read size.", Color.Red);
                return false;
            }

            if (!CompareRange(pages, tagData, 0, 9))
            {
                Console.WriteLine("Source UID does not match the target!", Color.Red);
                return false;
            }

            return true;
        }

        static void WritePages(MifareUltralight tag, byte pagestart, byte pageend, byte[][] data)
        {
            for (byte i = pagestart; i <= pageend; i++)
            {
                tag.WritePage(i, data[i]);
            }
        }

        static void WritePassword(MifareUltralight tag)
        {
            byte[] pages0_1 = tag.ReadPages(0);

            if (pages0_1 == null || pages0_1.Length != TagUtil.PAGE_SIZE * 4)
                throw new Exception("Read failed");

            byte[] uid = TagUtil.UidFromPages(pages0_1);
            byte[] password = TagUtil.Keygen(uid);

            tag.WritePage(0x86, new byte[] { (byte)0x80, (byte)0x80, (byte)0, (byte)0 });

            tag.WritePage(0x85, password);
        }

        static void WriteLockInfo(MifareUltralight tag)
        {
            byte[] pages = tag.ReadPages(0);

            if (pages == null || pages.Length != TagUtil.PAGE_SIZE * 4)
                throw new Exception("Read failed");

            //lock bits
            tag.WritePage(2, new byte[] {
                pages[2 * TagUtil.PAGE_SIZE],
                pages[(2 * TagUtil.PAGE_SIZE) + 1],
                (byte)0x0F,
                (byte)0xE0 }
            );
            //dynamic lock bits. should the last bit be 0xBD accoridng to the nfc docs though:
            //Remark: Set all bits marked with RFUI to 0, when writing to the dynamic lock bytes.
            tag.WritePage(130, new byte[] { (byte)0x01, (byte)0x00, (byte)0x0F, (byte)0x00 });
            //config
            tag.WritePage(131, new byte[] { (byte)0x00, (byte)0x00, (byte)0x00, (byte)0x04 });
            //config
            tag.WritePage(132, new byte[] { (byte)0x5F, (byte)0x00, (byte)0x00, (byte)0x00 });
        }
    }
}
