using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SocketServer
{
    public partial class Form1 : Form
    {
        
        private TcpListener server;
        private Thread serverThread;
        private Dictionary<string, TcpClient> clients = new Dictionary<string, TcpClient>();

        private bool serverRunning = false;


        public Form1()
        {
            InitializeComponent();
        }

        private void StartServer(int port = 8000)
        {
            serverRunning = true;

            server = new TcpListener(IPAddress.Any, port);
            server.Start();
            AppendLog("Server started on port " + port);

            while (serverRunning)
            {
                if (server.Pending())
                {
                    // Check if there's a client waiting to connect
                    TcpClient client = server.AcceptTcpClient();
                    string clientId = Guid.NewGuid().ToString();
                    clients[clientId] = client;

                    Invoke(new Action(() => listBoxClients.Items.Add(clientId)));
                    AppendLog(string.Format("Client connected: {0}", clientId));

                    Thread clientThread = new Thread(() => HandleClient(client, clientId));
                    clientThread.IsBackground = true;
                    clientThread.Start();
                }
            }

            // Stop the server after the loop exits
            AppendLog("Server stopped.");
            server.Stop();

        }

        private void HandleClient(TcpClient client, string clientId)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            try
            {
                while (true)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    AppendLog(string.Format("From {0}: {1}", clientId, message));
                }
            }
            catch (Exception ex)
            {
                AppendLog(string.Format("Error with client {0}: {1}", clientId, ex.Message));
            }
            finally
            {
                client.Close();
                clients.Remove(clientId);
                Invoke(new Action(() => listBoxClients.Items.Remove(clientId)));
                AppendLog(string.Format("Client disconnected: {0}", clientId));
            }
        }

        private void AppendLog(string message)
        {
            Invoke(new Action(() =>
            {
                textBoxLog.AppendText(string.Format("{0}: {1}{2}", DateTime.Now, message, Environment.NewLine));
            }));
        }


        private void listBoxClients_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnSend.Enabled = listBoxClients.SelectedItem != null;
            textBoxMessage.Enabled = listBoxClients.SelectedItem != null;
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (listBoxClients.SelectedItem == null) return;

            string clientId = listBoxClients.SelectedItem.ToString();

            TcpClient client; if (clients.TryGetValue(clientId, out client))
            {
                NetworkStream stream = client.GetStream();
                byte[] data = Encoding.UTF8.GetBytes(textBoxMessage.Text);
                stream.Write(data, 0, data.Length);
                AppendLog(string.Format("To {0}: {1}", clientId, textBoxMessage.Text));

            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            btnStop.Enabled = true;
            btnStart.Enabled = false;
            txtPort.Enabled = false;

            lblStatus.Text = "ACTIVE";
            lblStatus.ForeColor = Color.Green;

            serverThread = new Thread(() => StartServer(decimal.ToInt32(txtPort.Value)));
            serverThread.IsBackground = true;
            serverThread.Start();                        
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            btnStop.Enabled = false;
            btnStart.Enabled = true;
            txtPort.Enabled = true;

            lblStatus.Text = "INACTIVE";
            lblStatus.ForeColor = Color.Red;

            serverRunning = false;
            
            foreach (var client in clients.Values)
                client.Close();

            server.Stop();
        }
    }
}
