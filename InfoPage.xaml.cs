using Microsoft.Maui.Storage;
using QR_scanner_zxing.Resources.Services;
using QR_scanner_zxing.Platforms.Android;
using System;
using System.IO;
using System.Reflection;

namespace QR_scanner_zxing;

public partial class InfoPage : ContentPage
{
    private readonly IBluetoothService _bluetoothService;

    public string AppName { get; set; }
	public string AppVersion { get; set; }
    public InfoPage()
	{
		InitializeComponent();

		AppName = AppInfo.Name;
        AppVersion = AppInfo.VersionString;

		BindingContext = this;
	}

	private async void OnExportLogClicked(object sender, EventArgs e) 
	{
		try
		{
			string logFilePath = Logger.GetLogFilePath();


			if (!File.Exists(logFilePath))
			{
				await DisplayAlert("Erro", "Nenhum log encontrado para exportação", "OK");
				return;
			}

			await Share.RequestAsync(new ShareFileRequest
			{
				Title = "Exportar Log",
				File = new ShareFile(logFilePath)
			});
		}
		catch (Exception ex) 
		{
			await DisplayAlert("Erro", $"Falha ao exportar log: {ex.Message}", "OK");
			Console.WriteLine(ex.Message);
		}
	}

    protected override bool OnBackButtonPressed()
    {
        NavigateToRoot();
        return true;
    }
	private async void NavigateToRoot()
	{

		if (App.BluetoothService != null)
		{

			await MainThread.InvokeOnMainThreadAsync(async () =>
			{

				await Navigation.PushAsync(new QRCodeScannerPage(App.BluetoothService));
			});
		}
		else
		{
			await DisplayAlert("Erro", "Serviço bluetooth não disponível!", "OK");
		}
	}
}