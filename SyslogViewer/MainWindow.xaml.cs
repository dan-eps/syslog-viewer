
using System.IO;
using SyslogViewer.Models;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;

namespace SyslogViewer;

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
            var reader = new StreamReader(stream, Encoding.UTF8);

            while (true)
            {
                string line = await reader.ReadLineAsync();

                if (line == null)
                    break;

                Dispatcher.Invoke(() =>
                {
                    Logs.Add(new SyslogEntry
                    {
                        Time = DateTime.Now,
                        Host = id,
                        Message = line
                    });
                });
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