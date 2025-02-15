using Microsoft.Extensions.DependencyInjection;
using QR_scanner_zxing.Platforms.Android;
namespace QR_scanner_zxing
{
    public partial class App : Application
    {
        public static IBluetoothService BluetoothService { get; set; }
        public App(IBluetoothService bluetoothService)
        {
            InitializeComponent();
            //Application.Current.UserAppTheme = AppTheme.Dark;

            BluetoothService = bluetoothService;

            MainPage = new NavigationPage(new QRCodeScannerPage(bluetoothService));

            //var qrPage = MauiProgram.CreateMauiApp().Services.GetRequiredService<QRCodeScannerPage>();
            //MainPage = new NavigationPage(qrPage);

        }
    }
}