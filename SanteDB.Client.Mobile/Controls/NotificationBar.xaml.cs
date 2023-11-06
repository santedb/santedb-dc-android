using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Platform;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Markup;
using static CommunityToolkit.Maui.Markup.GridRowsColumns;
using System.Drawing.Printing;

namespace SanteDB.Client.Mobile.Controls;

public partial class NotificationBar : ContentView, INotifyPropertyChanged
{
    bool _IsDismissed;

    public NotificationBar()
    {
        InitializeComponent();

        this.Notifications.CollectionChanged += Notifications_CollectionChanged;

        this.IsVisible = false;
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        NotificationsUpdated();
    }

    private void Notifications_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        NotificationsUpdated();
    }

    private void NotificationsUpdated()
    {
        OnPropertyChanged(nameof(NotificationCount));
        OnPropertyChanged(nameof(NotificationText));
        OnPropertyChanged(nameof(NotificationProgress));
        OnPropertyChanged(nameof(ShowNotificationsDetailEnabled));
        OnPropertyChanged(nameof(ShowNotificationBar));
    }

    public ObservableCollection<ViewModels.NotificationViewModel> Notifications { get; } = new();

    public int NotificationCount
    {
        get => Notifications.Count;
    }

    public string NotificationText
    {
        get
        {
            var c = NotificationCount;

            switch (c)
            {
                case 0:
                    return "There are no notifications";
                case 1:
                    return Notifications.Single().Message;
                default:
                    return $"{c} Notifications. Click for Details";
            }
        }
    }

    public bool ShowNotificationBar
    {
        get
        {
            if (_IsDismissed)
                return false;

            return NotificationCount > 0;
        }
    }


    public float NotificationProgress
    {
        get
        {
            var c = NotificationCount;

            switch (c)
            {
                case 1:
                    return Notifications.Single().ProgressIndicator;
                case 0:
                    return 0;
                default:
                    return Notifications.Sum(n => Math.Min(n.ProgressIndicator, 1)) / c;
            }
        }
    }

    public async Task ShowOrUpdateNotificationAsync(string identifier, string message, float progressIndicator = 0f)
    {
        await Dispatcher.DispatchAsync(() =>
        {
            var notification = Notifications.FirstOrDefault(n => n.Identifier == identifier);
            bool update = false;

            if (null == notification)
            {
                notification = new();
                notification.Identifier = identifier;

                Notifications.Add(notification);

                _IsDismissed = false; //Reset dismissal because we're adding a new notification.
                IsVisible = true;
            }
            else
            {
                update = true;
                
            }

            notification.Message = message;
            notification.ProgressIndicator = progressIndicator;
            notification.LastUpdated = DateTimeOffset.UtcNow;

            if (update)
                NotificationsUpdated();
        });
    }

    public async Task RemoveNotificationAsync(string identifier)
    {
        await Dispatcher.DispatchAsync(() =>
        {
            foreach (var i in Notifications.Where(n => n.Identifier == identifier).Select((_, idx) => idx).ToArray())
            {
                Notifications.RemoveAt(i);
            }

        });
    }

    private async void DismissedButton_Clicked(object sender, EventArgs e)
    {
        await Dispatcher.DispatchAsync(() =>
        {
            _IsDismissed = true;
            IsVisible = false;

        });

    }

    private async void ShowNotificationsPopup_Clicked(object sender, EventArgs args)
    {
        await Dispatcher.DispatchAsync(async () =>
        {
            var popup = CreateNotificationDetailPopup();

            Window.Page?.ShowPopupAsync(popup);

        });
    }

    private bool ShowNotificationsDetailEnabled
    {
        get
        {
            return NotificationCount > 1;
        }
    }

    private Popup CreateNotificationDetailPopup()
    {
        var popup = new Popup();


        popup.Content = new Grid()
        {
            RowDefinitions = Rows.Define(Auto, Stars(1), Auto),
            Children =
            {
                new Label()
                    .Text("Notifications")
                    .Bold()
                    .FontSize(16)
                    .CenterHorizontal()
                    .FillVertical()
                    .Margin(10, 5)
                    .Row(0).Column(0),
                new ScrollView
                {
                    Content = new VerticalStackLayout()
                    
                    .FillVertical()
                    .Margin(10, 0)
                    .Invoke(vsl => vsl.Spacing = 15)
                    .Assign(out VerticalStackLayout stack),
                }.Row(1).Column(1),
                new Button()
                    .Row(2).Column(0)
                    .Text("Close")
                    .CenterHorizontal()
                    .Margins(10, 20, 10, 5)
                    .Assign(out Button closebutton)
            },
        }.Padding(20).Margins(bottom: 50);

        var notifications = Notifications.ToList();

        int c = 0;

        bool showprogress = notifications.Any(n => n.ProgressIndicator > 0);

        foreach (var notification in notifications)
        {
            stack.Children.Add(
                new Grid()
                {
                    RowDefinitions = Rows.Define(Stars(1), Auto),
                    Children = {
                        new Label()
                            .Text(notification.Message)
                            .Row(0).Column(0)
                            .TextStart()
                            .FillHorizontal(),
                        new ProgressBar()
                        {
                            Progress = notification.ProgressIndicator,
                            IsVisible = showprogress
                        }
                        .Row(1).Column(0)
                    }
                });


            c++;

            if (c >= 5)
            {
                //break;
            }
        }

        closebutton.Clicked += (s, e) =>
        {
            popup.Close();
        };

        return popup;
    }
}