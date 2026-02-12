using ModbusRTUProject.Interfaces;
using System.IO.Ports;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ModbusRTUProject.Communication
{
    public class ComPort : ICommunicator
    {
        private SerialPort _serialPort;
        private bool _disposed = false; //флаг переключения метода Dispose()
        private readonly object _lock = new object();

        public string PortName { get; set; } // Имя порта
        public int BaudRate { get; set; } = 19200;  // Скорость передачи
        public Parity Parity { get; set; } = Parity.None; // бит четности
        public int DataBits { get; set; } = 8; // количество бит данных
        public StopBits StopBits { get; set; } = StopBits.One;  // стоп биты
        public int ReadTimeout
        {
            get => _serialPort.ReadTimeout;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Таймаут должен быть больше 0");
                _serialPort.ReadTimeout = value;
            }
        }

        public ComPort(string portName)
        {
            PortName = portName;
            _serialPort = new SerialPort(portName, BaudRate, Parity, DataBits, StopBits);
            _serialPort.ReadTimeout = 1000;
            _serialPort.WriteTimeout = 1000;
        }


        public void Open()
        {
            lock (_lock)
            {
                if (_serialPort.IsOpen)
                {
                    throw new InvalidOperationException($"COM-порт {_serialPort.PortName} уже открыт.");
                }
                try
                {
                    _serialPort.Open();
                    DiscardIOBuffers(); //чистим порт

                }

                catch (Exception ex)
                {
                    throw new Exception($"ошибка при открытии порта{_serialPort.PortName}", ex);
                }
            }
        }

        /// <summary>
        /// Закрывает последовательный порт и освобождает ресурсы
        /// finally для гарантированной очистки
        /// </summary>
        public void Close()
        {

            lock (_lock)
            {
                if (!IsPortNotNull()) return;

                try
                {
                    if (IsPortOpen())
                    {
                        DiscardIOBuffers(); // чистим порт
                        _serialPort.Close(); // закрываем порт
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"ошибка при закрытии  порта {_serialPort.PortName}", ex);
                }

            }

        }

        public int Write(byte[] buffer)
        {
            if (buffer == null || buffer.Length == 0)
            {
                throw new ArgumentNullException(nameof(buffer),
                    "Массив данных для отправки пустой.");
            }


            if (!IsPortNotNull() || !IsPortOpen())
            {
                throw new InvalidOperationException("_serialPort равен null или не открыт");
            }

            try
            {
                lock (_lock)
                {
                    _serialPort.Write(buffer, 0, buffer.Length);
                    return buffer.Length;
                }
            }
            catch (TimeoutException ex)
            {
                throw new TimeoutException($"Таймаут записи в порт {PortName}.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при записи {ex.Message}");
            }

        }

        public void DiscardIOBuffers()
        {
            if (!IsPortNotNull() || !IsPortOpen()) return;
            try
            {
                lock (_lock)
                {
                    _serialPort.DiscardInBuffer();   // Очистка входного буфера
                    _serialPort.DiscardOutBuffer();  // Очистка выходного буфера
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка в методе DiscardIOBuffers()", ex);
            }
        }


        public byte[] Read()
        {

            ValidationPortForRead();
            lock (_lock)
            {
                try
                {
                    // количество байтов, доступных для чтения
                    int bytesToRead = _serialPort.BytesToRead;

                    if (bytesToRead == 0) return Array.Empty<byte>();


                    //  Создаем массив нужного размера
                    byte[] buffer = new byte[bytesToRead];

                    //  Читаем  данные и возвращаем реальное количество байт
                    int bytesRead = _serialPort.Read(buffer, 0, bytesToRead);

                    ///<summary>
                    ///Если прочитано меньше, то удаляем лишнее
                    ///делаем копирование в массив меньшей длины
                    ///</summary>///
                    if (bytesRead != bytesToRead)
                    {
                        byte[] actualData = new byte[bytesRead];
                        Array.Copy(buffer, 0, actualData, 0, bytesRead);
                        buffer = actualData;
                    }

                    return buffer;
                }
                catch (Exception ex)
                {
                    throw new Exception($"ошибка при чтении данных из порта {_serialPort.PortName}.", ex);
                }
            }

        }

        public byte[] Read(int byteCount)
        {
            if (byteCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(byteCount), "Количество байтов должно быть больше 0.");
            }

            ValidationPortForRead();

            if (_serialPort.ReadTimeout <= 0) throw new InvalidOperationException("Таймаут должен быть больше нуля");

            byte[] buffer = new byte[byteCount]; //инициализация массива для чтения
            int totalBytesRead = 0; //количество прочитанных
            DateTime startTime = DateTime.UtcNow; //таймер для старта 
            lock (_lock)
            {
                try
                {
                    while (totalBytesRead < byteCount)
                    {
                        TimeSpan elapsed = DateTime.UtcNow - startTime; //сколько времени идет цикл
                        if (elapsed.TotalMilliseconds > _serialPort.ReadTimeout)
                        {
                            throw new TimeoutException($"Сработал таймер общего чтения. Прочитано {totalBytesRead} байтов за {elapsed.TotalMilliseconds}мс.");
                        }

                        int bytesToReadThisIteration = byteCount - totalBytesRead; // количество осталось прочитать
                        int bytesRead = _serialPort.Read(buffer, totalBytesRead, bytesToReadThisIteration); // чтение

                        if (bytesRead <= 0) throw new IOException("метод Read вернул 0 байт"); //защита от пустого чтения и бесконечного цикла

                        totalBytesRead += bytesRead; //увеличиваем счетчик прочитанных

                    }

                }

                catch (TimeoutException ex)
                {
                    throw new TimeoutException($"Сработал таймаут. Ожидалось {byteCount}, получено {totalBytesRead}.", ex);
                }
                catch (Exception ex)
                {
                    throw new Exception($"ошибка при чтении данных из порта {_serialPort.PortName}.", ex);
                }

                return buffer;
            }

        }

        public void Dispose()
        {
            if (_disposed)
                return;

            lock (_lock)
            {
                if (!IsPortNotNull())
                {
                    try
                    {
                        if (IsPortOpen())
                        {
                            DiscardIOBuffers();
                            _serialPort.Close();
                        }
                        _serialPort.Dispose();
                    }
                    catch
                    {
                        // Игнорируем
                    }
                    finally
                    {
                        _serialPort = null;
                    }
                }

                _disposed = true;
            }
        }


        public void ValidationPortForRead()
        {
            if (!IsPortNotNull()) throw new InvalidOperationException("Порт не должен быть null");
            if (!IsPortOpen()) throw new InvalidOperationException("COM-порт закрыт. Чтение не возможно");
        }


        private bool IsPortOpen() => _serialPort.IsOpen;

        private bool IsPortNotNull() => _serialPort != null;




    }
}
