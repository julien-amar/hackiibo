using System;
using System.IO;
using System.Linq;

namespace Hackiibo
{
    public class TagUtil
    {
        public static int TAG_FILE_SIZE = 532;
        public static int PAGE_SIZE = 4;
        public static int AMIIBO_ID_OFFSET = 0x54;
        public static int APP_ID_OFFSET = 0xB6;
        public static int APP_ID_LENGTH = 4;

        public static byte[] Keygen(byte[] uuid)
        {
            //from AmiiManage (GPL)
            byte[] key = new byte[4];
            int[] uuid_to_ints = new int[uuid.Length];

            for (int i = 0; i < uuid.Length; i++)
                uuid_to_ints[i] = (0xFF & uuid[i]);

            if (uuid.Length == 7)
            {
                key[0] = ((byte)(0xFF & (0xAA ^ (uuid_to_ints[1] ^ uuid_to_ints[3]))));
                key[1] = ((byte)(0xFF & (0x55 ^ (uuid_to_ints[2] ^ uuid_to_ints[4]))));
                key[2] = ((byte)(0xFF & (0xAA ^ (uuid_to_ints[3] ^ uuid_to_ints[5]))));
                key[3] = ((byte)(0xFF & (0x55 ^ (uuid_to_ints[4] ^ uuid_to_ints[6]))));
                return key;
            }

            return null;
        }

        /**
         * Returns the UID of a tag from first two pages of data (TagFormat)
         */
        public static byte[] UidFromPages(byte[] pages0_1)
        {
            //removes the checksum bytes from the first two pages of a tag to get the actual uid
            if (pages0_1.Length < 8) return null;

            byte[] key = new byte[7];
            key[0] = pages0_1[0];
            key[1] = pages0_1[1];
            key[2] = pages0_1[2];
            key[3] = pages0_1[4];
            key[4] = pages0_1[5];
            key[5] = pages0_1[6];
            key[6] = pages0_1[7];
            return key;
        }

        public static long AmiiboIdFromTag(byte[] data)
        {
            if (data.Length < TAG_FILE_SIZE)
                throw new Exception("Invalid tag data.");

            byte[] amiiboId = new byte[4 * 2];
            Array.Copy(data, AMIIBO_ID_OFFSET, amiiboId, 0, amiiboId.Length);
            return BitConverter.ToInt64(amiiboId, 0);
        }

        public static String AmiiboIdToHex(long amiiboId)
        {
            return amiiboId.ToString("x16");
        }

        public static byte[][] SplitPages(byte[] data)
        {
            if (data.Length < TAG_FILE_SIZE)
                throw new Exception("Invalid tag data.");

            byte[][] pages = new byte[data.Length / TagUtil.PAGE_SIZE][];
            for (int i = 0, j = 0; i < data.Length; i += TagUtil.PAGE_SIZE, j++)
            {
                pages[j] = data
                        .Skip(i)
                        .Take(TagUtil.PAGE_SIZE)
                        .ToArray();
            }
            return pages;
        }

        public static void ValidateTag(byte[] data)
        {
            byte[][] pages = TagUtil.SplitPages(data);

            if (pages[0][0] != (byte)0x04)
                throw new Exception("Invalid tag file. Tag must start with a 0x04.");

            if (pages[2][2] != (byte)0x0F || pages[2][3] != (byte)0xE0)
                throw new Exception("Invalid tag file. lock signature mismatch.");

            if (pages[3][0] != (byte)0xF1 || pages[3][1] != (byte)0x10 || pages[3][2] != (byte)0xFF || pages[3][3] != (byte)0xEE)
                throw new Exception("Invalid tag file. CC signature mismatch.");

            if (pages[0x82][0] != (byte)0x01 || pages[0x82][1] != (byte)0x0 || pages[0x82][2] != (byte)0x0F)
                throw new Exception("Invalid tag file. dynamic lock signature mismatch.");

            if (pages[0x83][0] != (byte)0x0 || pages[0x83][1] != (byte)0x0 || pages[0x83][2] != (byte)0x0 || pages[0x83][3] != (byte)0x04)
                throw new Exception("Invalid tag file. CFG0 signature mismatch.");

            if (pages[0x84][0] != (byte)0x5F || pages[0x84][1] != (byte)0x0 || pages[0x84][2] != (byte)0x0 || pages[0x84][3] != (byte)0x00)
                throw new Exception("Invalid tag file. CFG1 signature mismatch.");
        }

        public static byte[] Decrypt(KeyManager keyManager, byte[] tagData)
        {
            if (!keyManager.HasFixedKey() || !keyManager.HasUnFixedKey())
                throw new Exception("Key files not loaded!");

            if (NativeMethods.SetKeysFixed(keyManager.fixedKey, keyManager.fixedKey.Length) == 0)
                throw new Exception("Failed to initialise amiitool");
            if (NativeMethods.SetKeysUnfixed(keyManager.unfixedKey, keyManager.unfixedKey.Length) == 0)
                throw new Exception("Failed to initialise amiitool");
            byte[] decrypted = new byte[TagUtil.TAG_FILE_SIZE];
            if (NativeMethods.Unpack(tagData, tagData.Length, decrypted, decrypted.Length) == 0)
                throw new Exception("Failed to decrypt tag");

            return decrypted;
        }

        public static byte[] Encrypt(KeyManager keyManager, byte[] tagData)
        {
            if (!keyManager.HasFixedKey() || !keyManager.HasUnFixedKey())
                throw new Exception("Key files not loaded!");

            if (NativeMethods.SetKeysFixed(keyManager.fixedKey, keyManager.fixedKey.Length) == 0)
                throw new Exception("Failed to initialise amiitool");
            if (NativeMethods.SetKeysUnfixed(keyManager.unfixedKey, keyManager.unfixedKey.Length) == 0)
                throw new Exception("Failed to initialise amiitool");
            byte[] encrypted = new byte[TagUtil.TAG_FILE_SIZE];
            if (NativeMethods.Pack(tagData, tagData.Length, encrypted, encrypted.Length) == 0)
                throw new Exception("Failed to decrypt tag");

            return encrypted;
        }


        public static byte[] PatchUid(byte[] uid, byte[] tagData, KeyManager keyManager, bool encrypted)
        {
            if (encrypted)
                tagData = Decrypt(keyManager, tagData);

            if (uid.Length < 9) throw new Exception("Invalid uid length.");

            byte[] patched = new byte[tagData.Length];
            Array.Copy(tagData, patched, tagData.Length);

            Array.Copy(uid, 0, patched, 0x1d4, 8);
            patched[0] = uid[8];

            byte[] result = new byte[TagUtil.TAG_FILE_SIZE];
            if (NativeMethods.Pack(patched, patched.Length, result, result.Length) == 0)
                throw new Exception("Failed to encrypt tag");

            return result;
        }

        public static byte[] ReadTag(string fileName)
        {
            using (BinaryReader inputStream = new BinaryReader(File.Open(fileName, FileMode.Open)))
            {
                byte[] data = new byte[TAG_FILE_SIZE];
                try
                {
                    data = inputStream.ReadBytes(TAG_FILE_SIZE);
                    if (data.Length != TAG_FILE_SIZE)
                        throw new Exception("Invalid file size. was expecting " + TAG_FILE_SIZE);

                    return data;
                }
                finally
                {
                    inputStream.Close();
                }
            }
        }
    }
}

