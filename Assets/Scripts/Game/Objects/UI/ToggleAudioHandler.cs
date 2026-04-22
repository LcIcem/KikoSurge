using UnityEngine;
using UnityEngine.UI;
using LcIcemFramework;

/// <summary>
/// 开关音效处理器
/// <para>挂载在 Toggle 上，自动播放切换音效</para>
/// </summary>
[RequireComponent(typeof(Toggle))]
public class ToggleAudioHandler : MonoBehaviour
{
    [Header("开启音效")]
    [SerializeField] private AudioClip _onSFX;

    [Header("关闭音效")]
    [SerializeField] private AudioClip _offSFX;

    private Toggle _toggle;

    private void Awake()
    {
        _toggle = GetComponent<Toggle>();
        _toggle.onValueChanged.AddListener(OnToggleValueChanged);
    }

    private void OnDestroy()
    {
        if (_toggle != null)
            _toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
    }

    private void OnToggleValueChanged(bool isOn)
    {
        AudioClip clip = isOn ? _onSFX : _offSFX;
        if (clip != null)
            ManagerHub.Audio?.PlaySFX(clip);
    }
}
