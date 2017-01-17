using System;
using System.Collections.Generic;
using System.Text;

namespace Shadowsocks.Model
{
    public class ServerSelectStrategy
    {
        public delegate bool FilterFunc(Server server, Server selServer); // return true if select the server
        private Random randomGennarator;
        private int lastSelectIndex;
        private DateTime lastSelectTime;
        private int lastUserSelectIndex;
        private const int MAX_CHANCE = 10000;
        private const int ERROR_PENALTY = MAX_CHANCE / 20;
        private const int CONNECTION_PENALTY = MAX_CHANCE / 100;
        private const int MIN_CHANCE = 10;

        public enum SelectAlgorithm
        {
            OneByOne,
            Random,
            LowLatency,
            LowException,
            SelectedFirst,
            Timer,
        }

        private struct ServerIndex
        {
            public int index;
            public Server server;
            public ServerIndex(int i, Server s)
            {
                index = i;
                this.server = s;
            }
        };
        private int lowerBound(List<double> data, double target)
        {
            int left = 0;
            int right = data.Count - 1;
            while (left < right)
            {
                int mid = (left + right) / 2;
                if (data[mid] >= target)
                    right = mid;
                else if (data[mid] < target)
                    left = mid + 1;
            }
            return left;
        }

        private double Algorithm2(ServerSpeedLog serverSpeedLog) // perfer less delay
        {
            if (serverSpeedLog.ErrorContinurousTimes >= 20)
                return 1;
            else if (serverSpeedLog.ErrorContinurousTimes >= 10)
                return MIN_CHANCE;
            else if (serverSpeedLog.AvgConnectTime < 0 && serverSpeedLog.TotalConnectTimes >= 3)
                return MIN_CHANCE;
            else if (serverSpeedLog.TotalConnectTimes < 1)
                return MAX_CHANCE;
            else
            {
                long avgConnectTime = serverSpeedLog.AvgConnectTime <= 0 ? 1 : serverSpeedLog.AvgConnectTime;
                if (serverSpeedLog.TotalConnectTimes >= 1 && serverSpeedLog.AvgConnectTime < 0)
                    avgConnectTime = 5000;
                long connections = serverSpeedLog.TotalConnectTimes - serverSpeedLog.TotalDisconnectTimes;
                double chance = MAX_CHANCE * 10.0 / avgConnectTime - connections * CONNECTION_PENALTY;
                if (chance > MAX_CHANCE) chance = MAX_CHANCE;
                chance -= serverSpeedLog.ErrorContinurousTimes * ERROR_PENALTY;
                if (chance < MIN_CHANCE) chance = MIN_CHANCE;
                return chance;
            }
        }

        private double Algorithm3(ServerSpeedLog serverSpeedLog) // perfer less error
        {
            if (serverSpeedLog.ErrorContinurousTimes >= 20)
                return 1;
            else if (serverSpeedLog.ErrorContinurousTimes >= 10)
                return MIN_CHANCE;
            else if (serverSpeedLog.AvgConnectTime < 0 && serverSpeedLog.TotalConnectTimes >= 3)
                return MIN_CHANCE;
            else if (serverSpeedLog.TotalConnectTimes < 1)
                return MAX_CHANCE;
            else
            {
                long avgConnectTime = serverSpeedLog.AvgConnectTime <= 0 ? 1 : serverSpeedLog.AvgConnectTime;
                if (serverSpeedLog.TotalConnectTimes >= 1 && serverSpeedLog.AvgConnectTime < 0)
                    avgConnectTime = 5000;
                long connections = serverSpeedLog.TotalConnectTimes - serverSpeedLog.TotalDisconnectTimes;
                double chance = MAX_CHANCE * 1.0 / (avgConnectTime / 500 + 1) - connections * CONNECTION_PENALTY;
                if (chance > MAX_CHANCE) chance = MAX_CHANCE;
                chance -= serverSpeedLog.ErrorContinurousTimes * ERROR_PENALTY;
                if (chance < MIN_CHANCE) chance = MIN_CHANCE;
                return chance;
            }
        }

        public int Select(List<Server> configs, int curIndex, int algorithm, FilterFunc filter, bool forceChange = false)
        {
            if (randomGennarator == null)
            {
                randomGennarator = new Random();
                lastSelectIndex = -1;
            }
            if (configs.Count <= lastSelectIndex || lastSelectIndex < 0 || !configs[lastSelectIndex].isEnable())
            {
                lastSelectIndex = -1;
                lastSelectTime = DateTime.Now;
                lastUserSelectIndex = -1;
            }
            if (lastUserSelectIndex != curIndex)
            {
                if (configs.Count > curIndex && curIndex >= 0 && configs[curIndex].isEnable())
                {
                    lastSelectIndex = curIndex;
                }
                lastUserSelectIndex = curIndex;
            }
            if (configs.Count > 0)
            {
                List<ServerIndex> serverList = new List<ServerIndex>();
                for (int i = 0; i < configs.Count; ++i)
                {
                    if (forceChange && lastSelectIndex == i)
                        continue;
                    if (configs[i].isEnable())
                    {
                        if (filter != null)
                        {
                            if (!filter(configs[i], lastSelectIndex < 0 ? null : configs[lastSelectIndex]))
                                continue;
                        }
                        serverList.Add(new ServerIndex(i, configs[i]));
                    }
                }
                if (forceChange && serverList.Count > 1)
                {
                    for (int i = 0; i < serverList.Count; ++i)
                    {
                        if (serverList[i].index == lastSelectIndex)
                        {
                            serverList.RemoveAt(i);
                            break;
                        }
                    }
                }
                if (serverList.Count == 0)
                {
                    int i = lastSelectIndex;
                    if (i >= 0 && i < configs.Count && configs[i].isEnable())
                        serverList.Add(new ServerIndex(i, configs[i]));
                }
                int serverListIndex = -1;
                if (serverList.Count > 0)
                {
                    if (algorithm == (int)SelectAlgorithm.OneByOne)
                    {
                        int selIndex = -1;
                        for (int i = 0; i < serverList.Count; ++i)
                        {
                            if (serverList[i].index == lastSelectIndex)
                            {
                                selIndex = i;
                                break;
                            }
                        }
                        if (selIndex != -1)
                        {
                            serverListIndex = serverList[(selIndex + 1) % serverList.Count].index;
                        }
                        else
                        {
                            serverListIndex = serverList[0].index;
                        }
                    }
                    else if (algorithm == (int)SelectAlgorithm.Random)
                    {
                        serverListIndex = randomGennarator.Next(serverList.Count);
                        serverListIndex = serverList[serverListIndex].index;
                    }
                    else if (algorithm == (int)SelectAlgorithm.LowException
                        || algorithm == (int)SelectAlgorithm.Timer)
                    {
                        if (algorithm == (int)SelectAlgorithm.Timer)
                        {
                            if ((DateTime.Now - lastSelectTime).TotalSeconds > 60 * 10)
                            {
                                lastSelectTime = DateTime.Now;
                            }
                            else
                            {
                                if (configs.Count > lastSelectIndex && lastSelectIndex >= 0 && configs[lastSelectIndex].isEnable() && !forceChange)
                                {
                                    return lastSelectIndex;
                                }
                            }
                        }
                        List<double> chances = new List<double>();
                        double lastBeginVal = 0;
                        foreach (ServerIndex s in serverList)
                        {
                            double chance = Algorithm3(s.server.ServerSpeedLog());
                            if (chance > 0)
                            {
                                chances.Add(lastBeginVal + chance);
                                lastBeginVal += chance;
                            }
                        }
                        {
                            double target = randomGennarator.NextDouble() * lastBeginVal;
                            serverListIndex = lowerBound(chances, target);
                            serverListIndex = serverList[serverListIndex].index;
                            lastSelectIndex = serverListIndex;
                            return serverListIndex;
                        }
                    }
                    else //if (algorithm == (int)SelectAlgorithm.LowLatency || algorithm == (int)SelectAlgorithm.SelectedFirst)
                    {
                        List<double> chances = new List<double>();
                        double lastBeginVal = 0;
                        foreach (ServerIndex s in serverList)
                        {
                            double chance = Algorithm2(s.server.ServerSpeedLog());
                            if (chance > 0)
                            {
                                chances.Add(lastBeginVal + chance);
                                lastBeginVal += chance;
                            }
                        }
                        if (algorithm == (int)SelectAlgorithm.SelectedFirst
                            && randomGennarator.Next(3) == 0
                            && configs[curIndex].isEnable())
                        {
                            for (int i = 0; i < serverList.Count; ++i)
                            {
                                if (curIndex == serverList[i].index)
                                {
                                    lastSelectIndex = curIndex;
                                    return curIndex;
                                }
                            }
                        }
                        {
                            double target = randomGennarator.NextDouble() * lastBeginVal;
                            serverListIndex = lowerBound(chances, target);
                            serverListIndex = serverList[serverListIndex].index;
                            lastSelectIndex = serverListIndex;
                            return serverListIndex;
                        }
                    }
                }
                lastSelectIndex = serverListIndex;
                return serverListIndex;
            }
            else
            {
                return -1;
            }
        }
    }
}
