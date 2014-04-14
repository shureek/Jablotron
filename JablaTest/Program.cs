using System.Collections.Generic;
using System.Collections.Specialized;
using KSoft._1C;
using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;

namespace CJablotron
{
    internal class Program
    {
        static readonly byte[] answerError = new byte[] { 0x15 };
        static readonly byte[] answerOK = new byte[] { 0x06 };
        static Connector connector;
        static volatile bool connectedTo1C = false;
        static List<Socket> serverSockets;
        static volatile bool stop = false;
        static string pultId;
        static volatile int socketsWaiting = 0;
        static readonly System.Text.RegularExpressions.Regex reMsg = new System.Text.RegularExpressions.Regex(@"^5(?<N1>\d{2})(?<N2>\d)\s18(?<ObjNo>[A-F\d]{4,6})(?<Code>[E|R]\d{3})(?<Part>[A-F\d]{2})(?<Zone>[A-F\d]{3})$", System.Text.RegularExpressions.RegexOptions.Compiled);

        /// <summary>
        /// Преобразует строку в число.
        /// </summary>
        /// <remarks>
        /// Преобразует строку в число. Предварительно делает замену: A => 0, B => 1 ... F => 5
        /// </remarks>
        /// <param name="s">Исходная строка</param>
        /// <returns>Результирующее число.</returns>
        static int Str2int(string s)
        {
            s = s.ToUpperInvariant().ReplaceChars("ABCDEF", "012345");
            s = s.TrimStart('0');
            if (s.Length == 0)
                return 0;
            return Int32.Parse(s);
        }

        /// <summary>
        /// Обработчик входящего подключения.
        /// </summary>
        /// <param name="sender">Сокет-сервер, к которому подключились.</param>
        /// <param name="e">Параметры события.</param>
        private static void AcceptHandler(object sender, SocketAsyncEventArgs e)
        {
            System.Threading.Interlocked.Decrement(ref socketsWaiting);
            Exception exception;
            Socket acceptSocket = e.AcceptSocket;
            Socket serverSocket = sender as Socket;
            EndPoint remoteEndPoint = null;
            EndPoint localEndPoint = null;
            dynamic connection = e.UserToken;
            try
            {
                if (!stop && connectedTo1C)
                {
                    ListenForConnection(serverSocket, connection as Connection);
                }
                NetworkStream stream = null;
                System.IO.StreamReader reader = null;
                bool flagContinue = false;
                if (acceptSocket.Connected)
                {
                    acceptSocket.NoDelay = true;
                    localEndPoint = acceptSocket.LocalEndPoint;
                    remoteEndPoint = acceptSocket.RemoteEndPoint;
                    WriteLine("Подключен {0} на {1} (в ожидании {2})", remoteEndPoint, localEndPoint, socketsWaiting);
                    stream = new NetworkStream(acceptSocket);
                    reader = new System.IO.StreamReader(stream, Encoding.ASCII, false, 32);
                    flagContinue = true;
                }
                while (flagContinue)
                {
                    try
                    {
                        string str = reader.ReadLine();
                        if (str == null)
                        {
                            WriteLine("Больше нет данных. Отключение.");
                            return;
                        }

                        if (str[15] == '@')
                        {
                            WriteLine("Тест");
                            acceptSocket.Send(answerOK);
                        }
                        else
                        {
                            //WriteLine("{0} => {1}: {2}", remoteEndPoint, localEndPoint, str);
                            try
                            {
                                var match = reMsg.Match(str);
                                if (!match.Success)
                                    throw new FormatException("Неверный формат сообщения");

                                Message message = new Message
                                {
                                    Source = string.Format("{0} {1},{2}", remoteEndPoint, match.Groups["N1"].Value, match.Groups["N2"].Value),
                                    PultId = pultId
                                };
                                string objNo = match.Groups["ObjNo"].Value;
                                if (objNo == "FFFF")
                                {
                                    // Особенный случай. Ничего не делаем.
                                }
                                else
                                {
                                    message.ObjectId = Str2int(objNo);
                                    message.PartNo = Str2int(match.Groups["Part"].Value);
                                    message.EventId = match.Groups["Code"].Value;
                                    message.ZoneNo = Str2int(match.Groups["Zone"].Value);
                                    object structure = connection.CreateStructure(message);
                                    object result = connection.AddEvent(structure);
                                    if (result is string)
                                    {
                                        var errorInfo = new Dictionary<string, object>();
                                        errorInfo.Add("Ошибка 1С", result);
                                        WriteException(null, errorInfo);
                                    }
                                }
                                acceptSocket.Send(answerOK);
                            } // try
                            catch (System.Runtime.InteropServices.InvalidComObjectException)
                            {
                                flagContinue = false;
                            }
                            catch (Exception exception1)
                            {
                                exception = exception1;

                                bool showException = true;
                                if (exception is System.Runtime.InteropServices.COMException)
                                {
                                    if (exception.Message.Contains("Сеанс работы завершен") || exception.Message.Contains("Сеанс отсутствует"))
                                    {
                                        WriteLine("Произошло отключение от базы 1С");
                                        // 1Ска отключилась. Отключаем все и пробуем заново подключиться к 1С
                                        flagContinue = false;
                                        connectedTo1C = false;
                                        showException = false;
                                    }
                                }

                                if (showException)
                                {
                                    IDictionary<string, object> addInfo = null;
                                    if (exception is FormatException)
                                    {
                                        addInfo = new Dictionary<string, object>();
                                        addInfo["Message"] = str;
                                    }
                                    WriteException(exception, addInfo);
                                }

                                SocketException socketEx = exception.GetException<SocketException>();
                                if (socketEx != null)
                                {
                                    flagContinue = false;
                                    acceptSocket.Send(answerError);
                                }
                            } // catch
                        }

                        if (stop || !connectedTo1C)
                        {
                            acceptSocket.Close();
                            WriteLine("Отключен {0} от {1} (в ожидании {2})", remoteEndPoint, localEndPoint, socketsWaiting);
                            return;
                        }
                    }
                    catch (Exception exception2)
                    {
                        exception = exception2;
                        WriteException(exception);
                        if (exception.GetException<SocketException>() != null)
                            flagContinue = false;
                        if (exception is ObjectDisposedException)
                            return;
                    }
                }
            }
            catch (Exception exception3)
            {
                exception = exception3;
                if (exception.GetException<SocketException>() == null)
                    WriteException(exception);
            }
            finally
            {
                acceptSocket.Close();
            }
        }

        /// <summary>
        /// Запускает асинхронное ожидание входящего подключения.
        /// </summary>
        /// <param name="serverSocket">Серверный сокет.</param>
        /// <param name="connectionTo1C">Подключение к 1Ске.</param>
        private static void ListenForConnection(Socket serverSocket, Connection connectionTo1C)
        {
            SocketAsyncEventArgs e = new SocketAsyncEventArgs();
            e.Completed += AcceptHandler;
            e.UserToken = connectionTo1C;
            System.Threading.Interlocked.Increment(ref socketsWaiting);
            serverSocket.AcceptAsync(e);
        }

        /// <summary>
        /// Объект для блокировки вывода в консоль.
        /// </summary>
        static object consoleLock = new object();

        public static void Main(string[] args)
        {
            IList<int> ports = new int[] { 10001 };
            string connectionString = "Srvr=\"Serv1C\";Ref=\"Pult\";Usr=\"Загрузка сработок\";Pwd=\"111\"";

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // Прочитаем переданные параметры
            string parName = null;
            string parValue = null;
            pultId = "Jablotron";
            System.Collections.Generic.Dictionary<string, object> parameters = new Dictionary<string, object>();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "/?" || args[i] == "-?")
                {
                    // Покажем справку
                    lock (consoleLock)
                    {
                        Console.WriteLine("Получатель событий Jablotron");
                        Console.WriteLine("Возможные параметры:");
                        Console.WriteLine("  -Port <Номер порта> (по-умолчанию {0})", ports);
                        Console.WriteLine("  -Pult <Идентификатор пульта> (по-умолчанию {0})", pultId);
                        Console.WriteLine("  -ConnectionString <Строка соединения с базой 1С> (по-умолчанию Srvr=\"Serv1C\";Ref=\"Pult\";Usr=\"Загрузка сработок\")");
                    }
                    return;
                }
                else if (args[i][0] == '-')
                {
                    parName = args[i].Substring(1).ToUpperInvariant();
                    if (!parameters.ContainsKey(parName))
                        parameters.Add(parName, null);
                }
                else if (!String.IsNullOrWhiteSpace(parName))
                {
                    parValue = args[i];
                    parameters[parName] = parValue;
                }
            }

            if (parameters.ContainsKey("CONNECTIONSTRING"))
                connectionString = parameters["CONNECTIONSTRING"] as String;
            if (parameters.ContainsKey("PORT"))
                ports = ParseInts(parameters["PORT"] as String);
            if (parameters.ContainsKey("PULT"))
                pultId = parameters["PULT"] as String;

            serverSockets = new List<Socket>();
            foreach (int port in ports)
            {
                Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                EndPoint localEP = new IPEndPoint(IPAddress.Any, port);
                server.Bind(localEP);
                server.Listen(0x80);
                serverSockets.Add(server);
            }

            while (!stop)
            {
                Connection connection = null;
                connectedTo1C = false;
                WriteLine("Подключение к базе 1С");
                try
                {
                    connector = new Connector();
                    connection = connector.Connect(connectionString, true);
                    connectedTo1C = true;
                    WriteLine("Подключились к 1С");
                }
                catch (Exception ex)
                {
                    WriteException(ex);
                }

                if (connectedTo1C)
                {
                    foreach (var server in serverSockets)
                    {
                        WriteLine("Ожидаем входящих подключений на {0}", server.LocalEndPoint);
                        ListenForConnection(server, connection);
                    }

                    // Ждем, пока не отключится 1Ска или не нажмут на клавишу
                    while (!stop && connectedTo1C)
                    {
                        System.Threading.Thread.Sleep(5000);
                        while (Console.KeyAvailable)
                        {
                            WriteLine("Остановка");
                            stop = true;
                            Console.ReadKey(true);
                        }
                    }

                    // Подождем, пока отключатся все сокеты
                    while (socketsWaiting > 0)
                        System.Threading.Thread.Sleep(1000);
                    WriteLine("Все подключения отключены");

                    connection.Close();
                }
                else
                {
                    if (Console.KeyAvailable)
                    {
                        WriteLine("Остановка");
                        stop = true;
                        while (Console.KeyAvailable)
                            Console.ReadKey(true);
                    }
                    else
                        System.Threading.Thread.Sleep(15000);
                }
            }

            foreach (var server in serverSockets)
            {
                server.Close();
            }
            serverSockets.Clear();

            WriteLine("Нажмите любую клавишу для выхода");
            Console.ReadKey(true);
        }

        /// <summary>
        /// Преобразует строку вида 1,2,5-8 в список чисел {1,2,3,5,6,7,8}
        /// </summary>
        /// <param name="str">Строка.</param>
        /// <returns>Массив чисел.</returns>
        static IList<int> ParseInts(string str)
        {
            List<int> numbers = new List<int>();
            foreach (var range in str.Split(','))
            {
                int sepIndex = range.IndexOf('-');
                if (sepIndex >= 0)
                {
                    int lower = Int32.Parse(range.Substring(0, sepIndex).Trim());
                    int upper = Int32.Parse(range.Substring(sepIndex + 1).Trim());
                    for (int nm = lower; nm <= upper; nm++)
                        numbers.Add(nm);
                }
                else
                {
                    int nm = Int32.Parse(range.Trim());
                    numbers.Add(nm);
                }
            }
            return numbers;
        }

        /// <summary>
        /// Обработчик необработанных исключений домена.
        /// </summary>
        /// <param name="sender">Источник исключения.</param>
        /// <param name="e">Параметры обработчика.</param>
        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            WriteException(e.ExceptionObject as Exception);
        }

        /// <summary>
        /// Вывод информации об исключении в консоль.
        /// </summary>
        /// <param name="ex">Исключение.</param>
        /// <param name="additionaInfo">Дополнительная информация.</param>
        static void WriteException(Exception ex, IDictionary<string, object> additionaInfo = null)
        {
            DateTime time = DateTime.Now;
            lock(consoleLock)
            {
                Console.ForegroundColor = ConsoleColor.Red;

                Console.Write("{0:HH:mm:ss,fff} ", time);
                Exception currentEx = ex;
                while (currentEx != null)
                {
                    Console.WriteLine("{0}: {1}", currentEx.GetType().FullName, currentEx.Message);
                    foreach (var property in currentEx.GetType().GetProperties())
                    {
                        if (property.Name == "Message")
                            continue;
                        object value = property.GetValue(currentEx, null);
                        Console.WriteLine("{0}: {1}", property.Name, value);
                    }

                    currentEx = currentEx.InnerException;
                    if (currentEx != null)
                        Console.Write("Because of ");
                }
                if (additionaInfo != null)
                    foreach (var keyValue in additionaInfo)
                        Console.WriteLine("{0}: {1}", keyValue.Key, keyValue.Value);

                Console.ResetColor();
            }
        }

        /// <summary>
        /// Вывод строки в консоль с предварительным выводом текущего времени.
        /// </summary>
        /// <param name="message">Форматированная строка.</param>
        /// <param name="args">Параметры для вывода.</param>
        private static void WriteLine(string message, params object[] args)
        {
            DateTime time = DateTime.Now;
            lock (consoleLock)
            {
                Console.Write("{0:HH:mm:ss,fff} ", time);
                try
                {
                    Console.WriteLine(message, args);
                }
                catch (FormatException)
                {
                    Console.WriteLine(message);
                }
            }
        }
    }
}

