using UnityEngine;
using UnityEngine.UI;
using LcIcemFramework;

/// <summary>
/// 按钮音效处理器
/// <para>挂载在 Button 上，自动播放点击音效</para>
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonAudioHandler : MonoBehaviour
{
    [Header("点击音效")]
    [SerializeField] private AudioClip _clickSFX;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnButtonClick);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnButtonClick);
    }

    private void OnButtonClick()
    {
        if (_clickSFX != null)
            ManagerHub.Audio?.PlaySFX(_clickSFX);
    }
}
