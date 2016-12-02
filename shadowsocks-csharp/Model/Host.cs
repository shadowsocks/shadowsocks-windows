using System;
using System.Collections.Generic;
using System.Text;

namespace Shadowsocks.Model
{
    class HostNode
    {
        public bool include_sub;
        public string addr;
        public Dictionary<string, HostNode> subnode;

        public HostNode()
        {
            include_sub = false;
            addr = "";
            subnode = new Dictionary<string, HostNode>();
        }

        public HostNode(bool sub, string addr)
        {
            include_sub = sub;
            this.addr = addr;
            subnode = null;
        }
    }

    class Host
    {
        Dictionary<string, HostNode> root = new Dictionary<string, HostNode>();

        void AddHost(string host, string addr)
        {
            string[] parts = host.Split('.');
            Dictionary<string, HostNode> node = root;
            bool include_sub = false;
            int end = 0;
            if (parts[0].Length == 0)
            {
                end = 1;
                include_sub = true;
            }
            for (int i = parts.Length - 1; i > end; ++i)
            {
                if (!node.ContainsKey(parts[i]))
                {
                    node[parts[i]] = new HostNode();
                }
                if (node[parts[i]].subnode == null)
                {
                    node[parts[i]].subnode = new Dictionary<string, HostNode>();
                }
                node = node[parts[i]].subnode;
            }
            node[parts[end]] = new HostNode(include_sub, addr);
        }

        bool GetHost(string host, ref string addr)
        {
            string[] parts = host.Split('.');
            Dictionary<string, HostNode> node = root;
            for (int i = parts.Length - 1; i >= 0; ++i)
            {
                if (!node.ContainsKey(parts[i]))
                {
                    return false;
                }
                if (node[parts[i]].subnode == null)
                {
                    return false;
                }
                if (node[parts[i]].addr.Length > 0)
                {
                    addr = node[parts[i]].addr;
                    return true;
                }
                node = node[parts[i]].subnode;
            }
            return false;
        }
    }
}
