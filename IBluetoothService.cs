using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin.BLE.Abstractions.Contracts;
using QR_scanner_zxing;

namespace QR_scanner_zxing.Platforms.Android
{
    public interface IBluetoothService
    {
        Task<IDevice> ConnectToBleAsync(string macAddress);
        Task<Dictionary<int, (long timestamp, double temp)>> ReadDataAsync(Guid serviceUuid, Guid characteristicUuid, IBluetoothService bluetoothService);
        Task DisconnectAsync();
    }
}
