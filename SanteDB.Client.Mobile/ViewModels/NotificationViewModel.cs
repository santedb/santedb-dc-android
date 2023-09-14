using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Client.Mobile.ViewModels
{
    public class NotificationViewModel
    {
        public string Identifier { get; set; }
        public string Message { get; set; }
        public float ProgressIndicator { get; set; }
        public DateTimeOffset LastUpdated { get; set; }
    }
}
