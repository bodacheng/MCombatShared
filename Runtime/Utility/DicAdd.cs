using System.Collections.Generic;
using UnityEngine;

public static class DicAdd<Key, Value>
{
    public static void Add(IDictionary<Key, Value> dic, Key key, Value value)
    {
        if (key == null)
        {
            Debug.Log("key值严重错误。欲添加的value值：" + value);
            return;
        }

        if (dic.ContainsKey(key))
        {
            dic[key] = value;
        }
        else
        {
            dic.Add(key, value);
        }
    }
}
