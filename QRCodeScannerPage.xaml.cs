using Plugin.BLE.Abstractions.Contracts;
using ZXing.Net.Maui;
using QR_scanner_zxing.Platforms.Android;
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
            NavigationPage.SetHasBackButton(this, false); // Remove botão de voltar caso o usuário volte para a tela inicial após uma conexão
        }

        // Removendo ação do botão de voltar na tela inicial

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            string bluetoothAddress = "FE:97:90:00:04:39";
            devPromptConnect(bluetoothAddress);

        }
        protected override bool OnBackButtonPressed() { return true; }



        private async void OnBarcodeDetected(object sender, BarcodeDetectionEventArgs e)
        {
            if (_isProcessing)
            {
                return;
            }

            try
            {
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
                    Console.WriteLine($"{macAddress}");

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
                    Console.WriteLine($"QR Code inválido. Valor do QR Code: {qrCodeValue}");
                    _isProcessing = true;
                    Dispatcher.Dispatch(async () =>
                    {
                        await DisplayAlert("Teste", $"QR Code inválido. Valor do QR Code: {qrCodeValue}", "OK");
                    });
                    _isProcessing= false;
                    //await DisplayAlert("Teste", $"Este QR Code não contém o formato específico para ser lido. Valor do QR Code: {qrCodeValue}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro", ex.Message, "OK");
                Console.WriteLine($"Erro {ex.Message}");
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
                Console.WriteLine($"Entrei na função PromptBluetoothConnection {bluetoothAddress}");
                _isProcessing = true;
                bool connect = await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    Console.WriteLine($"(PromptBLuetoothConnection) {bluetoothAddress} Address detected.");
                    return await DisplayAlert("Conectar ao Bluetooth?", $"Foi detectado o endereço Bluetooth pelo QR lido: {bluetoothAddress}", "Sim", "Não");
                });

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



        private async Task<bool> CheckPermissionsAsync()
        {
            return await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                    if (status != PermissionStatus.Granted)
                    {
                        status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                    }
                }

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
            string data = null;

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

                Console.WriteLine("[OnConnectButtonClicked][Erro] Não foi possível ler os dados do dispositivo.");
                await DisplayAlert("Erro", "Não foi possível ler os dados do dispositivo.", "OK");
            }


            // Depois de fazer a leitura dos dados -> navega para a página que exibe os dados
            //if (data != null && data.Length > 0)
            //{
            //    await MainThread.InvokeOnMainThreadAsync(async () =>
            //    {
            //        Application.Current.UserAppTheme = AppTheme.Light;
            //        await Navigation.PushAsync(new RecordDisplayPage(_bluetoothService));
            //    });
            //}
            //else 
            //{
            //    Console.WriteLine("[OnConnectButtonClicked][Aviso] Nenhum dado encontrado para leitura.");
            //}
            _isProcessing = false;
        }
    }
}
