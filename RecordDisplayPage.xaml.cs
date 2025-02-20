using QR_scanner_zxing.Platforms.Android;
using System.Runtime.InteropServices;
using System.Text;
using QR_scanner_zxing.Models;
using QR_scanner_zxing.Resources.Services;
using System.Text.Json;
using System.Net.Http;
using System.Diagnostics;



namespace QR_scanner_zxing;

public partial class RecordDisplayPage : ContentPage
{
	private readonly HttpClient _httpClient;
	private readonly IBluetoothService _bluetoothService;
    SensorData _records = new SensorData();





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
            Logger.Log("Info", "[BluetoothService][DisconnectAsync] Dispositivo desconectado pelo usuário");

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
			Logger.Log("Info", "[RecordDisplayPage][StartReadingData] Preparando payload de leitura de dados");
			// Remover
            Guid serviceUuid = Guid.Parse("0000fff0-0000-1000-8000-00805f9b34fb");
			Guid characteristicUuid = Guid.Parse("0000fff1-0000-1000-8000-00805f9b34fb");


			SensorData data = await _bluetoothService.ReadDataAsync(serviceUuid, characteristicUuid, _bluetoothService);
			Console.WriteLine($"{data.Model} | {data.Address} | {data.Battery} | {data.StartDate} | {data.CurrentDate} | {data.CurrentTemp}");
			
			_records = data;



            var registrosList = _records.Records.Select(r => new Record
			{
				Timestamp = long.TryParse(r.Timestamp, out long unixTime)
							? DateTimeOffset.FromUnixTimeSeconds(unixTime).ToString("yyyy-MM-dd HH:mm:ss")
							: "Invalid timestamp",
				Temperatura = $"{r.Temperatura} °C"
			}).ToList();

			await Task.Delay(2000);
            Console.WriteLine($"[INFO] - [RecordDisplayPage][StartReadingData] Total de registros: {registrosList.Count}");
            Logger.Log("Info", $"[RecordDisplayPage][StartReadingData] Total de registros lidos: {registrosList.Count}");
            
			
			//foreach (var registro in registrosList)
            //{
            //    Console.WriteLine($"Timestamp: {registro.Timestamp}, Temperatura: {registro.Temperatura}");
            //}

            RecordsListView.ItemsSource = registrosList;
			TitleLabel.Text = $"Registros lidos ({registrosList.Count})";

		}
        catch (Exception ex) 
		{
			Console.WriteLine($"[ERROR] - [RecordDisplayPage][StartReadingData] Erro: {ex.Message}");
			Logger.Log("error", $"[RecordDisplayPage][StartReadingData] {ex.Message}");
		}
	}
	private async void OnExportClicked(object sender, EventArgs e)
	{
		if (_records != null && _records.Records.Count > 0)
		{
			//await ExportToCsvAsync(_records.Records);
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


			File.WriteAllText(filePath, data.ToString());

			await DisplayAlert("Exportação megalomaníaca", $"Arquivo salvo em: {filePath}", "OK");
			await ShareCsvAsync(filePath);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[ERROR] - [RecordDisplayPage][ExportToCsvAsync] Erro ao tentar exportar CSV: {ex.Message}");
			await DisplayAlert("Erro", "Não foi possível exportar os dados.", "OK");
		}
	}

	private async void OnExportToTagoClicked(object sender, EventArgs e)
	{
		try
		{
            var handler = new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            var _httpClient = new HttpClient(handler);


            string apiUrl = "https://192.168.15.6:8080/tago";

			int chunkSize = 1000;

			string fullJson = JsonSerializer.Serialize(_records, new JsonSerializerOptions { WriteIndented = true });
			int totalChunk = (int)Math.Ceiling((double)fullJson.Length / chunkSize);
			for (int i = 0; i < totalChunk; i++)
			{
				string chunk = fullJson.Substring(i * chunkSize, Math.Min(chunkSize, fullJson.Length - (i * chunkSize)));

				var payload = new
				{
					chunkIndex = i,
					totalChunk = totalChunk,
					data = chunk
				};

				string jsonChunk = JsonSerializer.Serialize(payload);
				var content = new StringContent(jsonChunk, Encoding.UTF8, "application/json");

				var response = await _httpClient.PostAsync(apiUrl, content);
				var responseBody = await response.Content.ReadAsStringAsync();
				Console.WriteLine($"Chunk {i + 1}/{totalChunk} enviado. Resposta: {responseBody}");
			}




			//var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

			//var response = await _httpClient.PostAsync(apiUrl, content);
			//var responseBody = await response.Content.ReadAsStringAsync();
			//Console.WriteLine($"Resposta: {responseBody}");


			//if (response.IsSuccessStatusCode)
   //         {
   //             Console.WriteLine("Dados enviados com sucesso.");
			//	await DisplayAlert("Aviso", "Dados enviados via API.", "OK");
   //         }
   //         else
   //         {
   //             Console.WriteLine($"Erro ao enviar dados: {response.StatusCode}");
   //         }
        }
		catch (Exception ex)
		{
			Console.WriteLine($"Erro na requisição: {ex.Message}");
			Console.WriteLine($"StackTrace: {ex.StackTrace}");
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
			Console.WriteLine($"[ERROR] - [RecordDisplayPage][ShareCsvAsync] Erro ao compartilhar CSV: {ex.Message}");
		}
	}
}