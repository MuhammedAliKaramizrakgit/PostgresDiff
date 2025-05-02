using PostgresDiff;
using System.Drawing;
using System.Windows.Forms;

public class ProjectLayerControl : UserControl
{
    private TableLayoutPanel tableLayoutPanel;

    public ProjectLayerControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();

        this.Name = "ProjectLayerControl";
        this.Size = new Size(400, 600);
        this.BackColor = Color.WhiteSmoke;
        this.Padding = new Padding(10);

        // TableLayoutPanel: For horizontal layout
        tableLayoutPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 1,
            AutoSize = false, // AutoSize kapalı!
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };

        tableLayoutPanel.RowStyles.Clear();
        tableLayoutPanel.ColumnStyles.Clear();

        this.Controls.Add(tableLayoutPanel);

        this.ResumeLayout(false);
    }



    public void LoadLayers(List<LayerData> layers)
    {
        if (this.InvokeRequired)
        {
            this.Invoke(new Action(() => LoadLayers(layers)));
            return;
        }

        tableLayoutPanel.Controls.Clear();
        tableLayoutPanel.RowCount = 1; // Hep 1 satır!
        tableLayoutPanel.ColumnCount = layers.Count; // Layer sayısı kadar sütun olacak.
        tableLayoutPanel.RowStyles.Clear();
        tableLayoutPanel.ColumnStyles.Clear();

        // Tek bir satır ekle
        tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // Her layer için bir sütun oluştur
        for (int i = 0; i < layers.Count; i++)
        {
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / layers.Count));

            var innerTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                RowStyles = {
            new RowStyle(SizeType.Absolute, 40),
            new RowStyle(SizeType.Absolute, 150),
            new RowStyle(SizeType.Percent, 100)
        },
                Margin = new Padding(5),
                BackColor = Color.WhiteSmoke
            };

            // Label
            var label = new Label
            {
                Text = layers[i].LayerName,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Gainsboro
            };
            innerTable.Controls.Add(label, 0, 0);

            // ConnectionListView
            var connListView = new ConnectionListView(layers[i].LayerName)
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(5)
            };
            connListView.SetConnections(layers[i].Connections);
            innerTable.Controls.Add(connListView, 0, 1);

            // DdlComparatorControl
            var ddlComparator = new DdlComparatorControl
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(5)
            };
            ddlComparator.SetConnectionListView(layers[i].Connections);
            innerTable.Controls.Add(ddlComparator, 0, 2);

            // Asıl fark burada: (SÜTUNA EKLİYORUZ)
            tableLayoutPanel.Controls.Add(innerTable, i, 0);
        }

    }






}
