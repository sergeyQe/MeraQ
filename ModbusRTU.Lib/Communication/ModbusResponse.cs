using ModbusRTU.Lib.Enums;


namespace ModbusRTUProject.Communication
{
    public class ModbusResponse
    {
        public ExceptionCode Error { get; set; } // Код ошибки в соответствии со спецификацией протокола Modbus RTU

        public byte[] Data { get; set; }  // Массив байтов ответа на запрос

        public ModbusResponse()
        {
            Error = ExceptionCode.Success;
            Data = Array.Empty<byte>();
        }

        public ModbusResponse(ExceptionCode error, byte[] data)
        {
            Error = error;
            Data = data ?? Array.Empty<byte>();
        }

        // Является ли ответ успешным
        public bool IsSuccess => Error == ExceptionCode.Success;

        // Возвращает текстовое описание ошибки
        public string GetErrorDescription()
        {
            return Error switch
            {
                ExceptionCode.Success => "Успешно",
                ExceptionCode.IllegalFunction => "Недопустимый код функции",
                ExceptionCode.IllegalDataAddress => "Недопустимый адрес данных",
                ExceptionCode.IllegalDataValue => "Недопустимое значение данных",
                ExceptionCode.SlaveDeviceFailure => "Ошибка устройства",
                ExceptionCode.Acknowledge => "Операция выполняется",
                ExceptionCode.SlaveDeviceBusy => "Устройство занято",
                ExceptionCode.NegativeAcknowledge => "Отрицательное подтверждение",
                ExceptionCode.MemoryParityError => "Ошибка чётности памяти",
                ExceptionCode.GatewayPathUnavailable => "Маршрут недоступен",
                ExceptionCode.GatewayTargetDeviceFailedToRespond => "Шлюз не отвечает",
                _ => $"Неизвестная ошибка (0x{(byte)Error:X2})"
            };
        }

        // Перегрузка для красивого вывода
        public override string ToString()
        {
            if (IsSuccess)
            {
                string dataHex = Data.Length > 0 ? BitConverter.ToString(Data) : "пусто";
                return $"ModbusResponse: Успех, Data[{Data.Length}] = {dataHex}";
            }
            return $"ModbusResponse: Ошибка = {Error} ({GetErrorDescription()})";
        }

    }
}
