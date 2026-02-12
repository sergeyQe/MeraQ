using ModbusRTUProject.Interfaces;
using System.IO.Ports;


namespace ModbusRTUProject.Communication
{
    public class ComPort : ICommunicator
    {
        private SerialPort _serialPort;
        private bool _disposed = false; //флаг переключения метода Dispose()
        private readonly object _lock = new object();

        public string PortName { get; set; } // Имя порта
        public int BaudRate  // Скорость передачи
        {
            get => _serialPort?.BaudRate ?? 19200;
            set
            {
                if (_serialPort != null)
                    _serialPort.BaudRate = value;
            }
        }

        public Parity Parity // бит четности
        {
            get => _serialPort?.Parity ?? Parity.None;
            set
            {
                if (_serialPort != null)
                    _serialPort.Parity = value;
            }
        }

        public int DataBits // количество бит данных
        {
            get => _serialPort?.DataBits ?? 8;
            set
            {
                if (_serialPort != null)
                    _serialPort.DataBits = value;
            }
        }
        public StopBits StopBits // стоп биты
        {
            get => _serialPort?.StopBits ?? StopBits.One;
            set
            {
                if (_serialPort != null)
                    _serialPort.StopBits = value;
            }
        }
        public int ReadTimeout
        {
            get
            {
                if (_serialPort == null)
                    throw new ObjectDisposedException(nameof(ComPort));
                return _serialPort.ReadTimeout;
            }
            set
            {
                if (_serialPort == null)
                    throw new ObjectDisposedException(nameof(ComPort));
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

            lock (_lock)
            {
                if (!IsPortNotNull() || !IsPortOpen())
                {
                    throw new InvalidOperationException("_serialPort равен null или не открыт");
                }

                try
                {


                    _serialPort.Write(buffer, 0, buffer.Length);
                    return buffer.Length;

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

        }

        public void DiscardIOBuffers()
        {
            DiscardIOBuffersInternal();
        }

        private void DiscardIOBuffersInternal()
        {
            if (!IsPortNotNull() || !IsPortOpen()) return;
            try
            {
                _serialPort.DiscardInBuffer();   // Очистка входного буфера
                _serialPort.DiscardOutBuffer();  // Очистка выходного буфера

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
                if (IsPortNotNull())
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


        private void ValidationPortForRead()
        {
            if (!IsPortNotNull()) throw new InvalidOperationException("Порт не должен быть null");
            if (!IsPortOpen()) throw new InvalidOperationException("COM-порт закрыт. Чтение не возможно");
        }


        private bool IsPortOpen() => _serialPort?.IsOpen ?? false;

        private bool IsPortNotNull() => _serialPort != null;




    }
}
