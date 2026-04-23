using LcIcemFramework;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LoadingPanel : BasePanel
{
    private const string IMG_PROGRESS_BAR = "img_progressBar";
    private const string TXT_PROGRESS_TEXT = "txt_progressText";

    private Image _progressBarImage;
    private TMP_Text _progressText;

    protected override void Awake()
    {
        base.Awake();
        _progressBarImage = GetControl<Image>(IMG_PROGRESS_BAR);
        _progressText = GetControl<TMP_Text>(TXT_PROGRESS_TEXT);
        UpdateProgress(0f);
    }

    public override void Show()
    {
        base.Show();
        UpdateProgress(0f);
    }

    public override bool CanBeClosedByClosePanel => false;

    /// <summary>
    /// 更新加载进度
    /// </summary>
    /// <param name="progress">0.0 ~ 1.0</param>
    public void UpdateProgress(float progress)
    {
        if (_progressBarImage != null)
            _progressBarImage.fillAmount = Mathf.Clamp01(progress);

        if (_progressText != null)
            _progressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
    }
}
