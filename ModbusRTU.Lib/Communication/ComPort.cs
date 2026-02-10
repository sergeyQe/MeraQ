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
            throw new NotImplementedException();
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
