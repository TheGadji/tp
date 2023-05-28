using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThredPool
{
    public interface IMyTask<out TResult>
    {
        public bool Complete { get; }
        public TResult Result { get; }
        public void Work();
        public IMyTask<TRes> ContinueWith<TRes>(Func<TResult, TRes> func);
    }
}
