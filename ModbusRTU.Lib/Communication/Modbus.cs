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

        public Modbus(ICommunicator communicator)
        {
            _communicator = communicator;
        }


    }
}
