using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unosquare.LedEmotion.Controller
{
    public interface IWorker
    {
        void Start();
        void Stop();
    }
}
