using System;
using System.Collections.Generic;

namespace Commands
{
    internal class Arguments
    {
        public enum ECCurveType
        {
            nistP256,
            nistP384,
            nistP521
        }
        
        internal static List<CmdLine.ArgOption> lArgOptions = new List<CmdLine.ArgOption>()
        {
            new CmdLine.ArgOption
            {
                Name = "bHelp",
                LongName = "Help",
                ShortName = "h",
                ArgumentType = CmdLine.ValueType.Boolean,
                HelpText = "Print help text",
            },
            new CmdLine.ArgOption
            {
                Name = "bGenerate",
                LongName = "Generate",
                ShortName = "g",
                ArgumentType = CmdLine.ValueType.Boolean,
                HelpText = "Generate ECC key pair",
            },
            new CmdLine.ArgOption
            {
                Name = "bEncrypt",
                LongName = "Encrypt",
                ShortName = "e",
                ArgumentType = CmdLine.ValueType.Boolean,
                HelpText = "Encrypt file",
            },
            new CmdLine.ArgOption
            {
                Name = "bDecrypt",
                LongName = "Decrypt",
                ShortName = "d",
                ArgumentType = CmdLine.ValueType.Boolean,
                HelpText = "Decrypt file",
            },
            new CmdLine.ArgOption
            {
                Name = "sName",
                LongName = "Name",
                ShortName = "n",
                ArgumentType = CmdLine.ValueType.String,
                HelpText = "Name of the key pair",
            },
            new CmdLine.ArgOption
            {
                Name = "eKeyType",
                LongName = "KeyType",
                ShortName = "kt",
                ArgumentType = CmdLine.ValueType.Enum,
                EnumValidator = typeof(ECCurveType),
                HelpText = "Key type (nistP256, nistP384, nistP521)",
            },
            new CmdLine.ArgOption
            {
                Name = "sFile",
                LongName = "File",
                ShortName = "f",
                ArgumentType = CmdLine.ValueType.String,
                HelpText = "File to encrypt/decrypt",
            },
            new CmdLine.ArgOption
            {
                Name = "sOutputFolder",
                LongName = "OutputFolder",
                ShortName = "o",
                ArgumentType = CmdLine.ValueType.String,
                HelpText = "Output folder for result file",
            },
            new CmdLine.ArgOption
            {
                Name = "sPublicKey",
                LongName = "PublicKey",
                ShortName = "pub",
                ArgumentType = CmdLine.ValueType.String,
                HelpText = "Public key file of the recipient",
            },
            new CmdLine.ArgOption
            {
                Name = "sPrivateKey",
                LongName = "PrivateKey",
                ShortName = "priv",
                ArgumentType = CmdLine.ValueType.String,
                HelpText = "Private key file of the sender",
            },
        };
        
        internal static void PrintAppHelp()
        {
            // Print automated help text
            Console.WriteLine("\nUsage: Magikarp.exe [options]\n");
            CmdLine.PrintHelp(lArgOptions);
            
            // Generate ECC key pair
            Console.WriteLine("\n# Generate ECC key pair in the current or specified folder");
            Console.WriteLine("Magikarp.exe /g /n:b33f /kt:nistP256 [/o:C:\\Some\\Path]\n");
            
            // Encrypt data
            Console.WriteLine("# Encrypt file in the current or specified folder");
            Console.WriteLine("Magikarp.exe /e /f:some.file /pub:bob.pub /priv:alice.key [/o:/Some/Path]\n");
            
            // Decrypt data
            Console.WriteLine("# Decrypt file in the current or specified folder");
            Console.WriteLine("Magikarp.exe /d /f:some.file /pub:alice.pub /priv:bob.key [/o:C:\\Some\\Path]\n");
        }
        
        internal static void PrintBanner()
        {
            Console.WriteLine(@"               /`·.¸        ");
            Console.WriteLine(@"              /¸...¸`:·     ");
            Console.WriteLine(@" Magi     ¸.·´  ¸   `·.¸.·´)");
            Console.WriteLine(@"   Karp  : © ):´;      ¸  { ");
            Console.WriteLine(@"          `·.¸ `·  ¸.·´\`·¸)");
            Console.WriteLine(@"     ~b33f    `\\´´\¸.·´    ");
        }
    }
}