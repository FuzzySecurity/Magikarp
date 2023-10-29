using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Commands;

namespace Cryptography
{
    internal class ECC
    {
        // Generate Public Key, Private Key, and PFX
        internal static void GenerateKeyPair(String sName, Arguments.ECCurveType eCurveType, String sOutputDirectory = "")
        {
            // If OutputDirectory, check if it exists
            if (sOutputDirectory != String.Empty)
            {
                if (!Directory.Exists(sOutputDirectory))
                {
                    Console.WriteLine("\n[!] Output directory does not exist..");
                    return;
                }
            }
            
            // Initialize the ECDH object
            ECDiffieHellman ecdh = ECDiffieHellman.Create(ECCurve.CreateFromFriendlyName(Enum.GetName(typeof(Arguments.ECCurveType), eCurveType) ?? String.Empty));
            
            // Get the public key
            ECParameters ecParameters = ecdh.ExportParameters(true);
            Byte[] xBytes = ecParameters.Q.X;
            Byte[] yBytes = ecParameters.Q.Y;
            String sPublicKey = General.SerializeECCObject(new General.ECCPublicKey
            {
                Name = sName,
                UtcTimestamp = DateTime.UtcNow,
                Curve = Enum.GetName(typeof(Arguments.ECCurveType), eCurveType),
                X = Convert.ToBase64String(xBytes),
                Y = Convert.ToBase64String(yBytes)
            });
            
            // Get the private key
            Byte[] dBytes = ecParameters.D;
            String sPrivateKey = General.SerializeECCObject(new General.ECCPrivateKey
            {
                Name = sName,
                UtcTimestamp = DateTime.UtcNow,
                Curve = Enum.GetName(typeof(Arguments.ECCurveType), eCurveType),
                D = Convert.ToBase64String(dBytes)
            });
            
            // Create PFX
            Byte[] bPFX = X509.CreateX509Certificate(ecParameters, sName);
            
            // Write the files to disk
            Console.WriteLine("\n[+] Key material generated successfully");
            String sAssemblyDirectory = String.Empty;
            if (sOutputDirectory == String.Empty)
            {
                sAssemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }
            else
            {
                // Normalize the path, if it ends with a slash, remove it
                if (sOutputDirectory.EndsWith(Path.DirectorySeparatorChar.ToString()))
                {
                    sOutputDirectory = sOutputDirectory.Substring(0, sOutputDirectory.Length - 1);
                }
                sAssemblyDirectory = sOutputDirectory;
            }

            String sBasePath = sAssemblyDirectory + Path.DirectorySeparatorChar + sName;
            File.WriteAllText(sBasePath + ".key", sPrivateKey);
            Console.WriteLine("    |_ Path: " + sBasePath + ".key");
            File.WriteAllText(sBasePath + ".pub", sPublicKey);
            Console.WriteLine("    |_ Path: " + sBasePath + ".pub");
            File.WriteAllBytes(sBasePath + ".pfx", bPFX);
            Console.WriteLine("    |_ Path: " + sBasePath + ".pfx");
        }
        
        // Encrypt a file
        internal static void EncryptFile(String sInputFile, String sPrivateKeyPath, String sPublicKeyPath, String sOutputDirectory = "")
        {
            // If OutputDirectory, check if it exists
            if (sOutputDirectory != String.Empty)
            {
                if (!Directory.Exists(sOutputDirectory))
                {
                    Console.WriteLine("\n[!] Output directory does not exist..");
                    return;
                }
            }
            
            // Check if the files exist
            if (!File.Exists(sInputFile) || !File.Exists(sPublicKeyPath) || !File.Exists(sPrivateKeyPath))
            {
                Console.WriteLine("\n[!] One or more input files do not exist..");
                return;
            }
            
            // Read in the file
            Byte[] bFile = File.ReadAllBytes(sInputFile);
            
            // Read the public key
            String sPublicKey = File.ReadAllText(sPublicKeyPath);
            General.ECCPublicKey eccPublicKey = General.DeserializeECCObject<General.ECCPublicKey>(sPublicKey);
            
            // Read the private key
            String sPrivateKey = File.ReadAllText(sPrivateKeyPath);
            General.ECCPrivateKey eccPrivateKey = General.DeserializeECCObject<General.ECCPrivateKey>(sPrivateKey);
            
            if (eccPublicKey.Curve != eccPrivateKey.Curve)
            {
                Console.WriteLine("\n[!] Both parties are not using the same curve..");
                return;
            }
            
            // Create the ECParameters object for the recipient
            ECParameters ecRecipient = new ECParameters
            {
                Curve = ECCurve.CreateFromFriendlyName(eccPublicKey.Curve ?? String.Empty),
                Q = new ECPoint
                {
                    X = Convert.FromBase64String(eccPublicKey.X),
                    Y = Convert.FromBase64String(eccPublicKey.Y)
                }
            };
            ECDiffieHellman ecdhRecipient = CreateECDiffieHellmanFromECParameters(ecRecipient);
            
            // Create the ECParameters object for the sender
            ECParameters ecSender = new ECParameters
            {
                Curve = ECCurve.CreateFromFriendlyName(eccPrivateKey.Curve ?? String.Empty),
#if NETFRAMEWORK
                Q = new ECPoint
                {
                    X = Convert.FromBase64String(eccPublicKey.X),
                    Y = Convert.FromBase64String(eccPublicKey.Y),
                },
#endif
                D = Convert.FromBase64String(eccPrivateKey.D)
            };
            ECDiffieHellman ecdhSender = CreateECDiffieHellmanFromECParameters(ecSender);
            
            // Get the shared secret
            Byte[] sharedSecret = ecdhSender.DeriveKeyMaterial(ecdhRecipient.PublicKey);
            
            // Print the shared secret
            Console.WriteLine("\n[+] Shared secret: " + String.Concat(sharedSecret.Select(x => x.ToString("X2"))));
            
            // Generate signature
            Console.WriteLine("[>] Signing SHA256 file hash with ECCurve");
            Byte[] signature = SignDataWithECDH(ecdhSender, bFile);
            
            // Generate random AES key material
            Console.WriteLine("[>] Generating random AES key material..");
            General.AESKeyMaterial oAESKeyMaterial = AES.GenerateRandomAESKeyMaterial();
            
            // Encrypt file using random AES key material
            Console.WriteLine("    |_ Encrypting compressed file");
            Byte[] bEncryptedFile = AES.EncryptAES(General.CompressByteArray(bFile), oAESKeyMaterial.Key, oAESKeyMaterial.IV);
            
            // Encrypt AES key material using ECDH shared secret
            Console.WriteLine("    |_ Protecting AES key with ECCurve shared secret");
            Byte[] bEncryptedAESKeyMaterial = AES.EncryptAESKeyMaterialWithSharedSecret(sharedSecret, oAESKeyMaterial);
            
            // Create encrypted file object
            Console.WriteLine("[>] Creating encrypted file object..");
            General.EncryptedFile oEncryptedFile = new General.EncryptedFile
            {
                FileName = Path.GetFileName(sInputFile),
                UtcTimestamp = DateTime.UtcNow,
                Signature = Convert.ToBase64String(signature),
                Key = Convert.ToBase64String(bEncryptedAESKeyMaterial),
                File = Convert.ToBase64String(bEncryptedFile)
            };
            
            // Serialize encrypted file object
            String sEncryptedFile = General.SerializeECCObject(oEncryptedFile);
            
            // Write encrypted file object to disk
            String sEncryptedFilePath = String.Empty;
            if (sOutputDirectory == String.Empty)
            {
                sEncryptedFilePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(sInputFile) + ".karp";
            }
            else
            {
                // Normalize the path, if it ends with a slash, remove it
                if (sOutputDirectory.EndsWith(Path.DirectorySeparatorChar.ToString()))
                {
                    sOutputDirectory = sOutputDirectory.Substring(0, sOutputDirectory.Length - 1);
                }
                sEncryptedFilePath = sOutputDirectory + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(sInputFile) + ".karp";
            }
            File.WriteAllText(sEncryptedFilePath, sEncryptedFile);
            Console.WriteLine("    |_ Path: " + sEncryptedFilePath);
        }
        
        // Decrypt a file
        internal static void DecryptFile(String sInputFile, String sPrivateKeyPath, String sPublicKeyPath, String sOutputDirectory = "")
        {
            // If OutputDirectory, check if it exists
            if (sOutputDirectory != String.Empty)
            {
                if (!Directory.Exists(sOutputDirectory))
                {
                    Console.WriteLine("\n[!] Output directory does not exist..");
                    return;
                }
            }
            
            // Check if the files exist
            if (!File.Exists(sInputFile) || !File.Exists(sPublicKeyPath) || !File.Exists(sPrivateKeyPath))
            {
                Console.WriteLine("\n[!] One or more input files do not exist..");
                return;
            }

            // Read in the file
            String sEncryptedFile = File.ReadAllText(sInputFile);

            // Deserialize encrypted file object
            General.EncryptedFile oEncryptedFile = General.DeserializeECCObject<General.EncryptedFile>(sEncryptedFile);

            // Read the public key
            String sPublicKey = File.ReadAllText(sPublicKeyPath);
            General.ECCPublicKey eccPublicKey = General.DeserializeECCObject<General.ECCPublicKey>(sPublicKey);

            // Read the private key
            String sPrivateKey = File.ReadAllText(sPrivateKeyPath);
            General.ECCPrivateKey eccPrivateKey = General.DeserializeECCObject<General.ECCPrivateKey>(sPrivateKey);

            if (eccPublicKey.Curve != eccPrivateKey.Curve)
            {
                Console.WriteLine("\n[!] Both parties are not using the same curve..");
                return;
            }
            
            // Create the ECParameters object for the sender
            ECParameters ecSender = new ECParameters
            {
                Curve = ECCurve.CreateFromFriendlyName(eccPublicKey.Curve ?? String.Empty),
                Q = new ECPoint
                {
                    X = Convert.FromBase64String(eccPublicKey.X),
                    Y = Convert.FromBase64String(eccPublicKey.Y)
                }
            };
            ECDiffieHellman ecdhSender = CreateECDiffieHellmanFromECParameters(ecSender);
            
            // Create the ECParameters object for the recipient
            ECParameters ecRecipient = new ECParameters
            {
                Curve = ECCurve.CreateFromFriendlyName(eccPrivateKey.Curve ?? String.Empty),
#if NETFRAMEWORK
                Q = new ECPoint
                {
                    X = Convert.FromBase64String(eccPublicKey.X),
                    Y = Convert.FromBase64String(eccPublicKey.Y),
                },
#endif
                D = Convert.FromBase64String(eccPrivateKey.D)
            };
            ECDiffieHellman ecdhRecipient = CreateECDiffieHellmanFromECParameters(ecRecipient);

            // Get the shared secret
            Byte[] sharedSecret = ecdhRecipient.DeriveKeyMaterial(ecdhSender.PublicKey);

            // Print the shared secret
            Console.WriteLine("\n[+] Shared secret: " + String.Concat(sharedSecret.Select(x => x.ToString("X2"))));

            // Decrypt AES key material using ECDH shared secret
            Byte[] bDecompressedFile = new Byte[] { };
            try
            {
                Console.WriteLine("[>] Decrypting file contents..");
                General.AESKeyMaterial oDecryptedAESKeyMaterial =
                    AES.DecryptAESKeyMaterialWithSharedSecret(sharedSecret,
                        Convert.FromBase64String(oEncryptedFile.Key));
                Console.WriteLine("    |_ Decrypted AES key material with ECCurve shared secret");
                Byte[] bDecryptedFile = AES.DecryptAES(Convert.FromBase64String(oEncryptedFile.File),
                    oDecryptedAESKeyMaterial.Key, oDecryptedAESKeyMaterial.IV);
                Console.WriteLine("    |_ Decrypted compressed file");
                bDecompressedFile = General.DecompressByteArray(bDecryptedFile);
                Console.WriteLine("    |_ Decompressed file");
            }
            catch
            {
                Console.WriteLine("    |_ Failed to decrypt file, invalid key pair / tampered file");
                return;
            }
            
            
            // Verify signature
            Console.WriteLine("[>] Verifying SHA256 file hash with ECCurve");
            //if (!VerifyDataWithECDH(ecdhSender, bDecompressedFile, Convert.FromBase64String(oEncryptedFile.Signature)))
            if (!VerifyDataWithECDH( ecdhSender, bDecompressedFile, Convert.FromBase64String(oEncryptedFile.Signature)))
            {
                Console.WriteLine("    |_ Signature verification failed, file was tampered with");
                return;
            }
            else
            {
                Console.WriteLine("    |_ Signature OK");
            }
            
            // Write the file to disk
            Console.WriteLine("[>] Creating decrypted file..");
            String sDecryptedFilePath = String.Empty;
            if (sOutputDirectory == String.Empty)
            {
                sDecryptedFilePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + oEncryptedFile.FileName;
            }
            else
            {
                // Normalize the path, if it ends with a slash, remove it
                if (sOutputDirectory.EndsWith(Path.DirectorySeparatorChar.ToString()))
                {
                    sOutputDirectory = sOutputDirectory.Substring(0, sOutputDirectory.Length - 1);
                }
                sDecryptedFilePath = sOutputDirectory + Path.DirectorySeparatorChar + oEncryptedFile.FileName;
            }
            File.WriteAllBytes(sDecryptedFilePath, bDecompressedFile);
            Console.WriteLine("    |_ Path: " + sDecryptedFilePath);
        }

        // Sign data with ECCurve
        internal static Byte[] SignDataWithECDH(ECDiffieHellman ecdh, Byte[] data)
        {
            ECParameters ecParams = ecdh.ExportParameters(true);
            using (ECDsa ecdsa = ECDsa.Create())
            {
                ecdsa.ImportParameters(ecParams);
                return ecdsa.SignData(data, HashAlgorithmName.SHA256);
            }
        }
        
        // Verify data with ECCurve
        internal static Boolean VerifyDataWithECDH(ECDiffieHellman ecdh, Byte[] data, Byte[] signature)
        {
            ECParameters ecParams = ecdh.ExportParameters(false);
            using (ECDsa ecdsa = ECDsa.Create(ecParams))
            {
                return ecdsa.VerifyData(data, signature, HashAlgorithmName.SHA256);
            }
        }
        
        // Create ECDiffieHellman object from ECParameters
        internal static ECDiffieHellman CreateECDiffieHellmanFromECParameters(ECParameters ecParameters)
        {
            ECDiffieHellman ecdh = ECDiffieHellman.Create();
            ecdh.ImportParameters(ecParameters);
            return ecdh;
        }
    }
}