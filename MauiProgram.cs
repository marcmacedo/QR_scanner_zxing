using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
using QR_scanner_zxing;
using QR_scanner_zxing.Platforms;
using QR_scanner_zxing.Platforms.Android;
using System;
using System.IO;
using Microsoft.Maui.Storage;



namespace QR_scanner_zxing
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseBarcodeReader();
#if ANDROID
            builder.Services.AddSingleton<IBluetoothService, BluetoothService>();
#endif
            builder.Services.AddTransient<QRCodeScannerPage>();
            //builder.Services.AddSingleton<App>();
            return builder.Build();
        }
    }
}