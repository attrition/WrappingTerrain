using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

public static class UnityExtensions
{
    public static T GetOrAddComponent<T>(this GameObject child) where T : Component
    {
        T result = child.GetComponent<T>();
        if (result == null)
        {
            result = child.gameObject.AddComponent<T>();
        }
        return result;
    }
}
