using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Ionburst.Apps.IonFS.Model
{
    public class IonFSCrypto
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "<Pending>")]
        public byte[] Key { get; set; }

        public string KeyToString()
        {
            return BitConverter.ToString(Key).Replace("-", "");
        }

        public byte[] StringToByteArray(String hex)
        {
            //https://stackoverflow.com/questions/311165/how-do-you-convert-a-byte-array-to-a-hexadecimal-string-and-vice-versa
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        public byte[] KeyGen(string passphrase)
        {
            MD5 md = MD5.Create();
            byte[] pp = Encoding.ASCII.GetBytes(passphrase);
            byte[] key_128 = md.ComputeHash(pp);
            //Console.WriteLine("128:{0}", BitConverter.ToString(key_128).Replace("-", ""));

            byte[] key_pp = new byte[key_128.Length + pp.Length];
            Buffer.BlockCopy(key_128, 0, key_pp, 0, key_128.Length);
            Buffer.BlockCopy(pp, 0, key_pp, key_128.Length, pp.Length);

            byte[] key_pp_hash = md.ComputeHash(key_pp);

            byte[] key_256 = new byte[key_128.Length + key_pp_hash.Length];
            Buffer.BlockCopy(key_128, 0, key_256, 0, key_128.Length);
            Buffer.BlockCopy(key_pp_hash, 0, key_256, key_128.Length, key_pp_hash.Length);

            //Console.WriteLine("256:{0}", BitConverter.ToString(key_256).Replace("-", ""));

            return key_256;
        }

        public void KeyFromFile(string path)
        {
            string keyString = File.ReadAllText(path);
            Key = StringToByteArray(keyString);
        }

        public void KeyFromPassphrase(string passphrase)
        {
            Key = KeyGen(passphrase);
        }
    }
}
