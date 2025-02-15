using Plugin.BLE.Abstractions.Contracts;
using ZXing.Net.Maui;
using QR_scanner_zxing.Platforms.Android;
using QR_scanner_zxing.Resources.Services;
using System.Data;
using Microsoft.Maui.Layouts;
using ZXing.Net.Maui.Controls;
using System.Linq.Expressions;


namespace QR_scanner_zxing
{
    public partial class QRCodeScannerPage : ContentPage
    {
        private readonly IBluetoothService _bluetoothService;
        private bool _isProcessing = false;
        public QRCodeScannerPage(IBluetoothService bluetoothService)
        {
            InitializeComponent();
            _bluetoothService = bluetoothService ?? throw new ArgumentNullException(nameof(bluetoothService));
            Application.Current.UserAppTheme = AppTheme.Dark;
            NavigationPage.SetHasBackButton(this, false); // Remove bot�o de voltar caso o usu�rio volte para a tela inicial ap�s uma conex�o
            Logger.Log("Info", "Teste de log");
        }

        // Fun��o para conex�o direta ao endere�o (uso somente em escala de desenvolvimento)
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            Logger.Log("INFO", "[QRCodeScannerPage][OnAppearing] Execu��o inicializada");

            //string bluetoothAddress = "FE:97:90:00:04:39";
            ////string bluetoothAddress = "CA:32:64:7A:79:A6";
            //devPromptConnect(bluetoothAddress);

        }
        protected override bool OnBackButtonPressed() { return true; }



        private async void OnBarcodeDetected(object sender, BarcodeDetectionEventArgs e)
        {
            Logger.Log("INFO", "[QRCodeScannerPage][OnBarcodeDetected] QR Code identificado");
            
            if (_isProcessing)
            {
                return;
            }

            try
            {
                Logger.Log("INFO", "[QRCodeScannerPage][OnBarcodeDetected] Iniciando tratamento do QR Code detectado");
                var qrCodeValue = e.Results[0].Value;

                if (qrCodeValue != null && qrCodeValue.Contains("html?mac="))
                {

                    // ------------------------------------------------------------------------ Formato convencional (XX:XX:XX:XX:XX) ------------------------------------------------------------------------
                    string macAddress = qrCodeValue.Substring(qrCodeValue.IndexOf("mac=") + 4, qrCodeValue.IndexOf('&', qrCodeValue.IndexOf("mac=")) - (qrCodeValue.IndexOf("mac=") + 4));

                    var formattedAddress = new System.Text.StringBuilder();

                    for (int i = 0; i < macAddress.Length; i += 2)
                    {
                        formattedAddress.Append(macAddress.Substring(i, 2));
                        if (i < macAddress.Length - 2)
                        {
                            formattedAddress.Append(":");
                        }
                    }

                    macAddress = formattedAddress.ToString();
                    // ---------------------------------------------------------------------------------------------------------------



                    // Formato UUID -----------------------------------------------------------
                    //string macAddress = qrCodeValue.Substring(qrCodeValue.IndexOf("mac=") + 4, qrCodeValue.IndexOf('&', qrCodeValue.IndexOf("mac=")) - (qrCodeValue.IndexOf("mac=") + 4));

                    //macAddress = $"00000000-0000-0000-0000-{macAddress.ToUpper()}";
                    // ------------------------------------------------------------------------

                    PromptBluetoothConnection(macAddress);
                }
                else if (qrCodeValue != null && qrCodeValue.Contains("="))
                {
                    string macAddress = qrCodeValue.Substring(qrCodeValue.IndexOf("=") + 1);

                    var formattedAddress = new System.Text.StringBuilder();

                    for (int i = 0; i < macAddress.Length; i += 2)
                    {
                        formattedAddress.Append(macAddress.Substring(i, 2));
                        if (i < macAddress.Length - 2)
                        {
                            formattedAddress.Append(":");
                        }
                    }

                    macAddress = formattedAddress.ToString();
                    PromptBluetoothConnection(macAddress);
                }
                else
                {
                    Console.WriteLine($"QR Code inv�lido. Valor do QR Code: {qrCodeValue}");
                    Logger.Log("warning", "[QRCodeScannerPage][OnBarcodeDetected] QR Code inv�lido");
                    _isProcessing = true;
                    Dispatcher.Dispatch(async () =>
                    {
                        await DisplayAlert("Teste", $"QR Code inv�lido.", "OK");
                    });
                    _isProcessing= false;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro", ex.Message, "OK");
                Console.WriteLine($"Erro {ex.Message}");
                Logger.Log("error", $"[QRCodeScannerPage][OnBarcodeDetected][Catch] {ex.Message}");

            }
        }

        private async void devPromptConnect(string bluetoothAddress)
        {
            try
            {
                _isProcessing = true;
                bool connect = true;

                if (connect)
                {
                    OnConnectButtonClicked(bluetoothAddress);
                }
                else if (connect == false)
                {
                    _isProcessing = false;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro", ex.Message, "OK");
                Console.WriteLine($"Erro {ex.Message}");
            }
        }



        private async void PromptBluetoothConnection(string bluetoothAddress)
        {
            try
            {
                _isProcessing = true;
                bool connect = await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    return await DisplayAlert("Conectar ao dispositivo?", $"Foi detectado o endere�o Bluetooth pelo QR lido: {bluetoothAddress}", "Sim", "N�o");
                });

                Logger.Log("info", "[QRCodeScannerPage][PromptBluetoothConnection] Perguntado ao usu�rio se a conex�o ser� efetuada no endere�o lido");

                if (connect)
                {
                    OnConnectButtonClicked(bluetoothAddress);
                }
                else if (connect == false)
                {
                    Logger.Log("info", "[QRCodeScannerPage][PromptBluetoothConnection] Conex�o recusada pelo usu�rio");
                    _isProcessing = false;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro", ex.Message, "OK");
                Console.WriteLine($"Erro {ex.Message}");
                Logger.Log("ERROR", $"[QRCodeScannerPage][PromptBluetoothConnection][Catch] {ex.Message}");

            }
        }



        private async Task<bool> CheckPermissionsAsync()
        {
            Logger.Log("info", "[QRCodeScannerPage][CheckPermissionsAsync] Verificando se as permiss�es de uso do aplicativo foram concedidas pelo usu�rio");
            return await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                    if (status != PermissionStatus.Granted)
                    {
                        //Logger.Log("info", "[QRCodeScannerPage][CheckPermissionsAsync] Verificando se as permiss�es de uso do aplicativo foram concedidas pelo usu�rio");
                        status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                    }
                }

                Logger.Log("info", "[QRCodeScannerPage][CheckPermissionsAsync] Permiss�es concedidas");
                return true;
            });
        }


        private async void OnConnectButtonClicked(string bluetoothAddress)
        {
            bool permissionsGranted = await CheckPermissionsAsync();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                loadingOverlay.IsVisible = true;
            });

            if (!permissionsGranted)
            {
                return;
            }

            IDevice device = await _bluetoothService.ConnectToBleAsync(bluetoothAddress);
            //string data = null;

            if (device != null)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    loadingOverlay.IsVisible = false;
                });


                await MainThread.InvokeOnMainThreadAsync(async () => 
                {
                    Application.Current.UserAppTheme = AppTheme.Light;
                    await Navigation.PushAsync(new RecordDisplayPage(_bluetoothService));
                });


                // P�gina de listagem de servi�os e caracter�sticas do dispositivo
                //await MainThread.InvokeOnMainThreadAsync(async () =>
                //{
                //    Application.Current.UserAppTheme = AppTheme.Light;
                //    await Navigation.PushAsync(new ServiceListPage(device, _bluetoothService));
                //});


                //try { data = await _bluetoothService.ReadDataAsync(); }
                //catch (Exception ex) { Console.WriteLine("Deu pane."); }

                //if (data != null)
                //    foreach (var dados in data)
                //    {
                //        Console.WriteLine($"Dados: {dados}");
                //    }
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    loadingOverlay.IsVisible = false;
                });

                Console.WriteLine("[QRCodeScannerPage][OnConnectButtonClicked][Erro] N�o foi poss�vel ler os dados do dispositivo.");
                Logger.Log("error", "[QRCodeScannerPage][OnConnectButtonClicked][Erro] N�o foi poss�vel ler os dados do dispositivo.");
                await DisplayAlert("Erro", "N�o foi poss�vel ler os dados do dispositivo.", "OK");
            }
            _isProcessing = false;
        }

        private async void OnAppInfoClicked(object sender, EventArgs e)
        {
            //await Navigation.PushAsync(new InfoPage());

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Navigation.PushAsync(new InfoPage());
            });

            return;
        }
    }
}
