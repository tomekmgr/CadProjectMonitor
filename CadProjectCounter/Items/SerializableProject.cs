using System;


namespace CadProjectCounter.Items
{
    public class SerializableProject
    {
        public SerializableProject()
        {
        }

        public SerializableProject(Project item)
        {
            this.Name = item.Name;
            this.Uid = item.Uid;
        }

        public string Name { get; set; }
        public Guid Uid { get; set; }
    }
}
