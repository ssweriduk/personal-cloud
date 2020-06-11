using System;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Prng;

namespace PrivateCloud.CertificateManagement
{
    // Source: https://github.com/bcgit/bc-csharp/blob/f18a2dbbc2c1b4277e24a2e51f09cac02eedf1f5/crypto/src/crypto/prng/CryptoApiRandomGenerator.cs
    public class CryptoApiRandomGenerator
        : IRandomGenerator
    {
        private readonly RandomNumberGenerator rndProv;

        public CryptoApiRandomGenerator()
            : this(RandomNumberGenerator.Create())
        {
        }

        public CryptoApiRandomGenerator(RandomNumberGenerator rng)
        {
            rndProv = rng;
        }

        #region IRandomGenerator Members

        public virtual void AddSeedMaterial(byte[] seed)
        {
            // We don't care about the seed
        }

        public virtual void AddSeedMaterial(long seed)
        {
            // We don't care about the seed
        }

        public virtual void NextBytes(byte[] bytes)
        {
            rndProv.GetBytes(bytes);
        }

        public virtual void NextBytes(byte[] bytes, int start, int len)
        {
            if (start < 0)
                throw new ArgumentException("Start offset cannot be negative", "start");
            if (bytes.Length < (start + len))
                throw new ArgumentException("Byte array too small for requested offset and length");

            if (bytes.Length == len && start == 0)
            {
                NextBytes(bytes);
            }
            else
            {
                byte[] tmpBuf = new byte[len];
                NextBytes(tmpBuf);
                Array.Copy(tmpBuf, 0, bytes, start, len);
            }
        }

        #endregion
    }
}
