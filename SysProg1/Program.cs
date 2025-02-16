﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace SysProg1
{
    internal class Program
    {
        public static readonly string localhost = "127.0.0.1";
        public static readonly uint serverPort = 8080;
        public static readonly string directory = @"../../root/";

        static void Main(string[] args)
        {
            Thread serverThread = new Thread(() =>
            {
                HttpServer server = new HttpServer(localhost, serverPort, directory, new LRUCache<string, byte[]>(10));
                server.Launch();
            });

            serverThread.Priority = ThreadPriority.Highest;

            serverThread.Start();
        }
    }
}
