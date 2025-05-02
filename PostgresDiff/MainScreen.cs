using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PostgresDiff
{
    public class MainScreen : Form
    {
        private ListBox listBoxProjects;
        private FlowLayoutPanel flowLayoutPanel1;
        private List<ProjectData> _allProjects;

        private Button btnAddProject;
        private Button btnDeleteProject;
        private Button btnRenameProject;

        public MainScreen()
        {
            this.Text = "Postgres Diff Viewer";
            this.WindowState = FormWindowState.Maximized;

            InitializeDynamicControls();

            Load += MainScreen_Load;
        }
        private SplitContainer splitContainer;

        private void InitializeDynamicControls()
        {
            // SplitContainer: Left = Project List, Right = Project Layers
            splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 100, // Temporary small value
                IsSplitterFixed = false,
                BorderStyle = BorderStyle.Fixed3D,
                Panel1MinSize = 30,
                Panel2MinSize = 100
            };

            // Project ListBox
            listBoxProjects = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10),
                IntegralHeight = false,
                ItemHeight = 24
            };
            listBoxProjects.SelectedIndexChanged += listBoxProjects_SelectedIndexChanged;

            // Buttons
            btnAddProject = new Button
            {
                Text = "Add Project",
                Dock = DockStyle.Top,
                Height = 40
            };
            btnAddProject.Click += btnAddProject_Click;

            btnDeleteProject = new Button
            {
                Text = "Delete Project",
                Dock = DockStyle.Top,
                Height = 40
            };
            btnDeleteProject.Click += btnDeleteProject_Click;

            btnRenameProject = new Button
            {
                Text = "Rename Project",
                Dock = DockStyle.Top,
                Height = 40
            };
            btnRenameProject.Click += btnRenameProject_Click;

            // FlowLayoutPanel for Project Layers
            flowLayoutPanel1 = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight, // << BURAYA DİKKAT!!!
                BackColor = Color.WhiteSmoke,
                Padding = new Padding(10)
            };

            // Add controls to SplitContainer
            splitContainer.Panel1.Controls.Add(listBoxProjects);
            splitContainer.Panel1.Controls.Add(btnRenameProject);
            splitContainer.Panel1.Controls.Add(btnDeleteProject);
            splitContainer.Panel1.Controls.Add(btnAddProject);
            splitContainer.Panel2.Controls.Add(flowLayoutPanel1);

            // Add SplitContainer to Form
            Controls.Add(splitContainer);
        }

        private void MainScreen_Load(object sender, EventArgs e)
        {
            LoadAllProjects();

            // Now the form has correct size, so we can adjust SplitterDistance properly
            splitContainer.SplitterDistance = this.Width / 15; // 6% of form width
        }



        private void LoadAllProjects()
        {
            _allProjects = ProjectDataHelper.LoadAllProjectDatas();

            listBoxProjects.Items.Clear();

            foreach (var project in _allProjects)
            {
                listBoxProjects.Items.Add(project.ProjectName);
            }
        }

        private void listBoxProjects_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBoxProjects.SelectedIndex >= 0)
            {
                flowLayoutPanel1.Controls.Clear();

                var selectedProject = _allProjects[listBoxProjects.SelectedIndex];
                LoadProject(selectedProject);
            }
        }

        private void LoadProject(ProjectData projectData)
        {
            foreach (var layer in projectData.Layers)
            {
                // Katman için yatay bir panel (layerPanel) oluştur
                var layerPanel = new TableLayoutPanel
                {
                    ColumnCount = layer.Connections.Count + 1, // Tüm bağlantılar + 1 adet DdlComparatorControl
                    RowCount = 1,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    Margin = new Padding(10),
                    Dock = DockStyle.Top
                };

                layerPanel.ColumnStyles.Clear();
                for (int i = 0; i < layer.Connections.Count; i++)
                {
                    layerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                }
                layerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f)); // Comparator genişletilebilir

                // ConnectionListView kontrollerini ekle
                foreach (var conn in layer.Connections)
                {
                    var connView = new ConnectionListView(conn)
                    {
                        Dock = DockStyle.Fill,
                        Margin = new Padding(5)
                    };
                    layerPanel.Controls.Add(connView);
                }

                // DdlComparatorControl ekle
                var comparator = new DdlComparatorControl
                {
                    Dock = DockStyle.Fill,
                    Margin = new Padding(5)
                };
                comparator.SetConnectionListView(layer.Connections); // varsa bağlantıları yükle

                layerPanel.Controls.Add(comparator);

                // flowLayoutPanel1 içine ekle
                flowLayoutPanel1.Controls.Add(layerPanel);

                // Sağ tıklama menüsü (aynen korunuyor)
                var contextMenu = new ContextMenuStrip();
                contextMenu.Items.Add("Edit Layer", null, (s, e) => EditLayer(layer));
                contextMenu.Items.Add("Add New Connection", null, (s, e) => AddNewConnection(layer));
                layerPanel.ContextMenuStrip = contextMenu;
            }
        }


        // Add Project
        private void btnAddProject_Click(object sender, EventArgs e)
        {
            string projectName = Microsoft.VisualBasic.Interaction.InputBox("Enter New Project Name", "Add Project", "", -1, -1);

            if (!string.IsNullOrEmpty(projectName))
            {
                // Create new project
                var newProject = new ProjectData { ProjectName = projectName };

                // Add new project to list
                _allProjects.Add(newProject);
                listBoxProjects.Items.Add(projectName);

                // Save project data (for example, to JSON)
                ProjectDataHelper.SaveAllProjectDatas(_allProjects);

                MessageBox.Show("New project added successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Project name cannot be empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Delete Project
        private void btnDeleteProject_Click(object sender, EventArgs e)
        {
            if (listBoxProjects.SelectedIndex >= 0)
            {
                var selectedProject = _allProjects[listBoxProjects.SelectedIndex];

                // Confirm delete
                var result = MessageBox.Show($"Are you sure you want to delete the project '{selectedProject.ProjectName}'?",
                                              "Delete Project", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    // Remove from list and save
                    _allProjects.Remove(selectedProject);
                    listBoxProjects.Items.RemoveAt(listBoxProjects.SelectedIndex);

                    // Save project data
                    ProjectDataHelper.SaveAllProjectDatas(_allProjects);

                    MessageBox.Show("Project deleted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show("Please select a project to delete.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Rename Project
        private void btnRenameProject_Click(object sender, EventArgs e)
        {
            if (listBoxProjects.SelectedIndex >= 0)
            {
                var selectedProject = _allProjects[listBoxProjects.SelectedIndex];
                string newName = Microsoft.VisualBasic.Interaction.InputBox("Enter New Project Name", "Rename Project", selectedProject.ProjectName, -1, -1);

                if (!string.IsNullOrEmpty(newName))
                {
                    selectedProject.ProjectName = newName;
                    listBoxProjects.Items[listBoxProjects.SelectedIndex] = newName;

                    // Save updated project data
                    ProjectDataHelper.SaveAllProjectDatas(_allProjects);

                    MessageBox.Show("Project renamed successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Project name cannot be empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a project to rename.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Edit Layer
        private void EditLayer(LayerData layer)
        {
            MessageBox.Show($"Editing Layer: {layer.LayerName}", "Edit", MessageBoxButtons.OK, MessageBoxIcon.Information);
            // Add your layer editing functionality here
        }

        // Add New Connection
        private void AddNewConnection(LayerData layer)
        {
            MessageBox.Show($"Adding New Connection to Layer: {layer.LayerName}", "Add Connection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            // Add your connection adding functionality here
        }
    }
}
