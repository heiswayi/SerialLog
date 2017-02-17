using System;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Management;
using System.Text;
using System.Threading;
using System.Timers;
using Unclassified.Util;
using Utilities;

namespace SerialLog
{
    internal class Program
    {
        private static bool _continue;
        private static SerialPort _serialPort;
        private static Thread _readThread;
        private static System.Timers.Timer _timer;
        private static string _dataBuffer;
        private static string _filename;

        private static bool listening;
        private static IniFileHelper ini;

        private static int[] baudrateArray = { 0, 100, 300, 600, 1200, 2400, 4800, 9600, 14400, 19200, 38400, 56000, 57600, 115200, 128000, 256000 };
        private static int[] databitsArray = { 5, 6, 7, 8 };

        private static string portName;
        private static int baudRate;
        private static Parity parity;
        private static int dataBits;
        private static StopBits stopBits;
        private static Handshake handshake;
        private static int readTimeout;
        private static int loggingInterval;

        private static void Main(string[] args)
        {
            _filename = DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
            Console.BackgroundColor = ConsoleColor.Black;
            ConsoleHelper.FixEncoding();
            Console.Title = Commons.GetTitle();
            Commons.PrintLogo();
            Commons.EmptyOneRow();

            if (!Commons.IsConfigFileExist())
            {
                try
                {
                    Commons.CreateConfigFile();
                }
                catch (Exception ex)
                {
                    Commons.ShowError(ex.ToString());
                    ConsoleHelper.Wait();
                }
            }

            try
            {
                ini = new IniFileHelper("config.ini");
                portName = ini.Read("portName", "SerialPort");
                baudRate = int.Parse(ini.Read("baudRate", "SerialPort"));
                parity = Commons.ParseEnum<Parity>(ini.Read("parity", "SerialPort"));
                dataBits = int.Parse(ini.Read("dataBits", "SerialPort"));
                stopBits = Commons.ParseEnum<StopBits>(ini.Read("stopBits", "SerialPort"));
                handshake = Commons.ParseEnum<Handshake>(ini.Read("handshake", "SerialPort"));
                readTimeout = int.Parse(ini.Read("readTimeout", "SerialPort"));
                loggingInterval = int.Parse(ini.Read("loggingInterval", "Logging"));

                Commons.ShowCurrentSettings(portName, baudRate, parity, dataBits, stopBits, handshake, readTimeout, loggingInterval);

                listening = true;
                if (_serialPort == null)
                    _serialPort = new SerialPort();
            }
            catch (Exception ex)
            {
                Commons.ShowError(ex.ToString());
                ConsoleHelper.Wait();
            }

            while (true)
            {
                if (listening == true)
                    InterpretInput(Console.ReadLine());
                else
                    break;
            }

            string folder = AppDomain.CurrentDomain.BaseDirectory + "\\datalogs";
            Directory.CreateDirectory(folder);

            _timer = new System.Timers.Timer();
            _timer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimed);
            _timer.Interval = loggingInterval;
            _timer.Enabled = true;

            if (listening == false)
            {
                string file = AppDomain.CurrentDomain.BaseDirectory + "\\datalogs\\" + _filename;
                try
                {
                    File.WriteAllText(file, "# This file is auto-generated on " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff tt") + "." + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    Commons.ShowError(ex.ToString());
                    ConsoleHelper.Wait();
                }

                Start();

                ConsoleHelper.WriteLine("Logging is now running...", ConsoleColor.Green);
            }
        }

        private static void OnTimed(object sender, ElapsedEventArgs e)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string file = AppDomain.CurrentDomain.BaseDirectory + "\\datalogs\\" + _filename;

            try
            {
                using (StreamWriter writer = new StreamWriter(file, true))
                {
                    writer.WriteLine(string.Format("[{0}] {1}", timestamp, _dataBuffer.TrimEnd(Environment.NewLine.ToCharArray())));
                }
            }
            catch (Exception ex)
            {
                Commons.ShowError(ex.ToString());
                ConsoleHelper.Wait();
            }

            ConsoleHelper.Write("Logged at ", ConsoleColor.DarkGray);
            ConsoleHelper.Write(string.Format("{0}", timestamp), ConsoleColor.Green);
            ConsoleHelper.Write(" : ", ConsoleColor.Red);
            Console.WriteLine(_dataBuffer);
        }

        private static void InterpretInput(string input)
        {
            string temp = input.Trim();
            if (temp.Contains("/start"))
            {
                _serialPort.PortName = portName;
                _serialPort.BaudRate = baudRate;
                _serialPort.DataBits = dataBits;
                _serialPort.Parity = parity;
                _serialPort.StopBits = stopBits;
                _serialPort.Handshake = handshake;
                _serialPort.ReadTimeout = readTimeout;
                //Start();
                listening = false;
            }
            else if (temp.Contains("/help"))
            {
                string[] vars = temp.Split(' ');
                ShowHelp(vars[1]);
            }
            else if (temp.Contains("/set"))
            {
                string[] vars = temp.Split(' ');
                UpdateSettings(vars[1], vars[2]);
            }
        }

        private static void UpdateSettings(string label, string value)
        {
            label = label.Trim();

            if (_serialPort == null)
                _serialPort = new SerialPort();

            if (ini == null)
                ini = new IniFileHelper("config.ini");

            try
            {
                if (label.ToLower() == "baudrate")
                {
                    baudRate = int.Parse(value);
                    _serialPort.BaudRate = baudRate;
                    ini.Write("baudRate", value, "SerialPort");
                }
                else if (label.ToLower() == "databits")
                {
                    dataBits = int.Parse(value);
                    _serialPort.DataBits = dataBits;
                    ini.Write("dataBits", value, "SerialPort");
                }
                else if (label.ToLower() == "parity")
                {
                    parity = Commons.ParseEnum<Parity>(value);
                    _serialPort.Parity = parity;
                    ini.Write("parity", parity.ToString(), "SerialPort");
                }
                else if (label.ToLower() == "stopbits")
                {
                    stopBits = Commons.ParseEnum<StopBits>(value);
                    _serialPort.StopBits = stopBits;
                    ini.Write("stopBits", stopBits.ToString(), "SerialPort");
                }
                else if (label.ToLower() == "handshake")
                {
                    handshake = Commons.ParseEnum<Handshake>(value);
                    _serialPort.Handshake = handshake;
                    ini.Write("handshake", handshake.ToString(), "SerialPort");
                }
                else if (label.ToLower() == "timeout")
                {
                    readTimeout = int.Parse(value);
                    _serialPort.ReadTimeout = readTimeout;
                    ini.Write("readTimeout", value, "SerialPort");
                }
                else if (label.ToLower() == "portname")
                {
                    portName = value.ToUpper();
                    _serialPort.PortName = portName;
                    ini.Write("portName", portName, "SerialPort");
                }
                else if (label.ToLower() == "loginterval")
                {
                    loggingInterval = int.Parse(value);
                    ini.Write("loggingInterval", value, "Logging");
                }
            }
            catch (Exception ex)
            {
                Commons.ShowError(ex.ToString());
                ConsoleHelper.Wait();
            }

            Console.Clear();
            Commons.PrintLogo();
            Commons.EmptyOneRow();
            Commons.ShowCurrentSettings(portName, baudRate, parity, dataBits, stopBits, handshake, readTimeout, loggingInterval);
        }

        private static void ShowHelp(string s)
        {
            Console.Clear();
            Commons.PrintLogo();
            Commons.EmptyOneRow();
            Commons.ShowCurrentSettings(portName, baudRate, parity, dataBits, stopBits, handshake, readTimeout, loggingInterval);

            if (s.ToLower() == "baudrate")
            {
                ConsoleHelper.WriteLine("Help -> BaudRate", ConsoleColor.Cyan);
                StringBuilder sb = new StringBuilder();
                foreach (var item in baudrateArray)
                    sb.Append(item.ToString() + " ");
                ConsoleHelper.WriteWrapped("  Available: " + sb.ToString());
                ConsoleHelper.Write("  To update the value, type e.g. ", ConsoleColor.DarkYellow);
                ConsoleHelper.WriteLine("/set baudrate 9600", ConsoleColor.Yellow);
            }
            else if (s.ToLower() == "databits")
            {
                ConsoleHelper.WriteLine("Help -> DataBits", ConsoleColor.Cyan);
                StringBuilder sb = new StringBuilder();
                foreach (var item in databitsArray)
                    sb.Append(item.ToString() + " ");
                ConsoleHelper.WriteWrapped("  Available: " + sb.ToString());
                ConsoleHelper.Write("  To update the value, type e.g. ", ConsoleColor.DarkYellow);
                ConsoleHelper.WriteLine("/set databits 8", ConsoleColor.Yellow);
            }
            else if (s.ToLower() == "parity")
            {
                ConsoleHelper.WriteLine("Help -> Parity", ConsoleColor.Cyan);
                StringBuilder sb = new StringBuilder();
                foreach (var item in Enum.GetValues(typeof(Parity)))
                    sb.Append(item.ToString() + " ");
                ConsoleHelper.WriteWrapped("  Available: " + sb.ToString());
                ConsoleHelper.Write("  To update the value, type e.g. ", ConsoleColor.DarkYellow);
                ConsoleHelper.Write("/set parity none", ConsoleColor.Yellow);
                ConsoleHelper.WriteLine(" (case-insensitive)", ConsoleColor.DarkYellow);
            }
            else if (s.ToLower() == "stopbits")
            {
                ConsoleHelper.WriteLine("Help -> StopBits", ConsoleColor.Cyan);
                StringBuilder sb = new StringBuilder();
                foreach (var item in Enum.GetValues(typeof(StopBits)))
                    sb.Append(item.ToString() + " ");
                ConsoleHelper.WriteWrapped("  Available: " + sb.ToString());
                ConsoleHelper.Write("  To update the value, type e.g. ", ConsoleColor.DarkYellow);
                ConsoleHelper.Write("/set stopbits one", ConsoleColor.Yellow);
                ConsoleHelper.WriteLine(" (case-insensitive)", ConsoleColor.DarkYellow);
            }
            else if (s.ToLower() == "handshake")
            {
                ConsoleHelper.WriteLine("Help -> Handshake / Flow Control", ConsoleColor.Cyan);
                StringBuilder sb = new StringBuilder();
                foreach (var item in Enum.GetValues(typeof(Handshake)))
                    sb.Append(item.ToString() + " ");
                ConsoleHelper.WriteWrapped("  Available: " + sb.ToString());
                ConsoleHelper.Write("  To update the value, type e.g. ", ConsoleColor.DarkYellow);
                ConsoleHelper.Write("/set handshake xonxoff", ConsoleColor.Yellow);
                ConsoleHelper.WriteLine(" (case-insensitive)", ConsoleColor.DarkYellow);
            }
            else if (s.ToLower() == "timeout")
            {
                ConsoleHelper.WriteLine("Help -> Read Timeout", ConsoleColor.Cyan);
                ConsoleHelper.WriteWrapped("  -1 means not set or infinity.");
                ConsoleHelper.Write("  To update the value, type e.g. ", ConsoleColor.DarkYellow);
                ConsoleHelper.Write("/set timeout 500", ConsoleColor.Yellow);
                ConsoleHelper.WriteLine(" (in milliseconds)", ConsoleColor.DarkYellow);
            }
            else if (s.ToLower() == "portname")
            {
                StringBuilder sb = new StringBuilder();
                ManagementObjectCollection moc;
                using (var searcher = new ManagementObjectSearcher(@"Select * From Win32_SerialPort"))
                    moc = searcher.Get();

                foreach (var device in moc)
                {
                    sb.Append(string.Format("{0} ({1})\n    ", device.GetPropertyValue("DeviceID"), device.GetPropertyValue("Description")));
                }

                moc.Dispose();

                ConsoleHelper.WriteLine("Help -> PortName", ConsoleColor.Cyan);
                ConsoleHelper.WriteWrapped("  Available:");
                ConsoleHelper.WriteWrapped("    " + sb.ToString());
                ConsoleHelper.Write("  To update the value, type e.g. ", ConsoleColor.DarkYellow);
                ConsoleHelper.Write("/set portname com1", ConsoleColor.Yellow);
                ConsoleHelper.WriteLine(" (case-insensitive)", ConsoleColor.DarkYellow);
            }
            else if (s.ToLower() == "loginterval")
            {
                ConsoleHelper.WriteLine("Help -> Log Interval", ConsoleColor.Cyan);
                ConsoleHelper.WriteWrapped("  Data will be logged into a file based on interval set.");
                ConsoleHelper.WriteWrapped("  Minimum is 200 milliseconds. Default is 1000.");
                ConsoleHelper.Write("  To update the value, type e.g. ", ConsoleColor.DarkYellow);
                ConsoleHelper.Write("/set loginterval 5000", ConsoleColor.Yellow);
                ConsoleHelper.WriteLine(" (in milliseconds)", ConsoleColor.DarkYellow);
            }
            Commons.EmptyOneRow();
        }

        public static void Start()
        {
            try
            {
                _serialPort.Open();
                _readThread = new Thread(Read);
                _readThread.Start();
                _continue = true;
            }
            catch (Exception ex)
            {
                Commons.ShowError(ex.ToString());
                ConsoleHelper.Wait();
            }
        }

        public static void Stop()
        {
            if (_serialPort.IsOpen)
            {
                _continue = false;
                _readThread.Join();
                _serialPort.Close();
            }
        }

        public static void Read()
        {
            while (_continue)
            {
                //byte[] readBuffer = new byte[_serialPort.ReadBufferSize + 1];
                try
                {
                    //int count = _serialPort.Read(readBuffer, 0, _serialPort.ReadBufferSize);
                    //string data = Encoding.ASCII.GetString(readBuffer, 0, count);
                    string data = _serialPort.ReadLine();

                    //ConsoleHelper.Write(string.Format("[{0}] ", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff",
                    //                        CultureInfo.InvariantCulture)), ConsoleColor.Green);
                    //Console.WriteLine(data);

                    _dataBuffer = data;
                }
                catch (TimeoutException) { }
            }
        }
    }
}