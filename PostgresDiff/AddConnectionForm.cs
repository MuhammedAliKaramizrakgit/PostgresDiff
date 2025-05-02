using System;
using System.Windows.Forms;
using Npgsql;

namespace PostgresDiff
{
    public partial class AddConnectionForm : Form
    {
        private TextBox txtName, txtHost, txtPort, txtDatabase, txtUsername, txtPassword;
        private CheckBox chkInactive;
        private Button btnSave, btnTestConnection;
        private ConnectionListView connectionList;
        private ConnectionItem editingConnection;
        private bool isEditMode = false;

        public AddConnectionForm(ConnectionListView list, ConnectionItem connection = null)
        {
            InitializeComponent();
            this.connectionList = list;
            this.Text = connection == null ? "Add Connection" : "Edit Connection";
            this.Size = new System.Drawing.Size(350, 320);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 8,
                Padding = new Padding(10),
                AutoSize = true
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));

            Label lblName = new Label() { Text = "Name", TextAlign = System.Drawing.ContentAlignment.MiddleRight, Dock = DockStyle.Fill };
            txtName = new TextBox() { Dock = DockStyle.Fill };

            Label lblHost = new Label() { Text = "Host", TextAlign = System.Drawing.ContentAlignment.MiddleRight, Dock = DockStyle.Fill };
            txtHost = new TextBox() { Dock = DockStyle.Fill };

            Label lblPort = new Label() { Text = "Port", TextAlign = System.Drawing.ContentAlignment.MiddleRight, Dock = DockStyle.Fill };
            txtPort = new TextBox() { Dock = DockStyle.Fill, Text = "5432" }; // Varsayılan PostgreSQL portu

            Label lblDatabase = new Label() { Text = "Database", TextAlign = System.Drawing.ContentAlignment.MiddleRight, Dock = DockStyle.Fill };
            txtDatabase = new TextBox() { Dock = DockStyle.Fill };

            Label lblUsername = new Label() { Text = "Username", TextAlign = System.Drawing.ContentAlignment.MiddleRight, Dock = DockStyle.Fill };
            txtUsername = new TextBox() { Dock = DockStyle.Fill };

            Label lblPassword = new Label() { Text = "Password", TextAlign = System.Drawing.ContentAlignment.MiddleRight, Dock = DockStyle.Fill };
            txtPassword = new TextBox() { Dock = DockStyle.Fill, UseSystemPasswordChar = true };

            chkInactive = new CheckBox() { Text = "Inactive", Dock = DockStyle.Fill };

            btnTestConnection = new Button() { Text = "Test Connection", Dock = DockStyle.Fill };
            btnTestConnection.Click += BtnTestConnection_Click;

            btnSave = new Button() { Text = "Save", Dock = DockStyle.Fill };
            btnSave.Click += BtnSave_Click;

            layout.Controls.Add(lblName, 0, 0);
            layout.Controls.Add(txtName, 1, 0);
            layout.Controls.Add(lblHost, 0, 1);
            layout.Controls.Add(txtHost, 1, 1);
            layout.Controls.Add(lblPort, 0, 2);
            layout.Controls.Add(txtPort, 1, 2);
            layout.Controls.Add(lblDatabase, 0, 3);
            layout.Controls.Add(txtDatabase, 1, 3);
            layout.Controls.Add(lblUsername, 0, 4);
            layout.Controls.Add(txtUsername, 1, 4);
            layout.Controls.Add(lblPassword, 0, 5);
            layout.Controls.Add(txtPassword, 1, 5);
            layout.Controls.Add(chkInactive, 1, 6);
            layout.Controls.Add(btnTestConnection, 0, 7);
            layout.Controls.Add(btnSave, 1, 7);

            this.Controls.Add(layout);

            if (connection != null)
            {
                isEditMode = true;
                editingConnection = connection;
                txtName.Text = connection.Name;
                txtHost.Text = connection.Host;
                txtPort.Text = connection.Port;
                txtDatabase.Text = connection.Database;
                txtUsername.Text = connection.Username;
                txtPassword.Text = connection.Password;
                chkInactive.Checked = connection.Inactive;
            }
        }

        private void BtnTestConnection_Click(object sender, EventArgs e)
        {
            string port = txtPort.Text.Trim();
            if (string.IsNullOrWhiteSpace(port) || !int.TryParse(port, out _))
            {
                MessageBox.Show("Invalid port number!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string connectionString = $"Host={txtHost.Text};Port={port};Database={txtDatabase.Text};Username={txtUsername.Text};Password={txtPassword.Text};Timeout=3;";

            using (var conn = new NpgsqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    MessageBox.Show("Connection Successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Connection Failed!\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtHost.Text) ||
                string.IsNullOrWhiteSpace(txtPort.Text) ||
                string.IsNullOrWhiteSpace(txtDatabase.Text) ||
                string.IsNullOrWhiteSpace(txtUsername.Text) ||
                string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("All fields are required.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!int.TryParse(txtPort.Text.Trim(), out _))
            {
                MessageBox.Show("Port must be a number.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (isEditMode && editingConnection != null)
            {
                editingConnection.Name = txtName.Text;
                editingConnection.Host = txtHost.Text;
                editingConnection.Port = txtPort.Text;
                editingConnection.Database = txtDatabase.Text;
                editingConnection.Username = txtUsername.Text;
                editingConnection.Password = txtPassword.Text;
                editingConnection.Inactive = chkInactive.Checked;
            }
            else
            {
                connectionList.AddConnection(txtName.Text, txtHost.Text, txtPort.Text, txtDatabase.Text, txtUsername.Text, txtPassword.Text, chkInactive.Checked);
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
