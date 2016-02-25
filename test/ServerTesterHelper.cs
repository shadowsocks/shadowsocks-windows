using System;
using System.Threading;

using Shadowsocks.Model;
using Shadowsocks.Controller.Service;

namespace test
{
    class ServerTesterHelper
    {
        public long Speed;
        public Exception Error;

        private bool finish = false;
        private int percent = -1;

        public bool Test(Server server)
        {
            ServerTester tester = new ServerTester(server);
            tester.Completed += tester_Completed;
            tester.Progress += tester_Progress;
            tester.Start();
            // wait for test finish
            while (!finish)
            {
                Thread.Sleep(100);
            }
            return Error == null;
        }

        private void tester_Progress(object sender, ServerTesterProgressEventArgs e)
        {
            if (e.Total > 0)
            {
                percent = (int)((e.Download * 100) / e.Total);
                // random download size
                int threshold = new Random().Next(30, 100);
                if (percent > threshold)
                {
                    e.Cancel = true;
                }
            }
        }

        private void tester_Completed(object sender, ServerTesterEventArgs e)
        {
            Speed = e.DownloadSpeed;
            Error = e.Error;
            finish = true;
        }
    }
}
