using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Npgsql;
using PostgresDiff;
namespace PostgresDiff
{
    public class ConnectionItem
    {
        public string Host { get; set; }
        public string Port { get; set; }
        public string Database { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool IsConnected { get; set; }
        public string Name { get; set; }
        public bool Inactive { get; set; }

        public string ConnectionString => $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password};Timeout=5;";

        public override string ToString()
        {
            return $"{Name} ({Host}:{Port})";
        }
        public ConnectionItem(string name, string host, string port, string database, string username, string password, bool inactive)
        {
            Name = name;
            Host = host;
            Port = port;
            Database = database;
            Username = username;
            Password = password;
            Inactive = inactive;
            IsConnected = false; // Varsayılan bağlantı durumu
        }
    }



    public class ConnectionListView : ListView
    {
        public List<ConnectionItem> Connections { get; private set; } = new List<ConnectionItem>();

        public ConnectionListView()
        {
            InitializeComponent();
        }

        public ConnectionListView(string masorclient) : this()
        {
            // İlgili bağlantı bilgileri masorclient üzerinden alınabilir
            // Henüz bir işlem tanımlı değil
        }

        public ConnectionListView(ConnectionItem connection) : this()
        {
            if (connection != null)
            {
                AddConnection(connection);
            }
        }

        public ConnectionListView(List<ConnectionItem> connectionList) : this()
        {
            if (connectionList != null)
            {
                foreach (var conn in connectionList)
                    AddConnection(conn);
            }
        }

        private void InitializeComponent()
        {
            View = View.Details;
            FullRowSelect = true;
            GridLines = true;
            CheckBoxes = false;
            MultiSelect = false;

            Columns.Add("Name", 100);
            Columns.Add("Host", 100);
            Columns.Add("Port", 50);
            Columns.Add("Database", 100);
            Columns.Add("Username", 100);
            Columns.Add("Password", 100);
        }

        public void AddConnection(string name, string host, string  port, string database, string username, string password, bool inactive = false)
        {
            var conn = new ConnectionItem(name, host, port, database, username, password, inactive);
            AddConnection(conn);
        }
        public async Task SetConnections(List<ConnectionItem> connections)
        {
            if (!this.IsHandleCreated)
            {
                await Task.Run(() => this.HandleCreated += async (s, e) => await SetConnections(connections));
                return;
            }

            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)(() =>
                {
                    this.Items.Clear();
                    Connections.Clear();

                    foreach (var conn in connections)
                    {
                        Connections.Add(conn);
                        this.Items.Add(new ListViewItem(conn.ToString()) { ImageKey = conn.IsConnected ? "connected" : "disconnected" });
                    }
                }));
            }
            else
            {
                this.Items.Clear();
                Connections.Clear();

                foreach (var conn in connections)
                {
                    Connections.Add(conn);
                    this.Items.Add(new ListViewItem(conn.ToString()) { ImageKey = conn.IsConnected ? "connected" : "disconnected" });
                }
            }
        }

        public void AddConnection(ConnectionItem conn)
        {
            if (conn == null)
                return;

            Connections.Add(conn);

            var item = new ListViewItem(conn.Name);
            item.SubItems.Add(conn.Host);
            item.SubItems.Add(conn.Port.ToString());
            item.SubItems.Add(conn.Database);
            item.SubItems.Add(conn.Username);
            item.SubItems.Add(conn.Password);

            if (conn.Inactive)
                item.ForeColor = System.Drawing.Color.Gray;

            Items.Add(item);
        }

        public List<ConnectionItem> GetSelectedConnections()
        {
            var selected = new List<ConnectionItem>();
            foreach (ListViewItem item in SelectedItems)
            {
                var index = item.Index;
                if (index >= 0 && index < Connections.Count)
                    selected.Add(Connections[index]);
            }
            return selected;
        }
    }
}






