using System;
using System.Threading;
using System.Collections.Generic;

namespace IMVU.IDL
{
    public class Counter
    {
        static Dictionary<string, Counter> counters;

        static Counter()
        {
            counters = new Dictionary<string, Counter>();
        }

        public Counter(string name, string meaning)
        {
            lock (counters)
            {
                if (counters.ContainsKey(name))
                {
                    throw new InvalidOperationException("Another counter called " + name + " already exists -- use that!");
                }
                this.Name = name;
                this.Description = meaning;
                counters.Add(Name, this);
            }
        }

        public readonly string Name;
        public readonly string Description;
        long count;

        public long Value { get { return count; } }

        public void Count()
        {
            Interlocked.Increment(ref count);
        }

        public static dict Audit()
        {
            dict ret = new dict();
            lock (counters)
            {
                foreach (KeyValuePair<string, Counter> kvp in counters)
                {
                    dict val = new dict();
                    val.Add("value", kvp.Value.Value);
                    val.Add("help", kvp.Value.Description);
                    ret.Add(kvp.Key, val);
                }
            }
            return ret;
        }
    }
}

