﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fiddler;

namespace Huoyaoyuan.AdmiralRoom.Officer
{
    class Staff
    {
        public static Staff Current { get; } = new Staff();
        public Proxy Proxy { get; set; }
        public void Start(int port = 39175)
        {
            FiddlerApplication.Startup(port, FiddlerCoreStartupFlags.ChainToUpstreamGateway);
            FiddlerApplication.BeforeRequest += FiddlerApplication_BeforeRequest;
            System.Diagnostics.Debug.WriteLine(FiddlerApplication.IsStarted());

            Helper.RefreshIESettings($"localhost:{port}");
        }

        private void FiddlerApplication_BeforeRequest(Session oSession)
        {
            if (Proxy != null && Config.Current.EnableProxy)
            {
                oSession["X-OverrideGateway"] = $"[{Proxy.Host}]:{Proxy.Port}";
            }
        }

        public void Stop()
        {
            FiddlerApplication.BeforeRequest -= FiddlerApplication_BeforeRequest;
            FiddlerApplication.Shutdown();
        }
    }
}
