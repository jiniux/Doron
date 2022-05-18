using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Doron
{
    internal static class Crypto
    {
        private static SHA1 SHA1 = SHA1.Create();

        public static string ComputeSHA1Base64(byte[] data) => 
            Convert.ToBase64String(SHA1.ComputeHash(data));
    }
}
