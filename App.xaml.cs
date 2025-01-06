using Microsoft.Extensions.DependencyInjection;
namespace QR_scanner_zxing
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            Application.Current.UserAppTheme = AppTheme.Dark;

            var qrPage = MauiProgram.CreateMauiApp().Services.GetRequiredService<QRCodeScannerPage>();
            MainPage = new NavigationPage(qrPage);

        }
    }
}