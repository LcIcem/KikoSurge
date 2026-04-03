using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 输入配置 ScriptableObject
/// <para> 存储所有动作的默认键位 </para>
/// <para> 通过 Assets/Create/KikoSurge/Input/Default Input Config 创建实例 </para>
/// </summary>
[CreateAssetMenu(fileName = "InputConfig_SO", menuName = "KikoSurge/Input/Default Input Config")]
public class InputConfig_SO : ScriptableObject
{
    [Header("移动键位")]
    public KeyCode moveUp    = KeyCode.W;
    public KeyCode moveDown  = KeyCode.S;
    public KeyCode moveLeft  = KeyCode.A;
    public KeyCode moveRight = KeyCode.D;

    [Header("动作键位")]
    public KeyCode shoot    = KeyCode.Mouse0;
    public KeyCode altShoot  = KeyCode.Mouse1;
    public KeyCode interact  = KeyCode.E;
    public KeyCode pause     = KeyCode.Escape;

    /// <summary>将 SO 中的所有键位导出为 Dictionary</summary>
    public Dictionary<InputActionType, KeyCode> ToDictionary()
    {
        return new Dictionary<InputActionType, KeyCode>
        {
            { InputActionType.MoveUp,    moveUp    },
            { InputActionType.MoveDown,  moveDown  },
            { InputActionType.MoveLeft,  moveLeft  },
            { InputActionType.MoveRight, moveRight },
            { InputActionType.Shoot,     shoot     },
            { InputActionType.AltShoot,  altShoot  },
            { InputActionType.Interact,  interact  },
            { InputActionType.Pause,    pause     },
        };
    }

    /// <summary>
    /// 硬编码默认键位（SO 不存在时的最后防线，也是 SO 字段的默认值来源）
    /// </summary>
    public static Dictionary<InputActionType, KeyCode> GetHardcodedDefaults()
    {
        return new Dictionary<InputActionType, KeyCode>
        {
            { InputActionType.MoveUp,    KeyCode.W },
            { InputActionType.MoveDown,  KeyCode.S },
            { InputActionType.MoveLeft,  KeyCode.A },
            { InputActionType.MoveRight, KeyCode.D },
            { InputActionType.Shoot,     KeyCode.Mouse0 },
            { InputActionType.AltShoot,  KeyCode.Mouse1 },
            { InputActionType.Interact,  KeyCode.E },
            { InputActionType.Pause,     KeyCode.Escape },
        };
    }
}