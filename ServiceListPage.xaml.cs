using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.Exceptions;
using QR_scanner_zxing.Platforms.Android;
using System.ComponentModel;
using System.Diagnostics;
namespace QR_scanner_zxing;

public partial class ServiceListPage : ContentPage
{
	private IDevice _connectedDevice;
	private IBluetoothService _bluetoothService;
	public ServiceListPage(IDevice connectedDevice, IBluetoothService bluetoothService)
	{
		InitializeComponent();
		_connectedDevice = connectedDevice;
		_bluetoothService = bluetoothService;
		LoadServices();
	}

	private async void LoadServices()
	{
		var services = await _connectedDevice.GetServicesAsync();
		servicesCollectionView.ItemsSource = services;

		foreach (var service in services) 
		{
			Console.WriteLine("");
		}
	}

	private async void OnServiceSelected(object sender, SelectionChangedEventArgs e)
	{
		var service = e.CurrentSelection.FirstOrDefault() as IService;

		if (service != null) 
		{
			await Navigation.PushAsync(new CharacteristicsDetailsPage(service));
		}

		//((CollectionView)sender).SelectedItem = null;
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();

		if (servicesCollectionView.SelectedItem != null)
		{
			servicesCollectionView.SelectedItem = null;
		}
    }

    protected override bool OnBackButtonPressed()
    {
		NavigateToRoot();
        return true;
    }

	private async void NavigateToRoot()
	{
		bool dc = await DisplayAlert("Aviso", "Ao voltar, o dispoistivo atual será desconectado. Deseja continuar?", "SIM", "NÃO");

        if (dc)
		{

            await _bluetoothService.DisconnectAsync();

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {

                await Navigation.PushAsync(new QRCodeScannerPage(_bluetoothService));
            });
        }


	}
}