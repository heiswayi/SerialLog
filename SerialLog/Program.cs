using NLog;
using System;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using Unclassified.Util;

namespace SerialLog
{
    internal class Program
    {
        private static bool _continue;
        private static SerialPort _serialPort;
        private static Thread _readThread;
        private static ILogger _logger;

        private static int[] baudrateArray = { 0, 100, 300, 600, 1200, 2400, 4800, 9600, 14400, 19200, 38400, 56000, 57600, 115200, 128000, 256000 };
        private static int[] databitsArray = { 5, 6, 7, 8 };

        private static void Main(string[] args)
        {
            Console.Title = "SerialLog v1.1";
            ConsoleHelper.FixEncoding();

            ConsoleHelper.WriteWrapped(@" __           _       _   __             ");
            ConsoleHelper.WriteWrapped(@"/ _\ ___ _ __(_) __ _| | / /  ___   __ _ ");
            ConsoleHelper.WriteWrapped(@"\ \ / _ \ '__| |/ _` | |/ /  / _ \ / _` |");
            ConsoleHelper.WriteWrapped(@"_\ \  __/ |  | | (_| | / /__| (_) | (_| |");
            ConsoleHelper.WriteWrapped(@"\__/\___|_|  |_|\__,_|_\____/\___/ \__, |");
            ConsoleHelper.WriteWrapped(@" v1.1    by Heiswayi Nrird, 2016   |___/ ");

            RemoveLogFile();

            _logger = LogManager.GetLogger("SerialLog");
            _logger.Info("SerialLog initiated.");

            _serialPort = new SerialPort();

            _logger.Trace("---> Configure serial port settings");
            _serialPort.PortName = SetPortName(_serialPort.PortName);
            _serialPort.BaudRate = SetPortBaudRate(_serialPort.BaudRate);
            _serialPort.Parity = SetPortParity(_serialPort.Parity);
            _serialPort.DataBits = SetPortDataBits(_serialPort.DataBits);
            _serialPort.StopBits = SetPortStopBits(_serialPort.StopBits);
            _serialPort.Handshake = SetPortHandshake(_serialPort.Handshake);
            _serialPort.ReadTimeout = SetReadTimeout(500);

            _logger.Info(string.Format("PortName={0}, BaudRate={1}, Parity={2}, DataBits={3}, StopBits={4}, Handshake={5}, ReadTimeout={6}",
                _serialPort.PortName, _serialPort.BaudRate, _serialPort.Parity, _serialPort.DataBits, _serialPort.StopBits, _serialPort.Handshake, _serialPort.ReadTimeout));

            _logger.Trace("<--- Configure serial port settings");

            Operation:
            Console.Write(Environment.NewLine);
            ConsoleHelper.Write("Start SerialLog now? [y|n]: ", ConsoleColor.Yellow);
            string response = Console.ReadLine();
            if (response.ToUpper() == "Y")
            {
                Start();
            }
            else if (response.ToUpper() == "N")
            {
                Environment.Exit(0);
            }
            else
            {
                goto Operation;
            }

            //ConsoleHelper.Wait();
        }

        private static void RemoveLogFile()
        {
            string logfile = string.Format(@"{0}\logs\{1}.log", AppDomain.CurrentDomain.BaseDirectory, DateTime.Now.ToString("yyyy-MM-dd"));
            if (File.Exists(logfile))
                File.Delete(logfile);
        }

        public static void Start()
        {
            _logger.Trace("---> Start()");
            try
            {
                _serialPort.Open();
                _readThread = new Thread(Read);
                _readThread.Start();
                _continue = true;
                ConsoleHelper.WriteLine("SerialLog started. Reading incoming data...", ConsoleColor.Black, ConsoleColor.Green);
                _logger.Debug("SerialLog successfully connected and started.");
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteLine("ERROR: " + ex.ToString(), ConsoleColor.White, ConsoleColor.DarkRed);
                _logger.Error(ex.ToString());
            }
            _logger.Trace("<--- Start()");
        }

        public static void Stop()
        {
            _logger.Trace("---> Stop()");
            if (_serialPort.IsOpen)
            {
                _continue = false;
                _readThread.Join();
                _serialPort.Close();
                _logger.Debug("SerialLog successfully disconnected and stopped.");
            }
            _logger.Trace("<--- Stop()");
        }

        public static void Read()
        {
            _logger.Trace("---> Read()");
            while (_continue)
            {
                byte[] readBuffer = new byte[_serialPort.ReadBufferSize + 1];
                try
                {
                    int count = _serialPort.Read(readBuffer, 0, _serialPort.ReadBufferSize);
                    string data = Encoding.ASCII.GetString(readBuffer, 0, count);
                    //string message = _serialPort.ReadLine();

                    ConsoleHelper.Write(string.Format("[{0}] ", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff",
                                            CultureInfo.InvariantCulture)), ConsoleColor.Green);
                    Console.WriteLine(data);

                    _logger.Info(data);
                }
                catch (TimeoutException) { }
            }
            _logger.Trace("<--- Read()");
        }

        public static string SetPortName(string defaultPortName)
        {
            string portName;

            // Display option list
            Console.Write(Environment.NewLine);
            Console.WriteLine("Available COM ports:-");
            foreach (string _portname in SerialPort.GetPortNames())
            {
                ConsoleHelper.WriteWrapped("  " + _portname);
            }

            // Selecting option
            Console.Write(Environment.NewLine);
            ConsoleHelper.Write(string.Format("Select COM port (default: {0}): ", defaultPortName), ConsoleColor.Green);
            portName = Console.ReadLine();

            // Validating
            if (portName == "")
            {
                portName = defaultPortName;
            }
            else if (!SerialPort.GetPortNames().Contains(portName))
            {
                ConsoleHelper.WriteLine("ERROR: " + "Invalid port name! Default value is used.", ConsoleColor.White, ConsoleColor.DarkRed);
                portName = defaultPortName;
            }

            // Display result
            Console.Write("  --> ");
            ConsoleHelper.WriteLine(portName, ConsoleColor.Yellow);

            return portName;
        }

        public static int SetPortBaudRate(int defaultPortBaudRate)
        {
            string baudRate;

            // Display option list
            Console.Write(Environment.NewLine);
            Console.WriteLine("Available baud rate options:-");
            string formatted = "";
            foreach (int _baudrate in baudrateArray)
            {
                formatted += "  " + _baudrate;
            }
            ConsoleHelper.WriteWrapped(formatted);

            // Selecting option
            Console.Write(Environment.NewLine);
            ConsoleHelper.Write(string.Format("Select baud rate (default: {0}): ", defaultPortBaudRate), ConsoleColor.Green);
            baudRate = Console.ReadLine();

            // Validating
            int temp;
            if (baudRate == "")
            {
                baudRate = defaultPortBaudRate.ToString();
            }
            else if (!int.TryParse(baudRate, out temp))
            {
                ConsoleHelper.WriteLine("ERROR: " + "Invalid baud rate! Default value is used.", ConsoleColor.White, ConsoleColor.DarkRed);
                baudRate = defaultPortBaudRate.ToString();
            }
            else if (!baudrateArray.Contains(int.Parse(baudRate)))
            {
                ConsoleHelper.WriteLine("ERROR: " + "Option not available! Default value is used.", ConsoleColor.White, ConsoleColor.DarkRed);
                baudRate = defaultPortBaudRate.ToString();
            }

            // Display result
            Console.Write("  --> ");
            ConsoleHelper.WriteLine(baudRate, ConsoleColor.Yellow);

            return int.Parse(baudRate);
        }

        public static Parity SetPortParity(Parity defaultPortParity)
        {
            string parity;

            // Display option list
            Console.Write(Environment.NewLine);
            Console.WriteLine("Available parity options:-");
            string formatted = "";
            foreach (string _parity in Enum.GetNames(typeof(Parity)))
            {
                formatted += "  " + _parity;
            }
            ConsoleHelper.WriteWrapped(formatted);

            // Selecting option
            Console.Write(Environment.NewLine);
            ConsoleHelper.Write(string.Format("Select parity (default: {0}): ", defaultPortParity), ConsoleColor.Green);
            parity = Console.ReadLine();

            // Validating
            if (parity == "")
            {
                parity = defaultPortParity.ToString();
            }
            else if (!Enum.GetNames(typeof(Parity)).Contains(parity))
            {
                ConsoleHelper.WriteLine("ERROR: " + "Option not available! Default value is used.", ConsoleColor.White, ConsoleColor.DarkRed);
                parity = defaultPortParity.ToString();
            }

            // Display result
            Console.Write("  --> ");
            ConsoleHelper.WriteLine(parity, ConsoleColor.Yellow);

            return (Parity)Enum.Parse(typeof(Parity), parity);
        }

        public static int SetPortDataBits(int defaultPortDataBits)
        {
            string dataBits;

            // Display option list
            Console.Write(Environment.NewLine);
            Console.WriteLine("Available data bits options:-");
            string formatted = "";
            foreach (int _databits in databitsArray)
            {
                formatted += "  " + _databits;
            }
            ConsoleHelper.WriteWrapped(formatted);

            // Selecting option
            Console.Write(Environment.NewLine);
            ConsoleHelper.Write(string.Format("Select data bits (default: {0}): ", defaultPortDataBits), ConsoleColor.Green);
            dataBits = Console.ReadLine();

            // Validating
            int temp;
            if (dataBits == "")
            {
                dataBits = defaultPortDataBits.ToString();
            }
            else if (!int.TryParse(dataBits, out temp))
            {
                ConsoleHelper.WriteLine("ERROR: " + "Invalid data bits! Default value is used.", ConsoleColor.White, ConsoleColor.DarkRed);
                dataBits = defaultPortDataBits.ToString();
            }
            else if (!databitsArray.Contains(int.Parse(dataBits)))
            {
                ConsoleHelper.WriteLine("ERROR: " + "Option not available! Default value is used.", ConsoleColor.White, ConsoleColor.DarkRed);
                dataBits = defaultPortDataBits.ToString();
            }

            // Display result
            Console.Write("  --> ");
            ConsoleHelper.WriteLine(dataBits, ConsoleColor.Yellow);

            return int.Parse(dataBits);
        }

        public static StopBits SetPortStopBits(StopBits defaultPortStopBits)
        {
            string stopBits;

            // Display option list
            Console.Write(Environment.NewLine);
            Console.WriteLine("Available stop bits options:-");
            string formatted = "";
            foreach (string _stopbits in Enum.GetNames(typeof(StopBits)))
            {
                formatted += "  " + _stopbits;
            }
            ConsoleHelper.WriteWrapped(formatted);

            // Selecting option
            Console.Write(Environment.NewLine);
            ConsoleHelper.Write(string.Format("Select stop bits (default: {0}): ", defaultPortStopBits), ConsoleColor.Green);
            stopBits = Console.ReadLine();

            // Validating
            if (stopBits == "")
            {
                stopBits = defaultPortStopBits.ToString();
            }
            else if (!Enum.GetNames(typeof(StopBits)).Contains(stopBits))
            {
                ConsoleHelper.WriteLine("ERROR: " + "Option not available! Default value is used.", ConsoleColor.White, ConsoleColor.DarkRed);
                stopBits = defaultPortStopBits.ToString();
            }

            // Display result
            Console.Write("  --> ");
            ConsoleHelper.WriteLine(stopBits, ConsoleColor.Yellow);

            return (StopBits)Enum.Parse(typeof(StopBits), stopBits);
        }

        public static Handshake SetPortHandshake(Handshake defaultPortHandshake)
        {
            string handshake;

            // Display option list
            Console.Write(Environment.NewLine);
            Console.WriteLine("Available handshake options:-");
            string formatted = "";
            foreach (string _handshake in Enum.GetNames(typeof(Handshake)))
            {
                formatted += "  " + _handshake;
            }
            ConsoleHelper.WriteWrapped(formatted);

            // Selecting
            Console.Write(Environment.NewLine);
            ConsoleHelper.Write(string.Format("Select handshake (default: {0}): ", defaultPortHandshake), ConsoleColor.Green);
            handshake = Console.ReadLine();

            // Validating
            if (handshake == "")
            {
                handshake = defaultPortHandshake.ToString();
            }
            else if (!Enum.GetNames(typeof(Handshake)).Contains(handshake))
            {
                ConsoleHelper.WriteLine("ERROR: " + "Option not available! Default value is used.", ConsoleColor.White, ConsoleColor.DarkRed);
                handshake = defaultPortHandshake.ToString();
            }

            // Display result
            Console.Write("  --> ");
            ConsoleHelper.WriteLine(handshake, ConsoleColor.Yellow);

            return (Handshake)Enum.Parse(typeof(Handshake), handshake);
        }

        public static int SetReadTimeout(int defaultTimeout)
        {
            string timeout;

            // Set option
            Console.Write(Environment.NewLine);
            ConsoleHelper.Write(string.Format("Set read timeout (default: {0}): ", defaultTimeout), ConsoleColor.Green);
            timeout = Console.ReadLine();

            // Validating
            int temp;
            if (timeout == "")
            {
                timeout = defaultTimeout.ToString();
            }
            else if (!int.TryParse(timeout, out temp))
            {
                ConsoleHelper.WriteLine("ERROR: " + "Invalid integer format! Default value is used.", ConsoleColor.White, ConsoleColor.DarkRed);
                timeout = defaultTimeout.ToString();
            }

            // Display result
            Console.Write("  --> ");
            ConsoleHelper.WriteLine(timeout, ConsoleColor.Yellow);

            return int.Parse(timeout);
        }
    }
}