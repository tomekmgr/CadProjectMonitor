using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace CadProjectCounter.Items
{
    public class FileItem : TreeNode, IMonitoredItems
    {
        private Stopwatch timer;

        public FileItem(string text) : base(text)
        {
            //  AD4: File          D:\!Szybowce\SZD-22 Mucha Standard\1_Baza\Mucha.TCW.dwl
            string name = text.Split('\\').Last();
            this.Elapsed = TimeSpan.Zero;
            this.Name = name;
            this.FilePath = ConvertPath(text);
            this.timer = new Stopwatch();
            this.Project = Guid.Empty;
        }

        public FileItem(SerializableFileItem item)
            : base(item.Name)
        {
            this.Name = item.Name;
            this.FilePath = item.FilePath;
            this.timer = new Stopwatch();
            this.Elapsed = string.IsNullOrEmpty(item.Elapsed) ? TimeSpan.Zero : XmlConvert.ToTimeSpan(item.Elapsed);
            this.Project = item.ProjectUid;
        }

        [VisibleProperty]
        public new string Name { get; set; }
        //public new string Text { get { return string.Format("{0} - {1}", this.Name, this.IsActive ? this.GetCurrentTime() : this.Elapsed); } set { } }

        [VisibleProperty]
        public string FilePath { get; private set; }

        [VisibleProperty]
        public TimeSpan Elapsed { get; set; }

        [VisibleProperty]
        public bool IsActive
        {
            get
            {
                return this.timer.IsRunning;
            }
        }

        [VisibleProperty]
        [ReadOnly(true)]
        public Guid Project { get; internal set; }

        public void StartTimer()
        {
            this.timer.Start();
        }

        public TimeSpan StopTimer()
        {
            this.timer.Stop();
            TimeSpan session = this.timer.Elapsed;
            this.Elapsed += this.timer.Elapsed;
            this.timer.Reset();

            return session;
        }

        public TimeSpan GetCurrentTime()
        {
            return this.Elapsed + this.timer.Elapsed;
        }

        public override string ToString()
        {
            this.UpdateText();
            return this.Text;
        }

        public void UpdateText()
        {
            this.Text = string.Format("{0} - {1}", this.Name, this.IsActive ? this.GetCurrentTime() : this.Elapsed);
        }

        internal static string ConvertPath(string text)
        {
            return text.Split(new string[] { "File" }, StringSplitOptions.RemoveEmptyEntries).Last().Trim();
        }
    }
}
