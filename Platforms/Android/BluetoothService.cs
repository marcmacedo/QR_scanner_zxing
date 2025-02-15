// BLUETOOTH BLE TEST

#if ANDROID
using Android.Net;
using AndroidX.Window.Layout;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Animations;
using Microsoft.Maui.Controls.Platform.Compatibility;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Plugin.BLE.Abstractions.Exceptions;
using Shiny.Reflection;
using System;
using System.Net.Mail;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Text;
using QR_scanner_zxing.Resources.Services;


namespace QR_scanner_zxing.Platforms.Android
{
    public class BluetoothService : IBluetoothService
    {
        private IDevice _connectedDevice;
        private Guid _serviceUuid;
        private Guid _characteristicUuid;
        private string _macAddress;
        int _countIndex;
        int _interval;

        uint _startDate;
        uint _endDate;

        double _currentTemp;


        ICharacteristic characteristicFFF3;


        public async Task<IDevice> ConnectToBleAsync(string macAddress)
        {
            Logger.Log("INFO", "[BluetoothService][ConnectToBleAsync] Iniciando conexão com dispositivo");
            var adapter = CrossBluetoothLE.Current.Adapter;
            int retry = 0;
            _macAddress = macAddress;

            adapter.DeviceAdvertised += HandleDeviceAdvertised;

            while (retry < 3)
            {
                try
                {
                    await adapter.StartScanningForDevicesAsync();

                    var device = adapter.DiscoveredDevices.FirstOrDefault(d => d.NativeDevice.ToString().Equals(macAddress, StringComparison.OrdinalIgnoreCase));

                    // Dispositivo existe
                    if (device != null)
                    {
                        await adapter.StopScanningForDevicesAsync();

                        try
                        {
                            if (device != null && device. State == Plugin.BLE.Abstractions.DeviceState.Disconnected)
                            {
                                await adapter.ConnectToDeviceAsync(device);
                            }
                            else if (device != null && device.State == Plugin.BLE.Abstractions.DeviceState.Connected)
                            {
                                await adapter.DisconnectDeviceAsync(device);
                                Console.WriteLine($"[ConnectToDeviceAsync] Estado do sensor Bluetooth: {device.State}");
                            }
                        }
                        catch (DeviceConnectionException ex)
                        {
                            Console.WriteLine($"[ConnectToDeviceAsync] Erro: {ex.Message}");
                            Logger.Log("ERROR", $"[BluetoothService][ConnectToBleAsync] {ex.Message}");

                        }



                        if (device != null && device.State == Plugin.BLE.Abstractions.DeviceState.Connected)
                        {
                            Console.WriteLine("[BluetoothService][ConnectToDeviceAsync] Conexão BLE realizada com sucesso.");
                            Logger.Log("INFO", "[BluetoothService][ConnectToDeviceAsync] Conexão BLE realizada com sucesso.");
                            _connectedDevice = device;
                            return device;
                        }
                    }
                    Console.WriteLine("[BluetoothService][ConnectToDeviceAsync] Falha ao conectar ao dispositivo.");
                    Logger.Log("info", "[BluetoothService][ConnectToDeviceAsync] Falha ao conectar ao dispositivo.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[BluetoothService][ConnectToDeviceAsync] Erro: {ex.Message}");
                    Logger.Log("ERROR", $"[BluetoothService][ConnectToDeviceAsync] {ex.Message}");

                }
                finally
                {
                    if (adapter.IsScanning)
                    {
                        await adapter.StopScanningForDevicesAsync();
                    }
                }

                Console.WriteLine($"[BluetoothService][ConnectToDeviceAsync][Retry] Conexão não estabelecida. Tentativa: {retry + 1} de 3.");
                Logger.Log("warning", $"[BluetoothService][ConnectToDeviceAsync][Retry] Conexão não estabelecida. Tentativa: {retry + 1} de 3.");
                retry++;
            }

            adapter.DeviceAdvertised -= HandleDeviceAdvertised;
            return null;
        }

        private void HandleDeviceAdvertised(object sender, DeviceEventArgs args)
        {
            var device = args.Device;
            if (device.NativeDevice.ToString().Equals(_macAddress, StringComparison.OrdinalIgnoreCase))
            {

                var records = device.AdvertisementRecords;

                foreach (var record in records)
                {
                    Console.WriteLine($"Ad Type: {record.Type}, Data: {BitConverter.ToString(record.Data)} || {record.Data} || {record.Data.Length}");

                    //ProcessAdvertisement(record.Data);
                }
            }
        }

        public void ProcessAdvertisement(byte[] data)
        {
            if (data.Length < 22)
            {
                //Console.WriteLine("Dados de advertisement incompletos.");
            }

            try
            {

                //Zen measure

                int pid = (data[0] << 8) + data[1];

                string mac = BitConverter.ToString(data, 2, 6).Replace("-", ":");
                var rfu = BitConverter.ToString(data, 8, 8);
                var bat = Convert.ToInt32(BitConverter.ToString(data, 17, 1), 16);

                var temperature = BitConverter.ToString(data, 18, 2).Replace("-", "");
                string temperature_hex = temperature.Substring(2, 2) + temperature.Substring(0, 2);
                double tempDecimal = Math.Round(int.Parse(temperature_hex, System.Globalization.NumberStyles.HexNumber) / 100.0, 2);
                _currentTemp = tempDecimal;

                Console.WriteLine($"PID {pid:X} | TagID: {mac} | Battery: {bat} | Temperature: {tempDecimal} | RFU: {rfu}");

                // ----------------------------
            }
            catch
            {

            }
        }
        public async Task<Dictionary<int, (long timestamp, double temp)>> ReadDataAsync(Guid serviceUuid, Guid characteristicUuid, IBluetoothService bluetoothService)
        {
            int totalIndex = 0;
            try
            {
                _serviceUuid = serviceUuid;
                _characteristicUuid = characteristicUuid;

                Logger.Log("info", "[BluetoothService][ReadDataAsync] Iniciando leitura de dados");

                Dictionary<int, (long timestamp, double temp)> allData = new Dictionary<int, (long timestamp, double temp)>();

                try
                {
                    await _connectedDevice.RequestMtuAsync(512);
                    Console.WriteLine("MTU concedido.");
                    Logger.Log("info", "[BluetoothService][ReadDataAsync] MTU Concedido");

                }
                catch
                {
                    Console.WriteLine("Não foi possível requerir o MTU do dispositivo.");
                    Logger.Log("info", "[BluetoothService][ReadDataAsync] Não foi possível requerir o MTU do dispositivo");

                }


                if (_connectedDevice == null)
                {
                    Logger.Log("Error", "[BluetoothService][ReadDataAsync] Nenhum dispositivo encontrado");
                    throw new InvalidOperationException("Nenhum dispositivo encontrado.");
                }


                var service = await _connectedDevice.GetServiceAsync(_serviceUuid);
                Logger.Log("Info", "[BluetoothService][ReadDataAsync] Coletando serviços");

                if (service == null)
                {
                    Logger.Log("Error", "[BluetoothService][ReadDataAsync] Nenhum serviço encontrado");
                    throw new Exception("Serviço não encontrado.");
                }


                var characteristics = await service.GetCharacteristicsAsync();

                foreach (var characteristic in characteristics)
                {
                    if (characteristics == null)
                    {
                        Console.WriteLine("Característica não encontrada.");
                        Logger.Log("Info", "[BluetoothService][ReadDataAsync] Coletando características do serviço");

                    }


                    if (characteristic.Id.ToString() == "0000fff1-0000-1000-8000-00805f9b34fb")
                    {
                        var countIndex = await characteristic.ReadAsync();
                        _countIndex = BitConverter.ToInt32(countIndex.Item1);
                        Console.WriteLine($"Valor do countIndex: {BitConverter.ToString(countIndex.Item1)}, {countIndex.Item1.Length}");
                        totalIndex = BitConverter.ToInt32(countIndex.Item1, 4); // Pegando o número do último índice em little endian
                        Console.WriteLine($"Último índice salvo no sensor: {totalIndex}");
                    }

                    if (characteristic.Id.ToString() == "0000fff2-0000-1000-8000-00805f9b34fb")
                    {
                        var storeIndex = await characteristic.ReadAsync();

                        ushort delay = BitConverter.ToUInt16(storeIndex.Item1, 2);
                        _startDate = BitConverter.ToUInt32(storeIndex.Item1, 4);
                        _endDate = BitConverter.ToUInt32(storeIndex.Item1, 8);
                        long interval = (BitConverter.ToUInt32(storeIndex.Item1, 8) - BitConverter.ToUInt32(storeIndex.Item1, 4)) / (_countIndex - 1);

                        // Procurando a menor diferença absoluta entre o resultado do cálculo de intervalo e os valores de config (ex: se o valor (em segundos) for 580, o valor será arredondado para 600, que possui a menor diferença absoluta entre os valores de configuração (expectedInterval)
                        _interval = (int)interval;
                        int[] expectedInterval = { 30, 60, 120, 300, 600 };

                        _interval = expectedInterval
                            .OrderBy(value => Math.Abs(value - _interval))
                            .First();
                        // ------

                        Console.WriteLine($"Valores do STOREINDEX: DELAY {delay}, INTERVAL {interval} | RAW DATA: {BitConverter.ToString(storeIndex.Item1)}");



                    }

                    if (characteristic.Id.ToString() == "0000fff3-0000-1000-8000-00805f9b34fb")
                    {
                        characteristicFFF3 = characteristic;
                    }


                    if (characteristic.Id.ToString() == "0000fff5-0000-1000-8000-00805f9b34fb")
                    {
                        var teste = await characteristic.ReadAsync();
                        Console.WriteLine($"Valor da característica que representa os limites de temperatura: {BitConverter.ToString(teste.Item1)}");
                    }
                }


                if (totalIndex > 0)
                {
                    var characteristicFFF4 = characteristics.FirstOrDefault(c => c.Id.ToString() == "0000fff4-0000-1000-8000-00805f9b34fb");
                    if (characteristicFFF4 != null)
                    {
                        var indexBytes = await characteristicFFF4.WriteAsync(new byte[] { 0x00, 0x00, 0x00, 0x00 });

                        Console.WriteLine($"Enviado para leitura ({indexBytes}).");
                    }
                }


                if (characteristicFFF3 != null)
                {
                    try
                    {
                        allData = new Dictionary<int, (long timestamp, double temp)>();
                        int globalIndex = 0;
                        characteristicFFF3.ValueUpdated += (sender, args) =>
                        {
                            var receivedData = args.Characteristic.Value;

                            for (int i = 4; i < receivedData.Length; i += 2) // i = 4 é o offset dos 4 primeiros bytes do array
                            {
                                if (i + 1 < receivedData.Length)
                                {
                                    ushort temp = BitConverter.ToUInt16(new byte[] { receivedData[i], receivedData[i + 1] });
                                    double value = Math.Round(temp / 100.0, 2);


                                    long timestamp = _endDate - (_interval * (_countIndex - globalIndex));

                                    if (allData.ContainsKey(globalIndex))
                                    {
                                        Console.WriteLine($"Chave duplicada detectada: {globalIndex} | {allData[globalIndex]}");

                                    }
                                    else
                                    {
                                        allData.Add(globalIndex, (timestamp, value));
                                    }
                                }
                                globalIndex++;
                            }
                        };

                        var descriptors = await characteristicFFF3.GetDescriptorsAsync();
                        var cccd = descriptors.FirstOrDefault(d => d.Id.ToString() == "00002902-0000-1000-8000-00805f9b34fb");
                        if (cccd != null)
                        {
                            await characteristicFFF3.StartUpdatesAsync();

                            var valor = new byte[] { 0x01 }; // Enviando true ao descriptor 2902 da FFF4
                            await cccd.WriteAsync(valor);
                            Console.WriteLine($"Escrita no CCCD bem sucedida. Valor enviado: {BitConverter.ToString(valor)}");
                        }
                    }

                    catch (Exception ex)
                    {
                        Console.WriteLine($"Não foi possível ativar as notificações {ex.Message}");
                    }
                }


                await Task.Delay(1000);
                
                if (characteristicFFF3 != null)
                {
                    await characteristicFFF3.StopUpdatesAsync();
                }

                await DisconnectAsync();
                return allData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReadDataAsync] Erro: {ex.Message}");
                return null;
            }
        }
            
        //private void TemperatureCharacteristic_ValueUpdated(object? sender, CharacteristicUpdatedEventArgs e)
        //{
        //    var data = e.Characteristic.Value;
        //    if (data != null && data.Length >= 6)
        //    {
        //        int rawTemperature = (data[0] & 0xFF) | (data[1] & 0xFF << 8);
        //        double temperature = rawTemperature / 100.0;

        //        long timestamp = (data[2] & 0xFFL) | ((data[3] & 0xFFL) << 8) | ((data[4] & 0xFFL) << 16) | ((data[5] & 0xFFL) << 24);

        //        Console.WriteLine($"Temperatura recebida: {temperature}");
        //    }
        //    else
        //    {
        //        Console.WriteLine("Dados de temperatura ou timestamp inválidos recebidos.");
        //    }
        //}

        //public string hexToString(string hexString)
        //{
        //    if (hexString == null || hexString.Length % 2 != 0)
        //    {
        //        throw new ArgumentException("A string hexadecimal deve ter um número par de dígitos", nameof(hexString));
        //    }

        //    var bytes = new byte[hexString.Length / 2];
        //    for (int i = 0; i < bytes.Length; i++)
        //    {
        //        bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
        //    }

        //    return Encoding.ASCII.GetString(bytes);
        //}

        public async Task DisconnectAsync()
        {
            try
            {
                if (_connectedDevice != null)
                {
                    var adapter = CrossBluetoothLE.Current.Adapter;
                    await adapter.DisconnectDeviceAsync(_connectedDevice);
                    _connectedDevice = null;
                    await Task.Delay(2000);
                    Console.WriteLine("[DisconnectAsync] Dispositivo desconectado.");
                    Logger.Log("Info", "[BluetoothService][DisconnectAsync] Dispositivo desconectado");

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DisconnectAsync] Erro: {ex.Message}");
                Logger.Log("Error", $"[BluetoothService][DisconnectAsync] {ex.Message}");
                
            }
        }
    }
}
#endif