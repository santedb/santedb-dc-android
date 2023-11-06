namespace SanteDB.Client.Mobile.Controls;

using BarcodeScanner.Mobile;
using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Maui.Views;
using static CommunityToolkit.Maui.Markup.GridRowsColumns;

public class BarcodeScannerPopup : Popup, IDisposable
{
    private bool disposedValue;

    private CameraView CameraView;
    private Button ToggleFlashlight;
    private Grid TheGrid;

    public BarcodeScannerPopup()
    {
        ResultWhenUserTapsOutsideOfPopup = string.Empty;

        Content = new VerticalStackLayout
        {
            Children = {
                new Border
                {
                    Stroke = SolidColorBrush.Transparent,
                    StrokeThickness = 0,
                    Content = new Grid
                    {
                        RowDefinitions = Rows.Define(Star),
                        Children = {
                            new CameraView
                            {
                                CaptureQuality = CaptureQuality.Low
                            }
                            .Size(720, 480)
                            .Row(0)
                            .Assign(out CameraView),
                            new Border
                            {
                                StrokeThickness = 0,
                                Stroke = SolidColorBrush.Transparent,
                                Background = SolidColorBrush.Transparent
                            }
                                .Fill(),
                            new Button()
                            {
                                HorizontalOptions = LayoutOptions.Start,
                                VerticalOptions = LayoutOptions.Start,
                            }
                                .Text("Toggle Flashlight")
                                .Assign(out ToggleFlashlight)
                                .Margin(10)

                        }
                    }
                    .MinSize(640, 480)
                    .Assign(out TheGrid)
                }
            }
        };

        CameraView.OnDetected += CameraView_OnDetected;
    }

    private void CameraView_OnDetected(object sender, OnDetectedEventArg e)
    {
        if (e?.BarcodeResults?.Count == 1)
        {
            this.Close(e.BarcodeResults.FirstOrDefault()?.RawValue);
        }
        else
        {
            CameraView.IsScanning = true;
        }
    }

    private void ToggleFlashlight_Clicked(object sender, EventArgs e)
    {
        if (null != CameraView)
        {
            CameraView.TorchOn = !CameraView.TorchOn;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
                if (null != TheGrid)
                {
                    do
                    {
                        TheGrid.Children.RemoveAt(0);
                    }
                    while (TheGrid.Children.Count > 0);
                }
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~BarcodeScannerPopup()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

