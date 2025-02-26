using QR_scanner_zxing.Platforms.Android;
using System.Text;
using QR_scanner_zxing.Models;
using QR_scanner_zxing.Resources.Services;
using System.Text.Json;
using QR_scanner_zxing.Resources.Raw;



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

	private async void OnExportToTagoClicked(object sender, EventArgs e)
    {
        try
		{

			var tokens = AppSettings.GetApiTokens();

			if (tokens.Count == 0)
			{
                bool register = await DisplayAlert("Nenhum Token Encontrado", "Nenhuma chave de conexão foi encontrada. Deseja adicionar um novo token?", "SIM", "NÃO");
                if (register)
                {
					await RegisterNewToken();
                }
				return;
			}

			string selectedToken = await DisplayActionSheet("Selecione um Token", "Cancelar", "Adicionar", tokens.Keys.ToArray());


			if (selectedToken == "Adicionar")
			{
                await RegisterNewToken();
            }
			else if (!string.IsNullOrEmpty(selectedToken))
			{

				string action = await DisplayActionSheet($"Opções {selectedToken}", "Cancelar", null, "Selecionar", "Editar", "Excluir");
				string tokenValue = "";

                if (action == "Selecionar")
				{
					tokenValue = tokens[selectedToken];
					Console.WriteLine($"Token selecionado: {selectedToken} | {tokenValue}");
				}
				else if (action == "Editar")
				{
					await EditToken(selectedToken);
				}
				else if (action == "Excluir")
				{
					bool confirm = await DisplayAlert("Excluir Token", $"Tem certeza que deseja excluir o token \"{selectedToken}\"?", "SIM", "NÃO");
					if (confirm)
					{
						AppSettings.DeleteApiToken(selectedToken);
						await DisplayAlert("Sucesso", "Token excluído com sucesso!", "OK");
					}
				}



				//var handler = new HttpClientHandler()

				//{
				//	ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
				//};

				//var _httpClient = new HttpClient(handler);
				var _httpClient = new HttpClient();
			


				string apiUrl = "https://api.tago.io/data";


				//var lastSentIndexStr = await SecureStorage.GetAsync("last_sent_index");
				int lastSentIndexStr = await AppSettings.GetLastSentIndexAsync(tokenValue);
                int lastSentIndex = lastSentIndexStr != null ? lastSentIndexStr : 0;
				Console.WriteLine($"VALOR DO ÚLTIMO ÍNDICE {lastSentIndex}");
                var tagoData = _records.ToTagoFormat(lastSentIndex);

				//Console.WriteLine($"{JsonSerializer.Serialize(_records.ToTagoFormat(0), new JsonSerializerOptions { WriteIndented = true })}");


                if (tagoData.Count <= 6)
                {
                    Console.WriteLine("Não há novos dados a serem enviados para a Tago.");
                    await DisplayAlert("Aviso", "Não há novos registros a serem enviados.", "OK");
                }

                else
                {
                    string jsonContent = JsonSerializer.Serialize(tagoData, new JsonSerializerOptions { WriteIndented = true });

                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Add("Device-Token", tokenValue);


                    var response = await _httpClient.PostAsync(apiUrl, content);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    Console.WriteLine($"Status: {response.StatusCode} | Resposta da API: {responseBody}");

                    if (response.IsSuccessStatusCode)
                    {
                        int newLastCount = _records.Records.Count;
                        await AppSettings.SaveLastSentIndexAsync(tokenValue, newLastCount);

                        Console.WriteLine($"Novo último índice enviado: {newLastCount}");
                    }
                }

            }


			
        }
		catch (Exception ex)
		{
			Console.WriteLine($"Erro na requisição: {ex.Message}");
			Console.WriteLine($"StackTrace: {ex.StackTrace}");
		}
	}


	private async Task RegisterNewToken()
	{
		try
		{
            string tokenName = await DisplayPromptAsync("Novo Token", "Digite um nome para o token:", "OK", "Cancelar");
            if (string.IsNullOrEmpty(tokenName)) return;

            string tokenValue = await DisplayPromptAsync("Novo Token", "Digite o valor do token:");
            if (string.IsNullOrEmpty(tokenValue)) return;

            AppSettings.SaveApiToken(tokenName, tokenValue);
            Console.WriteLine($"Token salvo com sucesso: {tokenName} | {tokenValue}");

            await DisplayAlert("Sucesso", "Token cadastrado com sucesso", "OK");
        }
		catch (Exception ex)
		{
			Console.WriteLine($"Erro ao cadastrar token: {ex.Message}");

		}
    }

	private async Task EditToken(string oldToken)
	{
		var tokens = AppSettings.GetApiTokens();
		if (!tokens.ContainsKey(oldToken)) return;

		string newName = await DisplayPromptAsync("Editar Token", "Insira o novo nome para o token.", initialValue: oldToken);
		if (string.IsNullOrEmpty(newName)) return;

		string newValue = await DisplayPromptAsync("Editar Token", "Insira o novo valor para o token.", initialValue: tokens[oldToken]);
		if (string.IsNullOrEmpty(newValue)) return;

		AppSettings.DeleteApiToken(oldToken);
		AppSettings.SaveApiToken(newName, newValue);

		await DisplayAlert("Sucesso", "Token editado com sucesso!", "OK");
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