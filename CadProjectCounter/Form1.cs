using CadProjectCounter.Items;
using CadProjectCounter.Searcher;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

using System.Windows.Forms;
using System.Xml.Serialization;

namespace CadProjectCounter
{
    public partial class Form1 : Form
    {
        private string[] lastActiveFiles;
        private List<IMonitoredItems> monitoredItems;
        private List<Project> projects;
        private System.Timers.Timer processCheck;

        public Form1()
        {
            InitializeComponent();
            this.monitoredItems = new List<IMonitoredItems>();
            this.lastActiveFiles = new string[0];
            this.projects = new List<Project>();
            this.Load();
            this.ShowFiles();
            this.processCheck_Tick(this, EventArgs.Empty);
            this.propertyGrid1.BrowsableAttributes = new AttributeCollection(new[] { new VisiblePropertyAttribute() });
            this.processCheck = new System.Timers.Timer(15000);
            this.processCheck.AutoReset = true;
            this.processCheck.Elapsed += this.processCheck_Tick;
        }

        private void processCheck_Tick(object sender, EventArgs e)
        {
            this.Log("Searching for CAD...");
            //string[] handles = OpenFileSearcher.FindFiles("tcw22");
            //string[] activeFile = handles.Where(item => item.Contains(".dwl")).ToArray();

            StringBuilder sb = new StringBuilder();
            Process handlesProc = new Process();
            handlesProc.StartInfo = new ProcessStartInfo("handle.exe", "-p tcw22") { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardInput = true };
            handlesProc.OutputDataReceived += (sendera, ee) => { sb.AppendLine(ee.Data); };
            handlesProc.Start();
            handlesProc.BeginOutputReadLine();
            handlesProc.WaitForExit();

            string[] handleLines = sb.ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Where(item => item.Contains(": File")).ToArray();
            string[] activeFile = handleLines.Where(item => item.Contains(".dwl")).ToArray();
            
            foreach (string file in activeFile)
            {
                //  AD4: File          D:\!Szybowce\SZD-22 Mucha Standard\1_Baza\Mucha.TCW.dwl
                this.Log("Found '{0}'", file);

                string name = file.Split('\\').Last();
                FileItem item = new FileItem(file);
                IMonitoredItems selected = this.monitoredItems.FirstOrDefault(itema => itema.FilePath.Equals(item.FilePath));
                if (selected == null)
                {
                    item.StartTimer();
                    this.monitoredItems.Add(item);
                }
                else
                {
                    if (!selected.IsActive)
                    {
                        selected.StartTimer();
                    }
                }
            }

            foreach (string lastItem in this.lastActiveFiles)
            {
                if (!activeFile.Contains(lastItem))
                {
                    IMonitoredItems monitoredItem = this.monitoredItems.FirstOrDefault(item => item.FilePath == FileItem.ConvertPath(lastItem));
                    if (monitoredItem != null)
                    {
                        this.Log("Stopping monitor for: {0}", monitoredItem.FilePath);
                        TimeSpan sessionTime = monitoredItem.StopTimer();
                        this.Log("Session ended after: {0}", sessionTime);
                    }
                    else
                    {
                        this.Log("FATAL: Last Item not found on monitored list...");
                    }
                }
            }

            this.lastActiveFiles = activeFile;
            this.ShowFiles();
        }

        //private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        //{
        //    this.Save();
        //}

        private void Save(bool stopCounters = true)
        {
            this.processCheck.Enabled = false;
            this.processCheck_Tick(this, EventArgs.Empty);

            if (stopCounters)
            {
                foreach (IMonitoredItems item in this.monitoredItems)
                {
                    if (item.IsActive)
                    {
                        item.StopTimer();
                    }
                }
            }

            this.SaveProject();

            List<SerializableFileItem> serializable = new List<SerializableFileItem>();

            foreach (IMonitoredItems item in this.monitoredItems)
            {
                serializable.Add(new SerializableFileItem(item));
            }

            XmlSerializer ser = new XmlSerializer(typeof(List<SerializableFileItem>));
            using (FileStream fs = File.Open("stats.xml", FileMode.Create))
            {
                ser.Serialize(fs, serializable);
            }

            this.processCheck.Enabled = true;
        }

        private void SaveProject()
        {
            List<SerializableProject> serializable = new List<SerializableProject>();

            foreach (Project item in this.projects)
            {
                serializable.Add(new SerializableProject(item));
            }
            XmlSerializer ser = new XmlSerializer(typeof(List<SerializableProject>));
            using (FileStream fs = File.Open("proj.xml", FileMode.Create))
            {
                ser.Serialize(fs, serializable);
            }
        }

        private void Load()
        {
            this.LoadProjects();

            if (!File.Exists("stats.xml"))
            {
                return;
            }

            XmlSerializer ser = new XmlSerializer(typeof(List<SerializableFileItem>));
            using (FileStream fs = File.OpenRead("stats.xml"))
            {
                List<SerializableFileItem> items = (List<SerializableFileItem>)ser.Deserialize(fs);

                foreach (SerializableFileItem item in items)
                {
                    this.monitoredItems.Add(new FileItem(item));
                }
            }
        }

        private void LoadProjects()
        {
            if (!File.Exists("proj.xml"))
            {
                return;
            }

            this.projects.Clear();
            XmlSerializer ser = new XmlSerializer(typeof(List<SerializableProject>));
            using (FileStream fs = File.OpenRead("proj.xml"))
            {
                List<SerializableProject> items = (List<SerializableProject>)ser.Deserialize(fs);

                foreach (SerializableProject item in items)
                {
                    this.projects.Add(new Project(item));
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ShowFiles();
        }

        private void ShowFiles()
        {
            if (this.treeView1.InvokeRequired)
            {
                this.treeView1.Invoke(new Action(this.ShowFiles));
            }
            else
            {
                this.treeView1.Nodes.Clear();
                foreach (FileItem item in this.monitoredItems.OfType<FileItem>())
                {
                    item.UpdateText();
                }

                foreach (Project project in this.projects)
                {
                    project.Nodes.Clear();
                    project.Nodes.AddRange(this.monitoredItems.OfType<FileItem>().Where(item => item.Project.Equals(project.Uid)).ToArray());

                    this.treeView1.Nodes.Add(project);
                }

                foreach (FileItem item in this.monitoredItems.OfType<FileItem>().Where(item => item.Project.Equals(Guid.Empty)))
                {
                    this.treeView1.Nodes.Add(item);
                }
            }
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            this.propertyGrid1.SelectedObject = e.Node;
            this.treeView1.SelectedNode = e.Node;
        }

        private void createProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.projects.Add(new Project());

            this.ShowFiles();
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            if (treeView1.SelectedNode is FileItem)
            {
                this.assignToolStripMenuItem.Visible = true;
                this.assignToolStripMenuItem.DropDownItems.Clear();
                foreach (Project project in this.projects)
                {
                    ToolStripItem item = this.assignToolStripMenuItem.DropDownItems.Add(project.Name);
                    item.Tag = project;
                    item.Click += this.AssignProjectClickHandler;
                }

                if (!(this.treeView1.SelectedNode as FileItem).Project.Equals(Guid.Empty))
                {
                    this.removeFromProjectToolStripMenuItem.Enabled = true;
                    this.assignToolStripMenuItem.Enabled = false;
                }
                else
                {
                    this.removeFromProjectToolStripMenuItem.Enabled = false;
                    this.assignToolStripMenuItem.Enabled = true;

                }

                this.deleteProjectToolStripMenuItem.Enabled = false;
            }

            if (this.treeView1.SelectedNode is Project)
            {
                this.assignToolStripMenuItem.Enabled = false;
                this.removeFromProjectToolStripMenuItem.Enabled = false;
                this.deleteProjectToolStripMenuItem.Enabled = true;
            }
        }

        private void AssignProjectClickHandler(object sender, EventArgs e)
        {
            if (sender is ToolStripItem)
            {
                if (treeView1.SelectedNode is FileItem)
                {
                    FileItem fileItem = treeView1.SelectedNode as FileItem;
                    fileItem.Project = ((sender as ToolStripItem).Tag as Project).Uid;

                    this.ShowFiles();
                }
            }
        }

        private void deleteProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Project delete
            if (this.treeView1.SelectedNode is Project)
            {
                Project project = this.treeView1.SelectedNode as Project;
                FileItem[] items = this.monitoredItems.OfType<FileItem>().Where(item => item.Project == project.Uid).ToArray();

                foreach (FileItem item in items)
                {
                    item.Project = Guid.Empty;
                }

                this.treeView1.Nodes.Remove(project);
                this.projects.Remove(project);

                this.ShowFiles();
            }
        }

        private void removeFromProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode is FileItem)
            {
                FileItem fileItem = treeView1.SelectedNode as FileItem;
                fileItem.Project = Guid.Empty;

                this.ShowFiles();
            }
        }

        private void saveTimer_Tick(object sender, EventArgs e)
        {
            this.Log("Autosaving files");
            this.Save(false);
        }

        private void Form1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Normal;
            }

            if (!this.Visible)
            {
                this.Show();
            }
            else
            {
                this.Hide();
            }
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            this.Log("Application started");
            this.processCheck.Enabled = true;
            this.Hide();
        }

        private void Log(string format, params object[] arg)
        {
            this.Log(string.Format(format, arg));
        }

        private void Log(string text)
        {
            if (this.richTextBox1.InvokeRequired)
            {
                this.richTextBox1.Invoke(new Action(() => Log(text)));
            }
            else
            {
                if (this.richTextBox1.Lines.Count() > 1000)
                {
                    this.richTextBox1.Clear();
                }

                this.richTextBox1.AppendText(text + Environment.NewLine);
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Save(false);
        }
    }
}
