using ModbusRTU.Lib.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusRTUProject.Communication
{
    internal class ModbusResponce
    {
        public ExceptionCode Error { get; set; }

        public byte[] Data { get; set; }
    }
}
