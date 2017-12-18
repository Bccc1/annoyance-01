﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Host_Process_for_Windows_Tasks
{
    public static class Utils
    {

        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default(TValue))
        {
            if (dictionary == null) { throw new ArgumentNullException(nameof(dictionary)); } // using C# 6
            if (key == null) { throw new ArgumentNullException(nameof(key)); } //  using C# 6

            TValue value;
            return dictionary.TryGetValue(key, out value) ? value : defaultValue;
        }
    }

    
}
