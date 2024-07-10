using UnityEngine;
using UnityEditor;

/*===============================================================*/
/**
* ジェネリックなシングルトンパターン
* 参考URL : http://msdn.microsoft.com/ja-jp/library/ms998558.aspx
* このジェネリッククラスはclassとnewの型パラメーターの制約をしている
* 参考URL : http://msdn.microsoft.com/ja-jp/library/d5x73970.aspx
* 2014年12月6日 Buravo
*/
public class Singleton<T> where T : class, new()
{
    private static volatile T m_instance;
    private static object m_sync_obj = new();
    public static T Instance
    {
        get
        {
            if (m_instance == null)
            {
                lock (m_sync_obj)
                {
                    if (m_instance == null)
                    {
                        m_instance = new T();
                    }
                }
            }
            return m_instance;
        }
    }
    protected Singleton() { }
}

//Editor
public class SingletonScriptableObject<T> : ScriptableObject where T : ScriptableObject
{
    private static T _instance;
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.FindObjectsOfTypeAll<T>()[0];
            }
            return _instance;
        }
    }
}

public class SingletonEditor<T> : Editor where T : Editor
{
    private static T _instance;
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                //IndexOutOfRangeException
                _instance = Resources.FindObjectsOfTypeAll<T>()[0];
            }
            return _instance;
        }
    }
}