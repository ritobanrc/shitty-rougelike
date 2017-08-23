using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    /// <summary>
    /// A two-way dictionary. Can get T1 given T2 or T2 given T1. 
    /// </summary>
    /// <typeparam name="T1">The "key" for the original dictionary</typeparam>
    /// <typeparam name="T2">The "value" for the original dictionary</typeparam>
public class BidirectionalDictionary<T1, T2> : Dictionary<T1, T2>
{
    // We are inheriting from Dictionary, so we already have an indexer that returns T2 given T1. We need one that returns T1 given T2
    public T1 this[T2 index]
    {
        get
        {
            // If none (non any) of the KeyValuePairs in this dictionary have a value (T2) that is index, KeyNotFound
            if (!this.Any(x => x.Value.Equals(index)))
                throw new KeyNotFoundException();
            // Otherwise, return the key (T1) of the first KeyValuePair where the value is index
            return this.First(x => x.Value.Equals(index)).Key;
        }
    }
}
 
 
 