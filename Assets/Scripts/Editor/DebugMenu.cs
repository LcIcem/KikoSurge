#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public static class DebugMenu
{
    private const string KEY = "KikoSurge.Debug.EnemyDebug";

    public static bool IsEnemyDebug
    {
        get
        {
            #if UNITY_EDITOR
            return EditorPrefs.GetBool(KEY, false);
            #else
            return false;
            #endif
        }
        set
        {
            #if UNITY_EDITOR
            EditorPrefs.SetBool(KEY, value);
            #endif
        }
    }

    #if UNITY_EDITOR
    [MenuItem("KikoSurge/Debug/Enemy Debug")]
    private static void ToggleEnemyDebug()
    {
        IsEnemyDebug = !IsEnemyDebug;
        Debug.Log($"[DebugMenu] Enemy Debug: {(IsEnemyDebug ? "ON" : "OFF")}");
    }

    [MenuItem("KikoSurge/Debug/Enemy Debug", true)]
    private static bool ToggleEnemyDebugValidate()
    {
        Menu.SetChecked("KikoSurge/Debug/Enemy Debug", IsEnemyDebug);
        return true;
    }
    #endif
}
