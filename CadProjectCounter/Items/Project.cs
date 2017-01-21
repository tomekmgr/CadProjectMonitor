using System;
using System.Collections.Generic;
using System.Linq;

using System.Windows.Forms;

namespace CadProjectCounter.Items
{
    public class Project : TreeNode
    {
        public Project()
        {
            this.Uid = Guid.NewGuid();
        }

        public Project(SerializableProject item)
        {
            this.Uid = item.Uid;
            this.Name = item.Name;
        }

        [VisibleProperty]
        public Guid Uid { get; private set; }

        [VisibleProperty]
        public new string Name { get { return this.Text; }  set { this.Text = value; } }

        [VisibleProperty]
        public TimeSpan ProjectTime
        {
            get
            {
                TimeSpan totalTime = TimeSpan.Zero;
                foreach (FileItem item in this.Nodes.OfType<FileItem>())
                {
                    totalTime += item.GetCurrentTime();
                }

                return totalTime;
            }
        }
    }
}
