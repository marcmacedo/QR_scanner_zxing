using Plugin.BLE.Abstractions.Contracts;

namespace QR_scanner_zxing;

public partial class CharacteristicsDetailsPage : ContentPage
{
	private IService _service;
	public CharacteristicsDetailsPage(IService service)
	{
		InitializeComponent();
		_service = service;
		LoadCharacteristics();
	}

	private async void LoadCharacteristics()
	{
		var characteristics = await _service.GetCharacteristicsAsync();
		characteristicsCollectionView.ItemsSource = characteristics;

		foreach (var characteristic in characteristics) { 
			Console.WriteLine($"[LoadCharacteristics] From: {_service.Name} | {_service.Id}. Característica: {characteristic.Name}, ID: {characteristic.Id}");
		}
	}
}