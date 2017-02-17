using System;
using System.IO;
using System.IO.Ports;
using System.Reflection;
using System.Text;
using Unclassified.Util;
using Utilities;

namespace SerialLog
{
    public static class Commons
    {
        private static SerialPort defaultSerialPort;

        public static string AppVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(2);
        public static void PrintLogo()
        {
            ConsoleHelper.WriteWrapped(@" __           _       _   __             ");
            ConsoleHelper.WriteWrapped(@"/ _\ ___ _ __(_) __ _| | / /  ___   __ _ ");
            ConsoleHelper.WriteWrapped(@"\ \ / _ \ '__| |/ _` | |/ /  / _ \ / _` |");
            ConsoleHelper.WriteWrapped(@"_\ \  __/ |  | | (_| | / /__| (_) | (_| |");
            ConsoleHelper.WriteWrapped(@"\__/\___|_|  |_|\__,_|_\____/\___/ \__, |");
            ConsoleHelper.WriteWrapped(string.Format(@" v{0}    by Heiswayi Nrird, 2017   |___/ ", AppVersion));
        }
        public static Func<string> GetTitle = () => string.Format("SerialLog v{0}", AppVersion);
        public static void EmptyOneRow()
        {
            Console.Write(Environment.NewLine);
        }
        public static void ShowError(string s)
        {
            ConsoleHelper.WriteLine("ERROR: " + s, ConsoleColor.White, ConsoleColor.DarkRed);
        }

        public static bool IsConfigFileExist()
        {
            if (File.Exists("config.ini"))
                return true;
            else
                return false;
        }

        public static void CreateConfigFile()
        {
            if (File.Exists("config.ini"))
                File.Delete("config.ini");

            IniFileHelper ini = new IniFileHelper("config.ini");

            if (defaultSerialPort == null)
                defaultSerialPort = new SerialPort();

            ini.Write("portName", defaultSerialPort.PortName, "SerialPort");
            ini.Write("baudRate", defaultSerialPort.BaudRate.ToString(), "SerialPort");
            ini.Write("parity", defaultSerialPort.Parity.ToString(), "SerialPort");
            ini.Write("dataBits", defaultSerialPort.DataBits.ToString(), "SerialPort");
            ini.Write("stopBits", defaultSerialPort.StopBits.ToString(), "SerialPort");
            ini.Write("handshake", defaultSerialPort.Handshake.ToString(), "SerialPort");
            ini.Write("readTimeout", defaultSerialPort.ReadTimeout.ToString(), "SerialPort");
            ini.Write("loggingInterval", "1000", "Logging");
        }

        public static T ParseEnum<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }

        public static void WriteInfo(object label, object value, string tag = "", int width = 20)
        {
            Console.Write("  " + label.ToString().PadRight(width, '.'));
            Console.Write(" : ");
            Console.WriteLine(value.ToString().PadRight(20) + " " + tag);
        }

        public static void ShowCurrentSettings(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits, Handshake handshake, int readTimeout, int loggingInterval)
        {
            ConsoleHelper.WriteLine("Current Settings", ConsoleColor.Cyan);
            WriteInfo("PortName", portName, portName.ToUpper() == "COM1" ? "(default)" : "");
            WriteInfo("BaudRate", baudRate, baudRate == 9600 ? "(default)" : "");
            WriteInfo("DataBits", dataBits, dataBits == 8 ? "(default)" : "");
            WriteInfo("Parity", parity, parity.ToString() == "None" ? "(default)" : "");
            WriteInfo("StopBits", stopBits, stopBits.ToString() == "One" ? "(default)" : "");
            WriteInfo("Handshake", handshake, handshake.ToString() == "None" ? "(default)" : "");
            WriteInfo("Timeout", readTimeout, readTimeout == -1 ? "(default)" : "");
            WriteInfo("LogInterval", loggingInterval, loggingInterval == 1000 ? "(default)" : "");
            EmptyOneRow();
            ConsoleHelper.Write("  To update the value, type ", ConsoleColor.DarkYellow);
            ConsoleHelper.WriteLine("/set <label> <value>", ConsoleColor.Yellow);
            ConsoleHelper.Write("  To get help, type ", ConsoleColor.DarkRed);
            ConsoleHelper.WriteLine("/help <label>", ConsoleColor.Red);
            ConsoleHelper.Write("  To start logging, type ", ConsoleColor.DarkGreen);
            ConsoleHelper.WriteLine("/start", ConsoleColor.Green);
            ConsoleHelper.WriteLine("  Data logging is automatic once connection is established.", ConsoleColor.DarkGray);
            EmptyOneRow();
        }
    }
}