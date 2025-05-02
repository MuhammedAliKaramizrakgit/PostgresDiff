using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace PostgresDiff
{
    public class ProjectListView : ListView
    {
        public ProjectListView(string title)
        {
            this.View = View.Details;
            this.FullRowSelect = true;
            this.GridLines = true;
            this.Columns.Add(title, -2, HorizontalAlignment.Left);
            this.MultiSelect = false;
        }

        public void AddProject(string projectName)
        {
            if (!string.IsNullOrWhiteSpace(projectName) && !ProjectExists(projectName))
            {
                this.Items.Add(new ListViewItem(projectName));
            }
            else
            {
                MessageBox.Show("Project name is empty or already exists.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public void RemoveSelectedProject()
        {
            if (this.SelectedItems.Count > 0)
            {
                this.Items.Remove(this.SelectedItems[0]);
            }
        }

        public List<string> GetProjects()
        {
            List<string> projects = new();
            foreach (ListViewItem item in this.Items)
            {
                projects.Add(item.Text);
            }
            return projects;
        }

        private bool ProjectExists(string projectName)
        {
            foreach (ListViewItem item in this.Items)
            {
                if (item.Text.Equals(projectName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
