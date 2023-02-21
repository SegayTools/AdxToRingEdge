using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdxToRingEdge.Core
{
    public interface IService : IDisposable
    {
        bool TryProcessUserInput(string[] args);

        void Start();
        void Stop();
        void PrintStatus();
    }
}
