using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Hackiibo
{
    public class KeyManager
    {
        public static String FIXED_KEY_FILE = "fixed_key.bin";
        private static String FIXED_KEY_MD5 = "0AD86557C7BA9E75C79A7B43BB466333";

        public static String UNFIXED_KEY_FILE = "unfixed_key.bin";
        private static String UNFIXED_KEY_MD5 = "2551AFC7C8813008819836E9B619F7ED";

        public byte[] fixedKey;
        public byte[] unfixedKey;

        private void ChecksumFile(string filePath, string expectedChecksum)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    var checksum = string.Join("",
                        md5.ComputeHash(stream)
                            .Select(x => x.ToString("X2"))
                            .ToArray());

                    if (checksum != expectedChecksum)
                    {
                        throw new Exception($"Checksum for file {filePath} is wrong.");
                    }
                }
            }
        }

        public void LoadKeys(string folderPath)
        {
            var fixedKeyPath = folderPath + FIXED_KEY_FILE;
            var unfixedKeyPath = folderPath + UNFIXED_KEY_FILE;

            ChecksumFile(fixedKeyPath, FIXED_KEY_MD5);
            ChecksumFile(unfixedKeyPath, UNFIXED_KEY_MD5);

            fixedKey = File.ReadAllBytes(fixedKeyPath);
            unfixedKey = File.ReadAllBytes(unfixedKeyPath);
        }

        public bool HasFixedKey()
        {
            return fixedKey != default(byte[]);
        }

        internal bool HasUnFixedKey()
        {
            return unfixedKey != default(byte[]);
        }
    }
}