using System.IO.Ports;

namespace ControlPanel.Core.Helpers
{
    public class SerialHelper
    {
        private readonly SerialPort _serialPort = new();

        public SerialHelper() { }

        public SerialHelper(string serialPort, int baudRate = 9600)
        {
            _serialPort = new(serialPort, baudRate);
            _serialPort.ReadTimeout = 100;
            _serialPort.WriteTimeout = 100;
            _serialPort.Parity = Parity.None;
            _serialPort.StopBits = StopBits.One;
            _serialPort.DataBits = 8;
            _serialPort.RtsEnable = true;
            _serialPort.DtrEnable = true;
        }

        public void RequestUpdate()
        {
            Write("GetState");
        }

        public void SetLevel(int level)
        {
            Write($"L{level}");
        }

        public void SetMute(bool mute)
        {
            Write($"M{(mute ? 1 : 0)}");
        }

        public void Write(string text)
        {
            try
            {
                if (!_serialPort.IsOpen)
                {
                    _serialPort.Open();
                }
                if (_serialPort.IsOpen)
                {
                    _serialPort.WriteLine(text);
                    _serialPort.BaseStream.Flush();
                }
            }
            catch (Exception)
            {
                _serialPort.Close();
            }
        }

        public string Read()
        {
            try
            {
                if (!_serialPort.IsOpen)
                {
                    _serialPort.Open();
                }
                if (_serialPort.IsOpen && _serialPort.BytesToRead > 0)
                {
                    return _serialPort.ReadExisting();
                }
            }
            catch (Exception)
            {
                _serialPort.Close();
            }
            return string.Empty;
        }
    }
}
