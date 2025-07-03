using Microsoft.Maui.Controls;
using SanteDB.Client.UserInterface;
using SanteDB.Core;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace SanteDB.Client.Mobile;

#nullable enable

/// <summary>
/// Displays a message to the user to quit the application and relaunch it.
/// </summary>
public partial class RestartPage : ContentPage, IQueryAttributable
{
    readonly ILocalizationService? _LocalizationService;
    
    /// <summary>
    /// Instantiates the <see cref="RestartPage"/> using <see cref="ApplicationServiceContext.Current" />.
    /// </summary>
    public RestartPage()
        : this(ApplicationServiceContext.Current as MauiApplicationContext)
    {

    }

    /// <summary>
    /// Instantiates the <see cref="RestartPage"/> using the specified <paramref name="serviceContext"/>.
    /// </summary>
    /// <param name="serviceContext">The service context to reference for this restart page.</param>
    public RestartPage(MauiApplicationContext? serviceContext)
	{
        if (null != serviceContext)
            _LocalizationService = SanteDB.Core.ApplicationServiceContext.GetService<ILocalizationService>(serviceContext); //Need to fully qualify because MEDI and ASC have the same extension method signature.

		InitializeComponent();
	}

    /// <summary>
    /// Support routing infrastructure.
    /// </summary>
    /// <param name="query">Query parameters</param>
    /// <remarks>
    /// Use constants for the value of the &quot;reason&quot;.
    /// </remarks>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var reason = "default";

        if (query.TryGetValue("reason", out var reasonobj))
        {
            if (reasonobj is string reasonstr)
            {
                if (Constants.REASONKEY_FILECONFIGURATION.Equals(reasonstr, StringComparison.InvariantCultureIgnoreCase))
                    reason = "configuration";
                else if (Constants.REASONKEY_INITIALCONFIGURATION.Equals(reasonstr, StringComparison.InvariantCultureIgnoreCase))
                    reason = "initial";
                else if (Constants.REASONKEY_UPDATE.Equals(reasonstr, StringComparison.InvariantCultureIgnoreCase))
                    reason = "update";
                else if (Constants.REASONKEY_RESTORE.Equals(reasonstr, StringComparison.InvariantCultureIgnoreCase))
                    reason = "backupRestore";
            }
        }

        ApplyStringsFromLocalizationService(reason);
    }

    private void ApplyStringsFromLocalizationService(string reason)
    {
        if (null == _LocalizationService)
            return;


        this.Title = _LocalizationService.GetString($"ui.app.restart.{reason}.pageTitle");
        TitleLabel.Text = _LocalizationService.GetString($"ui.app.restart.{reason}.titleLabel");
        ContentLabel.Text = _LocalizationService.GetString($"ui.app.restart.{reason}.contentLabel");
        QuitButton.Text = _LocalizationService.GetString($"ui.app.restart.{reason}.quit");

    }

    private void QuitButton_Clicked(object sender, EventArgs e)
    {
		Application.Current?.Quit();
    }

    
}