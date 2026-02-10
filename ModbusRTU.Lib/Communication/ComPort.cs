using ModbusRTUProject.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusRTUProject.Communication
{
    internal class ComPort : ICommunicator
    {
        /// </summary>
        public void Close()
        {
            if (_serialPort == null)
            {
                return;
            }

            try
            {
                if (_serialPort.IsOpen)
                {
                    DiscardIOBuffers(); // чистим порт
                    _serialPort.Close(); // закрываем порт
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                _serialPort.Dispose(); // Освобождаем ресурсы
                _serialPort = null;
            }

        }

        public int Write(byte[] buffer)
        {
            if (buffer == null && buffer.Length == 0)
            {
                throw new ArgumentNullException(nameof(buffer),
                    "Массив данных для отправки пустой.");
        }

        public void DiscardIOBuffers()
        {
            throw new NotImplementedException();
        }

        public void Open()
        {
            throw new NotImplementedException();
        }

        public byte[] Read(byte[] buffer)
        {
            throw new NotImplementedException();
        }

        public byte[] Read(int byteCount)
        {
            throw new NotImplementedException();
        }

        public int Write(byte[] buffer)
        {
            throw new NotImplementedException();
        }
    }
}
