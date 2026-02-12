using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusRTUProject.Interfaces
{
    public interface ICommunicator : IDisposable
    {
        void Open(); //подготовить удаленный порт данных для общения

        void Close(); //сообщить удаленному порту данных, что дальнейшего общения не требуется

        int Write(byte[] buffer); //передать на удаленный порт массив байтов, результат – количество переданных байтов;

        byte[] Read(); //получить от удаленного порта массив байтов, результат – массив принятых байтов;

        byte[] Read(int byteCount); // получить от удаленного порта заданное количество байтов byteCount, результат – массив принятых байтов.

        void DiscardIOBuffers(); //очистить буфер приема/передачи данных

        int ReadTimeout { get; set; } //таймаут

    }

    


}
