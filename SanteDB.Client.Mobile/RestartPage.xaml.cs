
using SanteDB.Core;
using SanteDB.Core.Services;
using System.ComponentModel;

namespace SanteDB.Client.Mobile;

public partial class RestartPage : ContentPage, IQueryAttributable, INotifyPropertyChanged
{
    readonly ILocalizationService? _LocalizationService;
    readonly IApplicationServiceContext _Services;

    protected string ContentPageTitle { get; set; } = "App Restart Required";
    protected string TitleLabelText { get; set; } = "App Restart Required";
    protected string ContentLabelText { get; set; } = "The App needs to be restarted. Click the Quit button below, and then re-launch the app.";

    protected string QuitButtonText { get; set; } = "Quit";


    public RestartPage()
	{
        _Services = ApplicationServiceContext.Current;

        if (null != _Services)
            _LocalizationService = SanteDB.Core.ApplicationServiceContext.GetService<ILocalizationService>(_Services); //Need to fully qualify because MEDI and ASC have the same extension method signature.

        BindingContext = this;

		InitializeComponent();
	}

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

        OnPropertyChanging(nameof(ContentPageTitle));
        OnPropertyChanging(nameof(Title));
        OnPropertyChanging(nameof(ContentLabelText));
        OnPropertyChanging(nameof(QuitButtonText));

        ContentPageTitle = _LocalizationService.GetString($"ui.app.restart.{reason}.pageTitle");
        Title = _LocalizationService.GetString($"ui.app.restart.{reason}.titleLabel");
        ContentLabelText = _LocalizationService.GetString($"ui.app.restart.{reason}.contentLabel");
        QuitButtonText = _LocalizationService.GetString($"ui.app.restart.{reason}.quit");

        OnPropertyChanged(nameof(ContentPageTitle));
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(ContentLabelText));
        OnPropertyChanged(nameof(QuitButtonText));
    }

    private void QuitButton_Clicked(object sender, EventArgs e)
    {
		Application.Current?.Quit();
    }
}