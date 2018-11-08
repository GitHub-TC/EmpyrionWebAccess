using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EWAExtenderCommunication
{
    public static class Extensions
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            if (source == null) return;
            foreach (T element in source) action(element);
        }
    }
}
