using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LogEntity = AdxToRingEdge.Core.Log<AdxToRingEdge.Core.Utils.AbortableThread>;

namespace AdxToRingEdge.Core.Utils
{
    public class AbortableThread
    {
        private Thread thread;
        private CancellationTokenSource cancellationTokenSource;
        private string threadName;

        public AbortableThread(string threadName, Action<CancellationToken> cancellableMethod)
        {
            cancellationTokenSource = new CancellationTokenSource();
            thread = new Thread(() => cancellableMethod?.Invoke(cancellationTokenSource.Token));
            this.threadName = threadName;
            thread.Name = threadName;
        }

        public ApartmentState ApartmentState
        {
            get
            {
                return thread.GetApartmentState();
            }

            set
            {
                thread.SetApartmentState(value);
            }
        }

        public bool IsBackground
        {
            get
            {
                return thread.IsBackground;
            }

            set
            {
                thread.IsBackground = value;
            }
        }

        public string Name
        {
            get
            {
                return thread.Name;
            }
            set
            {
                thread.Name = threadName = value;
            }
        }

        public void Start()
        {
            thread.Start();
            LogEntity.Debug($"Thread {Name} started.");
        }


        public void Abort()
        {
            LogEntity.Debug($"Begin to abort thread {Name}.");
            cancellationTokenSource.Cancel();
            thread?.Join();
            LogEntity.Debug($"Aborted thread {Name}.");
        }
    }


    public class AbortableThread<T> : AbortableThread
    {
        public AbortableThread(Action<CancellationToken> cancellableMethod) : base(typeof(T).Name + "_Thread", cancellableMethod)
        {

        }
    }
}
