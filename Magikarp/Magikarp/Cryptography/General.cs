using System;
using System.IO;
using System.IO.Compression;
using System.Xml.Serialization;

namespace Cryptography
{
    public class General
    {
        // Definition for an ECC public key object
        public class ECCPublicKey
        {
            public String Name { get; set; }
            public DateTime UtcTimestamp { get; set; }
            public String Curve { get; set; }
            public String X { get; set; }
            public String Y { get; set; }
        }
        
        // Definition for an ECC private key object
        public class ECCPrivateKey
        {
            public String Name { get; set; }
            public DateTime UtcTimestamp { get; set; }
            public String Curve { get; set; }
            public String D { get; set; }
        }
        
        // Definition for encrypted file object
        public class EncryptedFile
        {
            public String FileName { get; set; }
            public DateTime UtcTimestamp { get; set; }
            public String Signature { get; set; }
            public String Key { get; set; }
            public String File { get; set; }
        }
        
        // Definition for AES key material
        public class AESKeyMaterial
        {
            public Byte[] Key  { get; set; }
            public Byte[] IV { get; set; }
        }
        
        // Serialize ECC object to XML
        internal static string SerializeECCObject(Object obj, bool bIndented = true)
        {
            XmlSerializer serializer = new XmlSerializer(obj.GetType());
            StringWriter textWriter = new StringWriter();
            serializer.Serialize(textWriter, obj);
            return textWriter.ToString();
        }
        
        // Deserialize ECC object from XML
        internal static T DeserializeECCObject<T>(string xml)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            StringReader textReader = new StringReader(xml);
            return (T)serializer.Deserialize(textReader);
        }
        
        // Compress a byte array
        internal static Byte[] CompressByteArray(Byte[] bInput)
        {
            using (MemoryStream oMemoryStream = new MemoryStream())
            {
                using (GZipStream oGZipStream = new GZipStream(oMemoryStream, CompressionMode.Compress))
                {
                    oGZipStream.Write(bInput, 0, bInput.Length);
                }
                return oMemoryStream.ToArray();
            }
        }
        
        // Decompress a byte array
        internal static Byte[] DecompressByteArray(Byte[] bInput)
        {
            using (MemoryStream oMemoryStream = new MemoryStream())
            {
                using (MemoryStream oInputMemoryStream = new MemoryStream(bInput))
                {
                    using (GZipStream oGZipStream = new GZipStream(oInputMemoryStream, CompressionMode.Decompress))
                    {
                        oGZipStream.CopyTo(oMemoryStream);
                    }
                }
                return oMemoryStream.ToArray();
            }
        }
    }
}