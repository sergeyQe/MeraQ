using ModbusRTUProject.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusRTUProject.Communication
{
    internal class Modbus
    {
        private readonly ICommunicator _communicator;

        public int ResponseTimeout {  get; set; } //определяет время ожидания ответа на запрос в мс

        public Modbus(ICommunicator communicator)
        {
            _communicator = communicator;
        }

        
        public ModbusResponse Write(byte deviceAddress, ushort registerAddress, byte[] data)
        {

        }

        public ModbusResponse Read(byte deviceAddress, ushort registerAddress, int byteCount)
        {

        }

         public static ushort Calculate(IEnumerable<byte>)
        {

        }

    }
}
