using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.IO;  
using System.IO.Compression;
using Shadowsocks.Controller.Service;
using Shadowsocks.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;


namespace Shadowsocks.Controller
{
    class OnlineConfigController
    {
        private string _cookie = null;
        public string Url = "";
        public string Type = "unknown";
        public string LocalPublickey = "";
        public string LocalPrivateKey = "";
        public string RemotePubkey = "";
        private KeyExchangeResponse KeyExchangeRes = null;
        //public RSAHandler _rsa;
        
        public OnlineConfigController(string Url)
        {
            this.Url = Url;
            string[] Prefix = Url.Split(new char[3] { ':', '/', '/' });
            switch (Prefix[0].ToLower())
            {
                case "http":
                    Type = "http";
                    break;
                case "https":
                    Type = "https";
                    break;
                default:
                    this.Url = "";
                    this.Type = "unknown";
                    break;
            }
            RSAHandler.GenerateKeyPair();
            this.LocalPrivateKey = System.Environment.CurrentDirectory + "\\keys\\local.private.pem";
            this.LocalPublickey = System.Environment.CurrentDirectory + "\\keys\\local.public.pem";
        }
        public Server[] GetOnlineConfig()
        {
            try
            {
                KeyExchangeResponse KeyExchangeResponse = this.KeyExchange(this.Url);
                GetConfigResponse ConfigResponse = this.GetConfig();
                return ConfigResponse.Config;
            }
            catch
            {
                throw;
            }
            
        }
        private KeyExchangeResponse KeyExchange(string Url)
        {
            if(this.Url=="" || this.Type == "unknown")
            {
                throw new ArgumentException("Invalid Url");
            }
            StreamReader LocalPublicKeyStream = new StreamReader(this.LocalPublickey);
            string LocalPublicKey = LocalPublicKeyStream.ReadToEnd();
            LocalPublicKeyStream.Dispose();
            HttpWebResponse res = CreatePostHttpResponse(this.Url, LocalPublicKey, _cookie);
            if (res.StatusCode != HttpStatusCode.OK)
            {
                throw new ArgumentException("Invalid Status Code");
            }

            //string[] x = res.Headers.AllKeys;
            
            string ResponseContent = "";
            using (StreamReader sr = new StreamReader(res.GetResponseStream()))
            {
                ResponseContent = sr.ReadToEnd();
            }
            this._cookie = res.Headers.Get("Set-Cookie");
            try
            {
                ResponseContent = RSAHandler.DecryptRSA(ResponseContent, this.LocalPrivateKey);
            }
            catch
            {
                throw;
            }
            StringReader reader = new StringReader(ResponseContent);
            JsonSerializer serializer = new JsonSerializer();
            KeyExchangeResponse response = (KeyExchangeResponse)serializer.Deserialize(new JsonTextReader(reader), typeof(KeyExchangeResponse));
            this.KeyExchangeRes = response;
            if(File.Exists(System.Environment.CurrentDirectory + "\\keys\\" + response.FingerPrint + ".pem"))
            {
                StreamReader stream = new StreamReader(System.Environment.CurrentDirectory + "\\keys\\" + response.FingerPrint + ".pem");
                string LocalVersion = stream.ReadToEnd();
                
                if (LocalVersion != response.ServerPublicKey)
                {
                    throw new ArgumentException("Server Public Key Mismatch");
                }

            }
            else
            {
                FileStream LocalVersion = File.Create(System.Environment.CurrentDirectory + "\\keys\\" + response.FingerPrint + ".pem");
                LocalVersion.Write(System.Text.Encoding.Default.GetBytes(response.ServerPublicKey), 0, response.ServerPublicKey.Length);
                LocalVersion.Close();
                LocalVersion.Dispose();
            }
            this.RemotePubkey = System.Environment.CurrentDirectory + "\\keys\\" + response.FingerPrint + ".pem";
            return response;
        }
        
        private GetConfigResponse GetConfig()
        {
            GetConfigRequest Request = new GetConfigRequest();
            Request.decrypted_string = this.KeyExchangeRes.VerifyString;
            char[] tempChr = new char[32];
            Random rand = new Random();
            for(int i = 0; i < 32; i++)
            {
                tempChr[i] = (char)rand.Next(61, 122);
            }
            Request.verify_string = new string(tempChr);
            string RequestContent = JsonConvert.SerializeObject(Request);
            string PostData = RSAHandler.EncryptRSA(RequestContent, this.RemotePubkey);
            HttpWebResponse res;
            string ResponseContent = "";
            try
            {
                res = CreatePostHttpResponse(this.Url, PostData, this._cookie);
                if (res.StatusCode != HttpStatusCode.OK)
                {
                    throw new ArgumentException("Invalid Status Code");
                }

                using (StreamReader sr = new StreamReader(res.GetResponseStream()))
                {
                    ResponseContent = sr.ReadToEnd();
                }
            }
            catch
            {
                throw;
            }
            ResponseContent = RSAHandler.DecryptRSA(ResponseContent, this.LocalPrivateKey);
            StringReader reader = new StringReader(ResponseContent);
            JsonSerializer serializer = new JsonSerializer();
            GetConfigResponse response = (GetConfigResponse)serializer.Deserialize(new JsonTextReader(reader), typeof(GetConfigResponse));
            if (response.VerifyString != Request.verify_string)
            {
                throw new ArgumentException("Server Verify Fail");
            }
            for(int i = 0; i < response.Config.Length;i++)
            {
                response.Config[i].provider = response.Provider;
                response.Config[i].fingerprint = this.KeyExchangeRes.FingerPrint;
            }
            return response;
        }

        public static HttpWebResponse CreatePostHttpResponse(string url, string postData,string cookies)
        {
            Encoding requestEncoding = Encoding.UTF8;
            string UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/46.0.2490.86 Safari/537.36";
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException("url");
            }
            if (requestEncoding == null)
            {
                throw new ArgumentNullException("requestEncoding");
            }
            HttpWebRequest request = null;
            if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                request = WebRequest.Create(url) as HttpWebRequest;
                request.ProtocolVersion = HttpVersion.Version10;
            }
            else
            {
                request = WebRequest.Create(url) as HttpWebRequest;
            }
            
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            
            
                request.UserAgent = UserAgent;
            if (cookies != null)
            {
                request.Headers.Add(HttpRequestHeader.Cookie, cookies);
            }
            if (postData!="")
            {

                byte[] data = requestEncoding.GetBytes(postData);
                try
                {
                    using (Stream stream = request.GetRequestStream())
                    {

                        stream.Write(data, 0, data.Length);
                    }

                }catch(Exception e)
                {
                    throw new ArgumentException(e.Message);
                }
                
            }
            HttpWebResponse res = null;
            try
            {
                res = request.GetResponse() as HttpWebResponse;
            }
            catch (Exception e)
            {
                throw new ArgumentException(e.Message);
            }
            return res;
        }

        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true;
        }
    }
}

