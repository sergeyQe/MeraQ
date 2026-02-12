using ModbusRTU.Lib.Enums;
using ModbusRTUProject.Interfaces;

namespace ModbusRTUProject.Communication
{
    public class Modbus
    {
        private readonly ICommunicator _communicator;

        public int ResponseTimeout { get; set; } //определяет время ожидания ответа на запрос в мс

        public Modbus(ICommunicator communicator)
        {
            _communicator = communicator;
            ResponseTimeout = 1000; //значение по умолчанию
        }


        public ModbusResponse Write(byte deviceAddress, ushort registerAddress, byte[] data)
        {
            ValidateDeviceAddress(deviceAddress);
            ValidateRegisterAddress(registerAddress);
            ValidateDataForWrite(data);

            // Определяем количество регистров для записи (каждый регистр = 2 байта)
            ushort registerCount = (ushort)(data.Length / 2);

            // Создаем Modbus RTU запрос для функции 0x10 (Write Multiple Registers)
            byte[] request = CreateWriteRequest(deviceAddress, registerAddress, registerCount, data);

            // Отправляем запрос и получаем ответ
            byte[] response = SendRequest(request, 8);

            // Обрабатываем ответ
            return ProcessWriteResponse(response, deviceAddress, registerAddress, registerCount);
        }



        public ModbusResponse Read(byte deviceAddress, ushort registerAddress, int byteCount)
        {
            ValidateDeviceAddress(deviceAddress);
            ValidateRegisterAddress(registerAddress);

            // Проверяем корректность количества байт
            if (byteCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(byteCount),
                    $"Количество байт должно быть положительным. Получено: {byteCount}");
            }

            // Данные должны содержать четное количество байтов (регистры по 2 байта)
            if (byteCount % 2 != 0)
            {
                throw new ArgumentException(
                    $"Количество байт должно быть четным (регистры по 2 байта). Получено: {byteCount} байт",
                    nameof(byteCount));
            }

            // Проверяем максимальное количество регистров согласно спецификации Modbus для функции 0x04
            // Максимум 125 регистров * 2 байта = 250 байт
            if (byteCount > 250)
            {
                throw new ArgumentOutOfRangeException(nameof(byteCount),
                    $"Количество байт ({byteCount}) превышает максимальное (250 байт или 125 регистров)");
            }

            // Определяем количество регистров для чтения (каждый регистр = 2 байта)
            ushort registerCount = (ushort)(byteCount / 2);

            // Создаем Modbus RTU запрос для функции 0x04 (Read Input Registers)
            byte[] request = CreateReadRequest(deviceAddress, registerAddress, registerCount);

            int expectedResponceLength = 3 + byteCount + 2; //ожидаемая длина
            // Отправляем запрос и получаем ответ
            byte[] response = SendRequest(request, expectedResponceLength);

            // Обрабатываем ответ
            return ProcessReadResponse(response, deviceAddress, registerAddress, registerCount, byteCount);
        }

        

        public static ushort Calculate(IEnumerable<byte> data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data), "Массив данных не может быть null");
            ushort crc = 0xFFFF; // Начальное значение CRC
            foreach (byte b in data)
            {
                crc ^= b; // XOR с текущим байтом

                for (int i = 0; i < 8; i++)
                {
                    bool lsb = (crc & 0x0001) != 0; // Проверяем младший бит
                    crc >>= 1; // Сдвигаем вправо на 1 бит

                    if (lsb)
                    {
                        crc ^= 0xA001; // Полином Modbus CRC-16 (0xA001)
                    }
                }
            }

            return crc;

        }


        /// <summary>
        /// Обрабатывает ответ на запрос чтения регистров (функция 0x04)
        /// </summary>
        private ModbusResponse ProcessReadResponse(byte[] response, byte deviceAddress, ushort registerAddress, ushort registerCount, int expectedByteCount)
        {
            // Проверяем минимальную длину ответа
            if (response.Length < 5) // Адрес + функция + кол-во байт + CRC (минимум)
            {
                throw new InvalidOperationException(
                    $"Некорректная длина ответа: {response.Length} байт. Ожидается минимум 5 байт.");
            }

            // Проверяем адрес устройства
            if (response[0] != deviceAddress)
            {
                throw new InvalidOperationException(
                    $"Несоответствие адреса устройства. Ожидался: {deviceAddress}, получен: {response[0]}");
            }

            // Проверяем код функции
            byte functionCode = response[1];

            // Если установлен старший бит (0x80), это код исключения
            if ((functionCode & 0x80) != 0)
            {
                byte exceptionCode = response[2];
                return new ModbusResponse
                {
                    Error = (ExceptionCode)exceptionCode,
                    Data = Array.Empty<byte>()
                };
            }

            // Проверяем, что это ответ на функцию 0x04
            if (functionCode != 0x04)
            {
                throw new InvalidOperationException(
                    $"Некорректный код функции в ответе: 0x{functionCode:X2}. Ожидался: 0x04");
            }

            // Получаем количество байт данных в ответе
            byte dataByteCount = response[2];

            // Проверяем соответствие ожидаемому количеству байт
            if (dataByteCount != expectedByteCount)
            {
                throw new InvalidOperationException(
                    $"Несоответствие количества байт данных. Ожидалось: {expectedByteCount}, получено: {dataByteCount}");
            }

            // Проверяем полную длину ответа
            int expectedTotalLength = 3 + dataByteCount + 2; // Адрес + функция + кол-во байт + данные + CRC
            if (response.Length != expectedTotalLength)
            {
                throw new InvalidOperationException(
                    $"Некорректная длина ответа: {response.Length} байт. Ожидается: {expectedTotalLength} байт");
            }

            // Проверяем CRC
            ValidateCRC(response);

            // Извлекаем данные
            byte[] data = new byte[dataByteCount];
            Array.Copy(response, 3, data, 0, dataByteCount);

            return new ModbusResponse
            {
                Error = ExceptionCode.Success,
                Data = data
            };
        }

        /// <summary>
        /// Создает Modbus RTU запрос для функции записи нескольких регистров (0x10)
        /// </summary>
        private byte[] CreateWriteRequest(byte deviceAddress, ushort registerAddress, ushort registerCount, byte[] data)
        {
            // Проверка максимального количества регистров согласно спецификации Modbus
            if (registerCount > 123)
            {
                throw new ArgumentOutOfRangeException(nameof(data),
                    $"Количество регистров ({registerCount}) превышает максимально допустимое (123)");
            }
            // Формируем PDU (Protocol Data Unit) запроса
            // Длина PDU = 1 байт (код функции) + 5 байт (адрес регистра + кол-во регистров + кол-во байт) + данные
            int pduLength = 6 + data.Length; // 6 = 1 + 2 + 2 + 1
            byte[] pdu = new byte[pduLength];

            int index = 0;

            // Код функции (0x10 = Write Multiple Registers)
            pdu[index++] = 0x10;

            // Адрес первого регистра (2 байта, старший байт первый)
            pdu[index++] = (byte)(registerAddress >> 8);    // Старший байт
            pdu[index++] = (byte)(registerAddress & 0xFF);  // Младший байт

            // Количество регистров (2 байта, старший байт первый)
            pdu[index++] = (byte)(registerCount >> 8);      // Старший байт
            pdu[index++] = (byte)(registerCount & 0xFF);    // Младший байт

            // Количество байтов данных
            pdu[index++] = (byte)data.Length;

            // Данные для записи (копируем как есть, в порядке high-byte first для каждого регистра)
            Array.Copy(data, 0, pdu, index, data.Length);

            // Добавляем адрес устройства в начало, чтобы получить ADU (Application Data Unit)
            byte[] adu = new byte[1 + pdu.Length + 2]; // Адрес + PDU + CRC
            adu[0] = deviceAddress;
            Array.Copy(pdu, 0, adu, 1, pdu.Length);

            // Рассчитываем и добавляем контрольную сумму CRC-16
            int bytesForCrc = 1 + pduLength; // Адрес + PDU
            ushort crc = Calculate(adu.Take(bytesForCrc).ToArray());
            adu[bytesForCrc] = (byte)(crc & 0xFF);      // Младший байт CRC
            adu[bytesForCrc + 1] = (byte)(crc >> 8);    // Старший байт CRC

            return adu;
        }

        /// <summary>
        /// Отправляет запрос и получает ответ
        /// </summary>
        private byte[] SendRequest(byte[] request, int expectedResponceLength)
        {
            try
            {
                // Отправляем запрос
                _communicator.Write(request);


                ResponseTimeout = _communicator.ReadTimeout;
                // Читаем ответ
                return _communicator.Read(expectedResponceLength);
            }
            catch (TimeoutException ex)
            {
                throw new TimeoutException($"Сработал таймаут при ожидании ответа от устройства.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка при отправке/получении данных: {ex.Message}", ex);
            }
        }

        private byte[] CreateReadRequest(byte deviceAddress, ushort registerAddress, ushort registerCount)
        {
            // Формируем PDU (Protocol Data Unit) запроса
            // Длина PDU = 1 байт (код функции) + 4 байта (адрес регистра + кол-во регистров)
            int pduLength = 5; // 1 + 4
            byte[] pdu = new byte[pduLength];

            int index = 0;

            pdu[index++] = 0x04;

            // Адрес первого регистра (2 байта, старший байт первый)
            pdu[index++] = (byte)(registerAddress >> 8);    // Старший байт
            pdu[index++] = (byte)(registerAddress & 0xFF);  // Младший байт

            // Количество регистров (2 байта, старший байт первый)
            pdu[index++] = (byte)(registerCount >> 8);      // Старший байт
            pdu[index++] = (byte)(registerCount & 0xFF);    // Младший байт

            // Добавляем адрес устройства в начало, чтобы получить ADU (Application Data Unit)
            byte[] adu = new byte[1 + pdu.Length + 2]; // Адрес + PDU + CRC
            adu[0] = deviceAddress;
            Array.Copy(pdu, 0, adu, 1, pdu.Length);

            // Рассчитываем и добавляем контрольную сумму CRC-16
            int bytesForCrc = 1 + pduLength; // Адрес + PDU
            ushort crc = Calculate(adu.Take(bytesForCrc).ToArray());
            adu[bytesForCrc] = (byte)(crc & 0xFF);      // Младший байт CRC
            adu[bytesForCrc + 1] = (byte)(crc >> 8);    // Старший байт CRC

            return adu;
        }

        private ModbusResponse ProcessWriteResponse(byte[] response, byte deviceAddress, ushort registerAddress, ushort registerCount)
        {
            // Проверяем минимальную длину ответа
            if (response.Length < 8) // Адрес + функция + адрес регистра + кол-во регистров + CRC
            {
                throw new InvalidOperationException(
                    $"Некорректная длина ответа: {response.Length} байт. Ожидается минимум 8 байт.");
            }

            // Проверяем адрес устройства
            if (response[0] != deviceAddress)
            {
                throw new InvalidOperationException(
                    $"Несоответствие адреса устройства. Ожидался: {deviceAddress}, получен: {response[0]}");
            }

            // Проверяем код функции
            byte functionCode = response[1];

            // Если установлен старший бит (0x80), это код исключения
            if ((functionCode & 0x80) != 0)
            {
                byte exceptionCode = response[2];
                return new ModbusResponse
                {
                    Error = (ExceptionCode)exceptionCode,
                    Data = Array.Empty<byte>()
                };
            }

            // Проверяем, что это ответ на функцию 0x10
            if (functionCode != 0x10)
            {
                throw new InvalidOperationException(
                    $"Некорректный код функции в ответе: 0x{functionCode:X2}. Ожидался: 0x10");
            }

            // Проверяем CRC
            ValidateCRC(response);

            // Проверяем адрес регистра в ответе
            ushort receivedRegisterAddress = (ushort)((response[2] << 8) | response[3]);
            if (receivedRegisterAddress != registerAddress)
            {
                throw new InvalidOperationException(
                    $"Несоответствие адреса регистра. Ожидался: {registerAddress}, получен: {receivedRegisterAddress}");
            }

            // Возвращаем успешный ответ
            return new ModbusResponse
            {
                Error = ExceptionCode.Success,
                Data = new byte[] { response[2], response[3], response[4], response[5] } // Адрес регистра и кол-во регистров
            };
        }

        /// <summary>
        /// Валидирует адрес устройства
        /// </summary>
        private void ValidateDeviceAddress(byte deviceAddress)
        {
            if (deviceAddress < 1 || deviceAddress > 247)
            {
                throw new ArgumentOutOfRangeException(nameof(deviceAddress),
                    $"Адрес устройства должен быть в диапазоне 1-247. Получено: {deviceAddress}");
            }
        }

        /// <summary>
        /// Валидирует адрес регистра
        /// </summary>
        private void ValidateRegisterAddress(ushort registerAddress)
        {
            if (registerAddress > 0xFFFF)
            {
                throw new ArgumentOutOfRangeException(nameof(registerAddress),
                    $"Адрес регистра должен быть в диапазоне 0x0000-0xFFFF. Получено: 0x{registerAddress:X4}");
            }
        }

        /// <summary>
        /// Валидирует данные для записи
        /// </summary>
        private void ValidateDataForWrite(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data), "Массив данных не может быть null");
            }

            if (data.Length == 0)
            {
                throw new ArgumentException("Массив данных не может быть пустым", nameof(data));
            }

            // Данные должны содержать четное количество байтов (регистры по 2 байта)
            if (data.Length % 2 != 0)
            {
                throw new ArgumentException(
                    $"Количество байтов в данных должно быть четным (регистры по 2 байта). Получено: {data.Length} байт",
                    nameof(data));
            }

            // Проверяем максимальный размер данных согласно спецификации Modbus
            // Максимум 123 регистра * 2 байта = 246 байт
            if (data.Length > 246)
            {
                throw new ArgumentOutOfRangeException(nameof(data),
                    $"Размер данных ({data.Length} байт) превышает максимальный (246 байт или 123 регистра)");
            }
        }





        private void ValidateCRC(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data), "Массив данных не может быть null");
            if (data.Length < 2)
                throw new InvalidOperationException("Данные слишком короткие для проверки CRC");

            // Вычисляем CRC для всех байтов кроме последних двух (где находится CRC)
            byte[] dataWithoutCrc = new byte[data.Length - 2];
            Array.Copy(data, 0, dataWithoutCrc, 0, data.Length - 2);

            ushort calculatedCrc = Calculate(dataWithoutCrc);

            // Извлекаем CRC из ответа (последние 2 байта, порядок little-endian для Modbus)
            ushort receivedCrc = (ushort)((data[data.Length - 1] << 8) | data[data.Length - 2]);

            if (calculatedCrc != receivedCrc)
            {
                throw new InvalidOperationException(
                    $"Ошибка контрольной суммы. Вычислено: 0x{calculatedCrc:X4}, получено: 0x{receivedCrc:X4}");
            }
        }
    }
}
