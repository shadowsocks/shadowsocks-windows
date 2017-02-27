/**************File added by Ian.May, 2016 Sept. 26*************/
using System;
using System.IO;

using System.Text;
using System.Net;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

using Shadowsocks.Controller;

namespace Shadowsocks.Model
{
    [Serializable]
    public class ConfigurationShadowFog : Configuration
    {
        public int transactionID;
        public int errorcode;
        public int expires_in;
        public string errormsg;
        public string access_token;

/****************************************************************************************************************************/
/****************** The following part tries to request the scheduler for getting a fogNode as proxy*************************/
/****************************************************************************************************************************/

        public static string GetFogNodeList(ClientUser User, bool isShadowFogStarted)
        {
            string tempBase64 = GetFogScheduler();
            byte[] tempBytes = Convert.FromBase64String(tempBase64);
            string temp = Encoding.UTF8.GetString(tempBytes);
            JObject schedulerInfo = JObject.Parse(temp);
            string schedulerAddr = (string)schedulerInfo["scheduler_addr"];
            /******************************************************/
             Console.WriteLine("Schdlr: " + schedulerAddr);
            /******************************************************/
            return GetFogCandidates(schedulerAddr, User, isShadowFogStarted);
        }

        public static string GetFogScheduler()
        {
            string content;
            HttpWebRequest myHttpWebRequest = null;
            HttpWebResponse myHttpWebResponse = null;
            string[] url = new string[] { "http://git.oschina.net/ShadowFogNetwork/Information/raw/master/sf-scheduler.json",
                                          "https://raw.githubusercontent.com/ShadowFog/Information/master/sf-scheduler.json",
                                          "https://bitbucket.org/shadowfogteam/infomation/raw/97464b30c4c693d74ed846ceac9f887909910c2f/sf-scheduler.json" };
            for (int url_cnt = 0; url_cnt < url.Length; url_cnt++)
            {
                if (null == myHttpWebResponse)
                {
                    myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url[url_cnt]);
                    myHttpWebRequest.AllowAutoRedirect = true; // be capable to handle 301/302/304/307 automatically
                    myHttpWebRequest.Referer = Regex.Match(url[url_cnt], "(?<=://).*?(?=/)").Value;
                    myHttpWebRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/53.0.2785.101 Safari/537.36";
                    try
                    {
                        myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
                    }
                    catch (Exception e)
                    {
                        //continue;
                        MessageBox.Show("Preloader Address: "+e.Message);
                    }
                }
                else break;
            }
            Stream receiveStream = myHttpWebResponse.GetResponseStream();
            Encoding encode = Encoding.GetEncoding("utf-8");
            StreamReader readStream = new StreamReader(receiveStream, encode);
            content = readStream.ReadToEnd();
            myHttpWebResponse.Close();
            readStream.Close();
            return content;
        }

        public static string GetFogCandidates(string SchedulerURL, ClientUser User, bool isShadowFogStarted)
        {
            //ClientUser's parameters are highly correlated with time, so these paras should be generate in real time;
            User.timeStamp = User.GetTimeStamp();
            User.nounce = User.GetNounce();
            User.transactionID = User.GetTransactionID();
            User.signature = User.GetSignature();

            string content;
            // TLS v1.2 required according to  nginx server configs
            //System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            HttpWebRequest myHttpWebRequest = null;
            HttpWebResponse myHttpWebResponse = null;

            myHttpWebRequest = (HttpWebRequest)WebRequest.Create(SchedulerURL + "?username=" + User.name + "&timestamp=" + User.timeStamp + "&nonce=" + User.nounce + "&transactionID=" + User.transactionID + "&signature=" + User.signature + "&clientstatus=" + isShadowFogStarted);

            Console.WriteLine("Username=" + User.name);
            Console.WriteLine("Nounce=" + User.nounce);
            Console.WriteLine("TransactionID=" + User.transactionID);
            Console.WriteLine("Clientstatus=" + isShadowFogStarted);

            // handle bad http responses from scheduler
            try
            {
                myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
            }
            catch (Exception e)
            {
                MessageBox.Show("Scheduler Access: " + e.Message);
                return null;
            }
            // end handle bad http reponses
            Stream receiveStream = myHttpWebResponse.GetResponseStream();
            Encoding encode = System.Text.Encoding.GetEncoding("utf-8");
            StreamReader readStream = new StreamReader(receiveStream, encode);
            content = readStream.ReadToEnd();
            myHttpWebResponse.Close();
            readStream.Close();
            return content;
        }
    }
}
