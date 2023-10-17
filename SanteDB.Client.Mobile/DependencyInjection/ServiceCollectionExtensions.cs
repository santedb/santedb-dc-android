using SanteDB.Client.Mobile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSanteDBBridge(this IServiceCollection services) 
            => services.AddSingleton<SanteDBBridge>();
    }
}
