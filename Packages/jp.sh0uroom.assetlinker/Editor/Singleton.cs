using UnityEditor;
using System;

public class SingletonEditor<T> : Editor where T : Editor
{
    private static readonly Lazy<T> _instance = new Lazy<T>(() => CreateInstance<T>());
    public static T Instance => _instance.Value;
}