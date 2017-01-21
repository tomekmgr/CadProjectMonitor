using System;
using System.Xml;

namespace CadProjectCounter.Items
{
    public class SerializableFileItem
    {
        public SerializableFileItem()
        {
                
        }

        public SerializableFileItem(IMonitoredItems item)
        {
            if (item is FileItem)
            {
                FileItem fItem = item as FileItem;

                this.Name = fItem.Name;
                this.FilePath = fItem.FilePath;
                this.Elapsed = XmlConvert.ToString(fItem.Elapsed);
                this.ProjectUid = fItem.Project;
            }
        }

        public string Elapsed { get; set; }
        public string FilePath { get; set; }
        public string Name { get; set; }
        public Guid ProjectUid { get; set; }
    }
}