using System.Collections.Generic;
using UnityEngine;

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
