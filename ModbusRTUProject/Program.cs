
using ModbusRTUProject.Communication;
using ModbusRTUProject.Interfaces;

namespace ModbusRTUProject
{
    public class Program
    {
        public static void Main(string[] args)
        {

            Console.WriteLine("Проект демонстрация");
            
            while (true)
            {
                Console.WriteLine("Выбор меню\n" +
                             "0 - для тестового режима без реальных портов\n" +
                             "1 - для теста с запросом к реальным портам\n " +
                             "любой другой символ или строка выход\n");

                if (int.TryParse(Console.ReadLine(), out var idCommand))
                {
                    switch (idCommand)
                    {
                        case 0:
                            DemoCrcCalculation();
                            break;
                        case 1:
                            TestWithPorts();
                            break;
                        default:
                            Exit();
                            return;
                    }
                }
                else
                {
                    Exit();
                    return;
                }

                StopIteration();
            }

        }

        /// <summary>
        /// Печать выхода из программы
        /// </summary>
        private static void Exit()
        {
            Console.WriteLine("Выход из программы");
        }

        /// <summary>
        /// Получения названия порта
        /// </summary>
        private static string GetPortName(string[] ports)
        {
            string portName = Console.ReadLine();
            if (string.IsNullOrEmpty(portName)) portName = ports[0];
            return portName;
        }

        /// <summary>
        /// Печать списков портов
        /// </summary>
        private static void PrintListPorts(string[] ports)
        {
            foreach (var port in ports)
            {
                Console.WriteLine($"  {port}");
            }
        }

        /// <summary>
        /// Демонстрация тестирования с реальными портами
        /// </summary>
        private static void TestWithPorts()
        {
            string[] ports = System.IO.Ports.SerialPort.GetPortNames();
            Console.WriteLine("Доступные COM-порты:");
            if (ports.Length == 0)
            {
                Console.WriteLine("  Нет доступных портов");
                Console.WriteLine("Для тестирования нужно использовать виртуальные COM-порты.");
                return;
            }

            PrintListPorts(ports); //Вывод списка всех портов

            Console.WriteLine("Введите имя выбранного порта (например, COM3): или будет выбран порт по умолчанию");
            string portName = GetPortName(ports);

            try
            {
                using ICommunicator comPort = new ComPort(portName) // Создаем порт с автоматическим вызовом Dispose()
                {
                    BaudRate = 9600,
                    ReadTimeout = 1000
                };

                var modbus = new Modbus(comPort)
                {
                    ResponseTimeout = 1000
                };

                comPort.Open();
                Console.WriteLine($"Порт {portName} открыт успешно.");

                // Демонстрация чтения регистров
                DemoReadRegisters(modbus);

                // Демонстрация записи регистров
                DemoWriteRegisters(modbus);

                comPort.Close();
                Console.WriteLine("Порт закрыт.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }

        }

        
        /// <summary>
        /// Демонстрация чтения регистров (функция 0x04)
        /// </summary>
        private static void DemoReadRegisters(Modbus modbus)
        {
            Console.WriteLine("--- Чтение регистров (функция 0x04) ---");

            byte deviceAddress = 1;
            ushort registerAddress = 0x0000;
            int byteCount = 4; // 2 регистра

            Console.WriteLine($"Адрес устройства: {deviceAddress}");
            Console.WriteLine($"Адрес регистра: 0x{registerAddress:X4}");
            Console.WriteLine($"Количество байт: {byteCount}");

            try
            {
                ModbusResponse response = modbus.Read(deviceAddress, registerAddress, byteCount);

                if (response.IsSuccess)
                {
                    Console.WriteLine($"Успех! Данные: {BitConverter.ToString(response.Data)}");
                }
                else
                {
                    Console.WriteLine($"Ошибка: {response.GetErrorDescription()}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Исключение: {ex.Message}");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Демонстрация записи регистров (функция 0x10)
        /// </summary>
        private static void DemoWriteRegisters(Modbus modbus)
        {
            Console.WriteLine("--- Запись регистров (функция 0x10) ---");

            byte deviceAddress = 1;
            ushort registerAddress = 0x0010;
            byte[] data = { 0x00, 0x0A, 0x01, 0x02 }; // 2 регистра

            Console.WriteLine($"Адрес устройства: {deviceAddress}");
            Console.WriteLine($"Адрес регистра: 0x{registerAddress:X4}");
            Console.WriteLine($"Данные: {BitConverter.ToString(data)}");

            try
            {
                ModbusResponse response = modbus.Write(deviceAddress, registerAddress, data);

                if (response.IsSuccess)
                {
                    Console.WriteLine("Успех! Данные записаны.");
                }
                else
                {
                    Console.WriteLine($"Ошибка: {response.GetErrorDescription()}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Исключение: {ex.Message}");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Демонстрация расчёта CRC-16 Modbus
        /// </summary>
        private static void DemoCrcCalculation()
        {
            PrintTestWithOutPort();
            Console.WriteLine(" Расчёт CRC-16 Modbus");

            byte[] testData = { 0x01, 0x04, 0x00, 0x00, 0x00, 0x02 };
            ushort crc = Modbus.Calculate(testData);

            Console.WriteLine($"Данные: {BitConverter.ToString(testData)}");
            Console.WriteLine($"CRC-16: 0x{crc:X4}");
            Console.WriteLine($"CRC (low byte): 0x{(crc & 0xFF):X2}");
            Console.WriteLine($"CRC (high byte): 0x{(crc >> 8):X2}");
        }

        /// <summary>
        /// Печать приветсвия  для тестового режима без портов
        /// </summary>
        private static void PrintTestWithOutPort()
        {
            Console.WriteLine("Запуск тестового режима без реальных портов");
        }

        /// <summary>
        /// Остановка перед следующим циклом
        /// </summary>
        private static void StopIteration()
        {
            Console.WriteLine("Нажмите любую клавишу для возврата...\n");
            Console.ReadKey();
        }
    }
}


