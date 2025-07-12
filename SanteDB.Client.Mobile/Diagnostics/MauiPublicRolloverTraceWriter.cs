/*
 * Portions Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 * Portions Copyright 2019-2024 SanteSuite Contributors (See NOTICE)
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: trevor
 * Date: 2023-4-19
 */
using Antlr4.Runtime.Misc;
using SanteDB.Core;
using SanteDB.Core.Diagnostics.Tracing;
using SanteDB.Core.Security;
using System.Collections.Generic;
using System.Diagnostics.Tracing;

namespace SanteDB.Client.Mobile.Diagnostics
{
    /// <summary>
    /// Rollover trace listener which interacts with the operating system provider to demand permission to access a public location
    /// </summary>
    public class MauiPublicRolloverTraceWriter : RolloverTextWriterTraceWriter
    {
        public MauiPublicRolloverTraceWriter(EventLevel filter, string fileName, IDictionary<string, EventLevel> sources) : base(filter, fileName, sources)
        {
        }

        /// <summary>
        /// Validate write permission 
        /// </summary>
        private bool ValidateWritePermission()
        {
            var osService = ApplicationServiceContext.Current.GetService<IPlatformSecurityProvider>();
            return osService.DemandPlatformServicePermission(PlatformServicePermission.ExternalMedia);
        }

        /// <inheritdoc/>
        public override void TraceEvent(EventLevel level, string source, string format, params object[] args)
        {
            if (this.ValidateWritePermission())
            {
                base.TraceEvent(level, source, format, args);
            }
        }

        /// <inheritdoc/>
        public override void TraceEventWithData(EventLevel level, string source, string message, object[] data)
        {
            if (this.ValidateWritePermission())
            {
                base.TraceEventWithData(level, source, message, data);
            }
        }
    }
}