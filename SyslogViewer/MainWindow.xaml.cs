
using System.IO;
using SyslogViewer.Models;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Text.RegularExpressions;

namespace SyslogViewer;

public enum SyslogSeverity
{
    Emergency = 0,
    Alert = 1,
    Critical = 2,
    Error = 3,
    Warning = 4,
    Notice = 5,
    Info = 6,
    Debug = 7
}

public enum SyslogFacility
{
    Kernel = 0,
    User = 1,
    Mail = 2,
    Daemon = 3,
    Auth = 4,
    Syslog = 5,
    Lpr = 6,
    News = 7,
    UUCP = 8,
    Cron = 9,
    AuthPriv = 10,
    FTP = 11,
    NTP = 12,
    LogAudit = 13,
    LogAlert = 14,
    ClockDaemon = 15,
    Local0 = 16,
    Local1 = 17,
    Local2 = 18,
    Local3 = 19,
    Local4 = 20,
    Local5 = 21,
    Local6 = 22,
    Local7 = 23
}

public partial class MainWindow : Window
{
    ObservableCollection<SyslogEntry> Logs = new();
    ConcurrentDictionary<string, TcpClient> Clients = new();

    public MainWindow()
    {
        InitializeComponent();

        LogGrid.ItemsSource = Logs;

        Task.Run(StartSyslogServer);
    }

    async Task StartSyslogServer()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, 514);
        listener.Start();

        while (true)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();

            string id = client.Client.RemoteEndPoint.ToString();

            Clients[id] = client;

            Dispatcher.Invoke(() =>
            {
                ClientList.Items.Add(id);
            });

            _ = HandleClient(id, client);
        }
    }

    async Task HandleClient(string id, TcpClient client)
    {
        try
        {
            var stream = client.GetStream();
            var buffer = new byte[4096];

            while (true)
            {
                int read = await stream.ReadAsync(buffer, 0, buffer.Length);
                if(read == 0)
                    break;

                string chunk = Encoding.UTF8.GetString(buffer, 0, read);
                string message = chunk.TrimEnd('\n', '\r');

                string pattern = @"^<(\d+)>(\d)\s(\S+)\s(\S+)\s(\S+)\s(\S+)\s(\S+)\s(\S+)\s?(.*)$";
                Match match = Regex.Match(message, pattern);

                string? NilToNull(string value) => value == "-" ? null : value;

                int? ParseNullableInt(string value)
                    => value == "-" ? null : int.Parse(value);

                DateTime? ParseNullableDate(string value)
                    => value == "-" ? null : DateTimeOffset.Parse(value).LocalDateTime;

                if (match.Success)
                {
                    Dispatcher.Invoke(() =>
                    {
                        Logs.Add(new SyslogEntry
                        {
                            Host = id,
                            Time = ParseNullableDate(match.Groups[3].Value),
                            Facility = Enum.GetName(typeof(SyslogFacility), int.Parse(match.Groups[1].Value) / 8),
                            Severity = Enum.GetName(typeof(SyslogSeverity), int.Parse(match.Groups[1].Value) % 8),
                            Hostname = NilToNull(match.Groups[4].Value),
                            AppName = NilToNull(match.Groups[5].Value),
                            ProcessId = ParseNullableInt(match.Groups[6].Value),
                            MessageId = ParseNullableInt(match.Groups[7].Value),
                            StructuredData = NilToNull(match.Groups[8].Value),
                            Message = NilToNull(match.Groups[9].Value),
                        });
                    });
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        Logs.Add(new SyslogEntry
                        {
                            Host = id,
                            Time = DateTime.Now,
                            Message = message,
                        });
                    });
                }
            }
        }
        catch { }

        Clients.TryRemove(id, out _);

        Dispatcher.Invoke(() =>
        {
            ClientList.Items.Remove(id);
        });
    }

    async void Send_Click(object sender, RoutedEventArgs e)
    {
        if (ClientList.SelectedItem == null)
            return;

        string clientId = ClientList.SelectedItem.ToString();
        string message = CommandBox.Text;

        if (!Clients.TryGetValue(clientId, out var client))
            return;

        byte[] data = Encoding.UTF8.GetBytes(message + "\n");

        await client.GetStream().WriteAsync(data);

        CommandBox.Clear();
    }
}