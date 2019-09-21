
using System.Collections.Concurrent;

namespace Buttplug.Server.Managers.LovenseDongleManager
{
    public class FixedSizedQueue<T> : ConcurrentQueue<T>
    {
        private object lockObject = new object();
        public int Limit { get; set; }

        public FixedSizedQueue(int max) : base()
        {
            Limit = max;
        }

        public new void Enqueue(T obj)
        {
            base.Enqueue(obj);
            lock (lockObject)
            {
                while (Count > Limit && base.TryDequeue(out var overflow))
                {

                };
            }
        }
    }
}
