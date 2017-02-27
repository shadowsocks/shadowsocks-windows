using System;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.IO;

using Shadowsocks.Controller;
using Newtonsoft.Json;

namespace Shadowsocks.Model
{
    [Serializable]
    public class ClientUser
    {
        public string name;
        public string pswdHashed;
        public bool isRemeberUser;

        public int timeStamp;
        public string nounce;
        public int transactionID; // preserved for multi-threads
        public string signature;

        private static string USER_FILE = "user-info.json";

        public int GetTimeStamp()
        {
            DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            return (int)(DateTime.Now - startTime).TotalSeconds;
        }
        public string GetNounce()
        {
            return "nounce";
        }
        public int GetTransactionID()
        {
            return 50;
        }
        public string GetSignature()
        {
            return HMAC_SHA256("shadowfog" + timeStamp + nounce + name, pswdHashed);
        }


        public static string SHA256(string str)
        {
            byte[] SHA256Data = Encoding.UTF8.GetBytes(str);
            SHA256Managed Sha256 = new SHA256Managed();
            byte[] Result = Sha256.ComputeHash(SHA256Data);
            string hashstring = string.Empty;
            foreach (byte x in Result)
            {
                hashstring += string.Format("{0:x2}", x);
            }
            return hashstring;
        }

        public static string HMAC_SHA256(string plainText, string hashedPSWD)
        {
            byte[] HMAC_SHA256Salt = Encoding.UTF8.GetBytes(hashedPSWD);
            var hmacsha256 = new HMACSHA256(HMAC_SHA256Salt);
            hmacsha256.ComputeHash(Encoding.UTF8.GetBytes(plainText));
            string hashstring = string.Empty;
            foreach (byte x in hmacsha256.Hash)
            {
                hashstring += string.Format("{0:x2}", x);
            }
            return hashstring;
        }

/****************************************************************************************************************************/
/****************** The following part tries to save and load extra info in file for shadowfog users ************************/
/****************************************************************************************************************************/

        public static ClientUser Load()
        {
            try
            {
                string userInfo = File.ReadAllText(USER_FILE);
                ClientUser User = JsonConvert.DeserializeObject<ClientUser>(userInfo);
                return User;
            }
            catch (Exception e)
            {
                if (!(e is FileNotFoundException))
                    Logging.LogUsefulException(e);
                return new ClientUser();
            }
        }

        public static void Save(ClientUser User)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(File.Open(USER_FILE, FileMode.Create)))
                {
                    string jsonString = JsonConvert.SerializeObject(User, Formatting.Indented);
                    sw.Write(jsonString);
                    sw.Flush();
                }
            }
            catch (IOException e)
            {
                Logging.LogUsefulException(e);
            }
        }

    }
}
