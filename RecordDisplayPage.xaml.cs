using QR_scanner_zxing.Platforms.Android;
using System.Runtime.InteropServices;
using System.Text;
using QR_scanner_zxing.Models;

namespace QR_scanner_zxing;

public partial class RecordDisplayPage : ContentPage
{
	private readonly IBluetoothService _bluetoothService;
    Dictionary<int, (long timestamp, double temp)> _records;

	public RecordDisplayPage(IBluetoothService bluetoothService)
	{
		InitializeComponent();
		_bluetoothService = bluetoothService;
		StartReadingData(_bluetoothService);


		//RecordsListView.ItemsSource = _records;
		//_records = records;
		BindingContext = this;
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
	private async void StartReadingData(IBluetoothService bluetoothService)
	{
		try
		{
         
			// Remover
            Guid serviceUuid = Guid.Parse("0000fff0-0000-1000-8000-00805f9b34fb");
			Guid characteristicUuid = Guid.Parse("0000fff1-0000-1000-8000-00805f9b34fb");


			Console.WriteLine("[RecordDisplayPage][StartReadingData] Iniciando leitura dos dados históricos");
			var data = await _bluetoothService.ReadDataAsync(serviceUuid, characteristicUuid, _bluetoothService);

			_records = data;



            var registrosList = _records.Select(r => new Record
			{
				Timestamp = DateTimeOffset.FromUnixTimeSeconds(r.Value.timestamp).ToString("yyyy-MM-dd HH:mm:ss"),
				Temperatura = $"{r.Value.temp} °C"
			}).ToList();

            Console.WriteLine($"Total de registros: {registrosList.Count}");
            foreach (var registro in registrosList)
            {
                Console.WriteLine($"Timestamp: {registro.Timestamp}, Temperatura: {registro.Temperatura}");
            }

            RecordsListView.ItemsSource = registrosList;
			TitleLabel.Text = $"Registros lidos ({registrosList.Count})";

		}
        catch (Exception ex) 
		{
			Console.WriteLine("Deu pane."); 
		}

	}
	private async void OnExportClicked(object sender, EventArgs e)
	{
		if (_records != null && _records.Count > 0)
		{
			await ExportToCsvAsync(_records);
		}
		else
		{
			await DisplayAlert("Aviso", "Não há dados para exportar.", "OK");
		}
	}
	private async Task ExportToCsvAsync(Dictionary<int, (long timestamp, double temp)> data)
	{
		try
		{
			string fileName = $"ExportedData_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
			string filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);

			StringBuilder csvBuilder = new StringBuilder();

			//foreach (string record in data)
			//{
			//	csvBuilder.AppendLine(record);
			//}

			File.WriteAllText(filePath, csvBuilder.ToString());

			await DisplayAlert("Exportação megalomaníaca", $"Arquivo salvo em: {filePath}", "OK");
			await ShareCsvAsync(filePath);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Erro ao tentar exportar CSV: {ex.Message}");
			await DisplayAlert("Erro", "Não foi possível exportar os dados.", "OK");
		}
	}

	private async Task ShareCsvAsync(string filePath)
	{
		try
		{
			await Share.RequestAsync(new ShareFileRequest
			{
				Title = "Exportar CSV",
				File = new ShareFile(filePath)
			});
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Erro ao compartilhar CSV: {ex.Message}");
		}
	}
}