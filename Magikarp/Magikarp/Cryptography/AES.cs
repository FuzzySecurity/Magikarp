using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Cryptography
{
    public class AES
    {
        // Generate a random AES key material
        internal static General.AESKeyMaterial GenerateRandomAESKeyMaterial()
        {
            // Create random 20-byte Pass
            Byte[] bPass = new Byte[20];
            using (RandomNumberGenerator rngCryptoServiceProvider = RandomNumberGenerator.Create())
            {
                rngCryptoServiceProvider.GetBytes(bPass);
            }
            
            // Create random 20-byte Salt
            Byte[] bSalt = new Byte[20];
            using (RandomNumberGenerator rngCryptoServiceProvider = RandomNumberGenerator.Create())
            {
                rngCryptoServiceProvider.GetBytes(bSalt);
            }
            
            // Iterate the input
            Rfc2898DeriveBytes oRfc2898DeriveBytes = new Rfc2898DeriveBytes(bPass, bSalt, 20);
            
            // Generate the key material
            General.AESKeyMaterial oAESKeyMaterial = new General.AESKeyMaterial
            {
                Key = oRfc2898DeriveBytes.GetBytes(32),
                IV = oRfc2898DeriveBytes.GetBytes(16)
            };
            
            return oAESKeyMaterial;
        }
        
        // Generate derived AES key material
        internal static General.AESKeyMaterial GenerateDerivedAesKeyMaterial(Byte[] sharedSecret)
        {
            // Split the shared secret
            Byte[] bPassword = new Byte[sharedSecret.Length / 2];
            Byte[] bSalt = new Byte[sharedSecret.Length / 2];
            
            Buffer.BlockCopy(sharedSecret, 0, bPassword, 0, sharedSecret.Length / 2);
            Buffer.BlockCopy(sharedSecret, sharedSecret.Length / 2, bSalt, 0, sharedSecret.Length / 2);
            
            // Iterate the input
            Rfc2898DeriveBytes oRfc2898DeriveBytes = new Rfc2898DeriveBytes(bPassword, bSalt, 20);
            
            // Generate the key material
            General.AESKeyMaterial oAESKeyMaterial = new General.AESKeyMaterial
            {
                Key = oRfc2898DeriveBytes.GetBytes(32),
                IV = oRfc2898DeriveBytes.GetBytes(16)
            };
            
            return oAESKeyMaterial;
        }
        
        // Encrypt AES key material with shared secret
        internal static Byte[] EncryptAESKeyMaterialWithSharedSecret(Byte[] sharedSecret, General.AESKeyMaterial oAESKeyMaterial)
        {
            // Generate derived AES key material
            General.AESKeyMaterial oDerivedAESKeyMaterial = GenerateDerivedAesKeyMaterial(sharedSecret);
            
            // Serialize the random AES key material
            String sAESKeyMaterial = General.SerializeECCObject(oAESKeyMaterial, false);
            
            // Encrypt the random AES key material
            return EncryptAES(Encoding.ASCII.GetBytes(sAESKeyMaterial), oDerivedAESKeyMaterial.Key, oDerivedAESKeyMaterial.IV);
        }
        
        // Decrypt AES key material with shared secret
        internal static General.AESKeyMaterial DecryptAESKeyMaterialWithSharedSecret(Byte[] sharedSecret, Byte[] bInput)
        {
            // Generate derived AES key material
            General.AESKeyMaterial oDerivedAESKeyMaterial = GenerateDerivedAesKeyMaterial(sharedSecret);
            
            // Decrypt the random AES key material
            String sAESKeyMaterial = Encoding.ASCII.GetString(DecryptAES(bInput, oDerivedAESKeyMaterial.Key, oDerivedAESKeyMaterial.IV));
            
            // Deserialize the random AES key material
            return General.DeserializeECCObject<General.AESKeyMaterial>(sAESKeyMaterial);
        }
        
        // Encrypt a byte array using AES
        internal static Byte[] EncryptAES(Byte[] bInput, Byte[] bKey, Byte[] bIV)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = bKey;
                aes.IV = bIV;
                
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(bInput, 0, bInput.Length);
                    }
                    return msEncrypt.ToArray();
                }
            }
        }
        
        // Decrypt a byte array using AES
        internal static Byte[] DecryptAES(Byte[] bInput, Byte[] bKey, Byte[] bIV)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = bKey;
                aes.IV = bIV;
                
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using (MemoryStream msDecrypt = new MemoryStream())
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Write))
                    {
                        csDecrypt.Write(bInput, 0, bInput.Length);
                    }
                    return msDecrypt.ToArray();
                }
            }
        }
    }
}