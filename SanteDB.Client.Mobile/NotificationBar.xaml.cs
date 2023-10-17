using AndroidX.Lifecycle;
using System.Collections.ObjectModel;

namespace SanteDB.Client.Mobile;

public partial class NotificationBar : ContentView
{
	public NotificationBar()
	{
        InitializeComponent();
        SetControlTemplate();
    }

	public ControlTemplate NoNotificationTemplate { get; set; }
	public ControlTemplate SingleNotificationTemplate { get; set; }
	public ControlTemplate MultipleNotificationTemplate { get; set; }

    public ObservableCollection<ViewModels.NotificationViewModel> Notifications { get; } = new();

	public string SingleNotificationText
	{
		get => Notifications.Single().Message;
	}

	private void SetControlTemplate()
	{
		if (Notifications.Count == 0)
			ControlTemplate = NoNotificationTemplate;
		else if (Notifications.Count == 1)
			ControlTemplate = SingleNotificationTemplate;
		else
			ControlTemplate = MultipleNotificationTemplate;
	}

	public async Task ShowOrUpdateNotificationAsync(string identifier, string message, float progressIndicator = 0f)
	{
		await Dispatcher.DispatchAsync(() =>
		{
			var notification = Notifications.FirstOrDefault(n => n.Identifier == identifier);

			if (null == notification)
			{
				notification = new();
				notification.Identifier = identifier;

				Notifications.Add(notification);
			}

			notification.Message = message;
			notification.ProgressIndicator = progressIndicator;
			notification.LastUpdated = DateTimeOffset.UtcNow;

			SetControlTemplate();
        });
	}

	public async Task RemoveNotificationAsync(string identifier)
	{
		await Dispatcher.DispatchAsync(() =>
		{
			foreach(var i in Notifications.Where(n => n.Identifier == identifier).Select((_, idx) => idx).ToArray())
			{
				Notifications.RemoveAt(i);
			}

			SetControlTemplate();
        });
	}
}