using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThredPool
{
    public interface IClass
    {
        public int ID { get; }
        public bool Active { get; }
        public void Start();
        public void Join();
        public void Enqueue(Action a);
    }
}
