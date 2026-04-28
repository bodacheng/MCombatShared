using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;

public static class GetDefault
{
    public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default)
    {
        return dictionary != null && dictionary.TryGetValue(key, out var value) ? value : defaultValue;
    }
}

public static class RandomSelect
{
    public static List<int> Get(int rangeMin, int rangeMax, int selectCount)
    {
        var target = new List<int>();
        while (target.Count != selectCount)
        {
            var one = Random.Range(rangeMin, rangeMax + 1);
            if (!target.Contains(one))
            {
                target.Add(one);
            }
        }

        return target;
    }
}

public static class Copier<TParent, TChild>
    where TParent : class
    where TChild : class
{
    public static void Copy(TParent parent, TChild child)
    {
        var parentFields = parent.GetType().GetFields();
        var childFields = child.GetType().GetFields();
        var childFieldsDict = childFields.ToDictionary(field => field.Name);

        foreach (var parentField in parentFields)
        {
            if (childFieldsDict.TryGetValue(parentField.Name, out var childField)
                && parentField.FieldType == childField.FieldType)
            {
                childField.SetValue(child, parentField.GetValue(parent));
            }
        }
    }
}

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

public class SingleAssignmentDisposableCleaner
{
    private static readonly List<SingleAssignmentDisposable> Disposables = new List<SingleAssignmentDisposable>();

    public static void Add(SingleAssignmentDisposable disposable)
    {
        if (!Disposables.Contains(disposable))
        {
            Disposables.Add(disposable);
        }
    }

    public static void Clear()
    {
        for (var i = 0; i < Disposables.Count; i++)
        {
            if (Disposables[i] != null && !Disposables[i].IsDisposed)
            {
                Disposables[i].Dispose();
            }
        }

        Disposables.Clear();
    }
}
