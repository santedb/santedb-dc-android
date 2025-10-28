using Microsoft.VisualBasic;
using SanteDB.Client.UserInterface;
using SanteDB.Core.Configuration;
using SanteDB.Core.Data;
using SanteDB.Core.Data.Backup;
using SanteDB.Core.i18n;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.AMI.Operation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Client.Mobile
{
    /// <summary>
    /// Wrapped backup provider
    /// </summary>
    public class MauiBackupProvider : DefaultBackupManager
    {
        private readonly IUserInterfaceInteractionProvider m_userInterfaceInteraction;
        private readonly IPolicyEnforcementService m_pepService;

        public MauiBackupProvider(IUserInterfaceInteractionProvider userInterfaceInteraction, IConfigurationManager configurationManager, IServiceManager serviceManager, IPolicyEnforcementService pepService, ILocalizationService localizationService, IPlatformSecurityProvider platformSecurityProvider) : base(configurationManager, serviceManager, pepService, localizationService, platformSecurityProvider)
        {
            this.m_userInterfaceInteraction = userInterfaceInteraction;
            this.m_pepService = pepService;
        }

        public override IEnumerable<IBackupDescriptor> GetBackupDescriptors(BackupMedia media)
        {
            switch (media)
            {
                case BackupMedia.Private:
                case BackupMedia.Public:
                    return base.GetBackupDescriptors(media);
                case BackupMedia.ExternalPublic:
                    this.m_pepService.Demand(PermissionPolicyIdentifiers.ManageBackups);

                    using (var backupStream = this.m_userInterfaceInteraction.SelectFile(UserMessages.ISOLATED_STORAGE_SELECT_BACKUP_FILE, "application/octet-stream", null))
                    {
                        if (backupStream == null)
                        {
                            return new IBackupDescriptor[0];
                        }
                        return new IBackupDescriptor[] { base.GetBackupDescriptorFromStream(backupStream) };
                    }
                default:
                    throw new NotSupportedException();
            }
        }

        public override IBackupDescriptor Backup(BackupMedia media, string password = null)
        {
            switch (media)
            {
                case BackupMedia.Private:
                case BackupMedia.Public:
                    return base.Backup(media, password);
                case BackupMedia.ExternalPublic:
                    if (this.AllowPublicBackups)
                    {
                        this.m_pepService.Demand(PermissionPolicyIdentifiers.CreateAnyBackup);
                    }
                    else
                    {
                        throw new InvalidOperationException(String.Format(ErrorMessages.POLICY_PREVENTS_ACTION, SecurityPolicyIdentification.AllowPublicBackups));
                    }

                    using (var ms = new TemporaryFileStream())
                    {
                        base.BackupToStream(ms, password);
                        ms.Flush();
                        ms.Seek(0, SeekOrigin.Begin);
                        var backupDescriptor = this.m_userInterfaceInteraction.SaveFile(
                            Path.GetDirectoryName(this.Configuration.PublicBackupLocation),
                            $"{DateTime.Now.Ticks}.{BACKUP_EXTENSION}",
                            ms); // This should give us permission to this file
                        ms.Seek(0, SeekOrigin.Begin);
                        return base.GetBackupDescriptorFromStream(ms);
                    }
                default:
                    throw new NotSupportedException();
            }
        }

        public override IBackupDescriptor GetBackupInternal(BackupMedia media, string backupDescriptorLabel)
        {
            switch (media)
            {
                case BackupMedia.Private:
                case BackupMedia.Public:
                    return base.GetBackup(media, backupDescriptorLabel);
                case BackupMedia.ExternalPublic:
                    this.m_pepService.Demand(PermissionPolicyIdentifiers.ManageBackups);

                    using (var backupStream = this.m_userInterfaceInteraction.SelectFile(UserMessages.ISOLATED_STORAGE_SELECT_BACKUP_FILE, "application/octet-stream", null))
                    {
                        if (backupStream == null)
                        {
                            return null;
                        }
                        return base.GetBackupDescriptorFromStream(backupStream);
                    }
                default:
                    throw new NotSupportedException();
            }
        }

        public override IBackupDescriptor GetBackup(string backupDescriptorLabel, out BackupMedia locatedOnMedia)
        {
            throw new NotSupportedException();
        }


        public override bool HasBackup(BackupMedia media)
        {
            switch (media)
            {
                case BackupMedia.Private:
                    return base.HasBackup(media);
                // TODO: Figure out a way to implement this without bothering the user to select a file?
                default:
                    throw new NotSupportedException();

            }
        }

        public override bool Restore(BackupMedia media, string backupDescriptorLabel, string password = null)
        {
            switch (media)
            {
                case BackupMedia.Private:
                case BackupMedia.Public:
                    return base.Restore(media, backupDescriptorLabel, password);
                case BackupMedia.ExternalPublic:
                    this.m_pepService.Demand(PermissionPolicyIdentifiers.ManageBackups);

                    using (var backupStream = this.m_userInterfaceInteraction.SelectFile(UserMessages.ISOLATED_STORAGE_SELECT_BACKUP_FILE, "application/octet-stream", null))
                    {
                        return base.RestoreFromStream(backupStream, password);
                    }
                default:
                    throw new NotSupportedException();
            }
        }

        public override void RemoveBackup(BackupMedia media, string backupDescriptorLabel)
        {
            switch (media)
            {
                case BackupMedia.Private:
                case BackupMedia.Public:
                    base.RemoveBackup(media, backupDescriptorLabel);
                    break;
                default:
                    throw new NotSupportedException(UserMessages.USE_SYSTEM_FUNCTION_TO_PERFORM_ACTION);

            }
        }
    }
}
