using SanteDB.Client.UserInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Client.Mobile
{
    internal class MauiInteractionProvider : IUserInterfaceInteractionProvider
    {
        public string ServiceName => "SanteDB Multiplatform Interaction Provider";

        StartupPage _StartupPage;

        public MauiInteractionProvider(StartupPage startupPage)
        {
            _StartupPage = startupPage;
        }

        public void Alert(string message)
        {
            
        }

        public bool Confirm(string message)
        {
            return false;
        }

        public string Prompt(string message, bool maskEntry = false)
        {
            return null;
        }

        public void SetStatus(string statusText, float progressIndicator)
            => SetStatus(string.Empty, statusText, progressIndicator);

        public void SetStatus(string taskIdentifier, string statusText, float progressIndicator)
        {
            //throw new NotImplementedException();
        }
    }
}
