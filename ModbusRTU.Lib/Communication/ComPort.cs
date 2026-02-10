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



        public byte[] Read()
        {

            ValidationPortNullOrCloseRead(_serialPort); //валидация на 

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
            throw new NotImplementedException();
        }


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
