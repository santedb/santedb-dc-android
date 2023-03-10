using SanteDB.Client.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Client.Mobile
{
    internal class MauiBridgeProvider : IAppletHostBridgeProvider
    {
        private string _BridgeScript;

        public MauiBridgeProvider(string bridgeScript)
        {
            _BridgeScript = bridgeScript;
        }

        public string GetBridgeScript()
        {
            return _BridgeScript;
        }
    }
}
