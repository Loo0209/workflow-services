using NLog;
using System.Security.Cryptography;
using System.Text;

namespace wfDocServices.BL
{
    public class UtilityBL : IDisposable
    {
        private static byte[] key = Encoding.UTF8.GetBytes("YourSecretKey123");
        private static Logger logger = LogManager.GetCurrentClassLogger();
        public string ReadConfigFile(string strKey)
        {
            string temp = "";

            IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory) // Set the base path to your application's root
            .AddJsonFile("appsettings.json"); // Add the appsettings.json file

            IConfigurationRoot configuration = builder.Build();

            temp = configuration[strKey];
            return temp;

        }
        public string EncryptPassword(string password)
        {
            try
            {
                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = key;
                    aesAlg.GenerateIV();
                    ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                            {
                                swEncrypt.Write(password);
                            }
                        }
                        byte[] iv = aesAlg.IV;
                        byte[] encryptedBytes = msEncrypt.ToArray();
                        byte[] combinedIVAndCipherText = new byte[iv.Length + encryptedBytes.Length];
                        Array.Copy(iv, 0, combinedIVAndCipherText, 0, iv.Length);
                        Array.Copy(encryptedBytes, 0, combinedIVAndCipherText, iv.Length, encryptedBytes.Length);
                        return Convert.ToBase64String(combinedIVAndCipherText);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return null;
            }
        }

        public string DecryptPassword(string encryptedPassword)
        {
            try
            {
                byte[] combinedIVAndCipherText = Convert.FromBase64String(encryptedPassword);
                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = key;
                    byte[] iv = new byte[aesAlg.BlockSize / 8];
                    byte[] cipherText = new byte[combinedIVAndCipherText.Length - iv.Length];
                    Array.Copy(combinedIVAndCipherText, iv, iv.Length);
                    Array.Copy(combinedIVAndCipherText, iv.Length, cipherText, 0, cipherText.Length);
                    aesAlg.IV = iv;
                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                    using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                            {
                                return srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }
    }
}
