using System;
using System.Collections.Generic;
using Commands;
using Cryptography;

internal class Magikarp
{
    public static void Main(String[] args)
    {
        // Print banner
        Arguments.PrintBanner();
        
        // Parse the arguments passed to the application
        IDictionary<String, Object> options = null;
        try
        {
            options = CmdLine.ParseArguments(args, Arguments.lArgOptions);
        } catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return;
        }
        
        if (options.Count == 0 || options.ContainsKey("bHelp"))
        {
            Arguments.PrintAppHelp();
        }
        else
        {
            if (options.ContainsKey("bGenerate") && options.ContainsKey("sName") && options.ContainsKey("eKeyType"))
            {
                // Generate a key pair
                if (options.ContainsKey("sOutputFolder"))
                {
                    ECC.GenerateKeyPair(
                        options["sName"].ToString(),
                        (Arguments.ECCurveType)Enum.Parse(typeof(Arguments.ECCurveType), options["eKeyType"].ToString()),
                        options["sOutputFolder"].ToString()
                    );
                }
                else
                {
                    ECC.GenerateKeyPair(
                        options["sName"].ToString(),
                        (Arguments.ECCurveType)Enum.Parse(typeof(Arguments.ECCurveType), options["eKeyType"].ToString())
                    );
                }
                return;
            }
            if (options.ContainsKey("bEncrypt") && options.ContainsKey("sFile") && options.ContainsKey("sPrivateKey") && options.ContainsKey("sPublicKey"))
            {
                // Encrypt a file
                if (options.ContainsKey("sOutputFolder")) {
                    ECC.EncryptFile(
                        options["sFile"].ToString(),
                        options["sPrivateKey"].ToString(),
                        options["sPublicKey"].ToString(),
                        options["sOutputFolder"].ToString()
                    );
                }
                else
                {
                    ECC.EncryptFile(
                        options["sFile"].ToString(),
                        options["sPrivateKey"].ToString(),
                        options["sPublicKey"].ToString()
                    );
                }
                return;
            }
            if (options.ContainsKey("bDecrypt") && options.ContainsKey("sFile") && options.ContainsKey("sPrivateKey") && options.ContainsKey("sPublicKey"))
            {
                // Decrypt a file
                if (options.ContainsKey("sOutputFolder"))
                {
                    ECC.DecryptFile(
                        options["sFile"].ToString(),
                        options["sPrivateKey"].ToString(),
                        options["sPublicKey"].ToString(),
                        options["sOutputFolder"].ToString()
                    );
                }
                else
                {
                    ECC.DecryptFile(
                        options["sFile"].ToString(),
                        options["sPrivateKey"].ToString(),
                        options["sPublicKey"].ToString()
                    );
                }
            }
            else
            {
                Console.WriteLine("\n[!] Missing required arguments..\n");
                Arguments.PrintAppHelp();
            }
        }
    }
}