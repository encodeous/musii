using System;
using System.Collections.Generic;
using System.Text;

namespace musii.Utilities
{
    public class Ref<T>
    {
        public T obj;

        public Ref(T obj)
        {
            obj = obj;
        }
    }
}
