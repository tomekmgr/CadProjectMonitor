using System;

namespace CadProjectCounter.Items
{
    public interface IMonitoredItems
    {
        string FilePath { get; }
        TimeSpan Elapsed { get; set; }
        void StartTimer();
        TimeSpan StopTimer();
        bool IsActive { get; }
    }
}