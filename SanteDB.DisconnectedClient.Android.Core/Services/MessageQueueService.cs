/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
 * 
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
 * User: fyfej
 * Date: 2017-9-1
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SanteDB.DisconnectedClient.Synchronization;
using Android.App;
using Android.Content;
using SanteDB.Core.Diagnostics;
using SanteDB.DisconnectedClient.Services;

namespace SanteDB.DisconnectedClient.Android.Core.Services
{
    /// <summary>
    /// The message queue service represents an intent service which is tasked
    /// with the exhaustion of the inbound and outbound queues when internet
    /// connections are present. 
    /// </summary>
    /// <remarks>
    /// This service can be started manually, for example, when the outbound queue has an
    /// item which it needs to send, or when tasked to do so, or on a schedule.
    /// </remarks>
    [Service(Enabled = true)]
    [IntentFilter(new String[] { "org.santedb.santedb_mobile.inbox" })]
    public class InboundMessageQueueService : IntentService
    {
        // Tracer for the queue service
        private Tracer m_tracer = Tracer.GetTracer(typeof(InboundMessageQueueService));
        private Intent m_intent = new Intent("org.santedb.santedb_mobile.inbox");

        /// <summary>
        /// Handle the specified intent
        /// </summary>
        protected override void OnHandleIntent(Intent intent)
        {
            SanteDB.DisconnectedClient.ApplicationContext.Current.GetService<IQueueManagerService>().ExhaustOutboundQueues();
        }
        
    }

    /// <summary>
    /// Android service for exhausting the outbound queue
    /// </summary>
    [Service(Enabled = true)]
    [IntentFilter(new String[] { "org.santedb.santedb_mobile.outbox" })]
    public class OutboundMessageQueueService : IntentService
    {

        // Tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(OutboundMessageQueueService));
        private Intent m_intent = new Intent("org.santedb.santedb_mobile.outbox");

        /// <summary>
        /// Retry to exhaust queue on regular schedule
        /// </summary>
        public override void OnCreate()
        {
            base.OnCreate();

            // Retry to exhaust queue every half hour (should only occur when server is down)
            if (!IsAlarmSet())
            {
                var alarm = (AlarmManager)this.GetSystemService(Context.AlarmService);
                var pendingServiceIntent = PendingIntent.GetService(this, 0, this.m_intent, PendingIntentFlags.CancelCurrent);
                alarm.SetRepeating(AlarmType.Rtc, 0, AlarmManager.IntervalHalfHour, pendingServiceIntent);
            }

        }

        /// <summary>
        /// Handle the intent
        /// </summary>
        protected override void OnHandleIntent(Intent intent)
        {
            SanteDB.DisconnectedClient.ApplicationContext.Current.GetService<IQueueManagerService>().ExhaustOutboundQueues();
        }
        
        /// <summary>
        /// Set an alarm to exhaust the queue when available
        /// </summary>
        /// <returns></returns>
        private bool IsAlarmSet()
        {
            return PendingIntent.GetBroadcast(this, 0, this.m_intent, PendingIntentFlags.NoCreate) != null;
        }
        
    }
}