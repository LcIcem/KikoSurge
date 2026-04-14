using UnityEngine;

/// <summary>
/// 检查点交互脚本
/// <para>放置在检查点预设体上，通过触发器检测玩家是否在交互范围内</para>
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class Checkpoint : MonoBehaviour
{
    [Header("交互提示UI（可选）")]
    [SerializeField] private GameObject _promptUI;

    /// <summary>
    /// 玩家是否在交互范围内且检查点未激活
    /// </summary>
    public bool CanInteract => _isPlayerInRange && !_isActivated;

    /// <summary>
    /// 检查点是否已被激活
    /// </summary>
    public bool IsActivated => _isActivated;

    private bool _isPlayerInRange;
    private bool _isActivated;

    private void Awake()
    {
        // 确保碰撞体是触发器
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }

        // 初始隐藏提示UI
        if (_promptUI != null)
            _promptUI.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        _isPlayerInRange = true;
        ShowPrompt(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        _isPlayerInRange = false;
        ShowPrompt(false);
    }

    /// <summary>
    /// 执行检查点交互（由 LevelController 调用）
    /// </summary>
    public void Interact()
    {
        if (!CanInteract)
            return;

        _isActivated = true;
        ShowPrompt(false);

        Debug.Log($"[Checkpoint] 激活检查点: {gameObject.name}");
    }

    private void ShowPrompt(bool show)
    {
        if (_promptUI != null)
            _promptUI.SetActive(show);
    }
}
