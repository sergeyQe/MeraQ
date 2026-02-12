using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusRTU.Lib.Enums
{

    public enum ExceptionCode : byte
    {
        Success = 0x00, // Успех

        IllegalFunction = 0x01, // Недопустимый код функции

        IllegalDataAddress = 0x02, // Недопустимый адрес данных

        IllegalDataValue = 0x03, // Недопустимое значение данных

        SlaveDeviceFailure = 0x04, // Ошибка устройства

        Acknowledge = 0x05, // операция выполняется

        SlaveDeviceBusy = 0x06, // Устройство занято

        NegativeAcknowledge = 0x07, // Отрицательное подтверждение

        MemoryParityError = 0x08, // Ошибка четности памяти

        GatewayPathUnavailable = 0x0A, //маршрут недоступен

        GatewayTargetDeviceFailedToRespond = 0x0B //Шлюз не отвечает
    }


}
