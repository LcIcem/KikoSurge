using LcIcemFramework;
using UnityEngine;
using UnityEngine.UI;
using LcIcemFramework.Core;
using Game.Event;
using Unity.VisualScripting;
using System.Collections;
using ProcGen.Core;

/// <summary>
/// 游戏内战斗HUD面板
/// <para>统一管理血量、金币、武器、波次等HUD元素的显示</para>
/// <para>使用约定：子控件 GameObject 名称即 GetControl 的 key</para>
/// </summary>
public class HubPanel : BasePanel
{
    // HUD 控件名称常量（与子对象名字一一对应）
    private const string IMG_GOLD = "img_gold";
    private const string TXT_GOLD = "txt_gold";
    private const string IMG_WEAPON = "img_weapon";
    private const string TXT_WEAPON = "txt_weapon";
    private const string IMG_AMMO = "img_ammo";
    private const string TXT_AMMO = "txt_ammo";
    private const string TXT_WAVE = "txt_wave";
    private const string TXT_ROOM = "txt_room";
    private const string IMG_ITEM_SLOT_1 = "img_itemSlot1";
    private const string IMG_ITEM_SLOT_2 = "img_itemSlot2";
    private const string IMG_ITEM_SLOT_3 = "img_itemSlot3";
    private const string IMG_ITEM_SLOT_4 = "img_itemSlot4";
    private const string TXT_ITEM_COUNT = "txt_itemCount";

    // 波次淡入淡出时长
    private const float WAVE_FADE_DURATION = 0.3f;

    // 波次淡入协程标识
    private IEnumerator _waveFadeInCoroutine;
    // 波次淡出协程标识
    private IEnumerator _waveFadeOutCoroutine;

    public override void Show()
    {
        base.Show();
        SubscribeEvents();
        // 初始化红屏控件为隐藏状态
        EnsureDamageFlashHidden();
        // 请求刷新当前房间UI
        EventCenter.Instance.Publish(GameEventID.OnRequestRoomRefresh);
        // 主动刷新心形UI（确保Hub显示时生命值正确）
        RefreshHeartDisplay();
    }

    /// <summary>
    /// 刷新心形UI显示
    /// </summary>
    private void RefreshHeartDisplay()
    {
        var playerData = SessionManager.Instance?.GetPlayerData();
        if (playerData != null)
        {
            EventCenter.Instance.Publish(GameEventID.UpdateHeartDisplay, playerData);
        }
    }

    /// <summary>
    /// 确保红屏控件默认隐藏
    /// </summary>
    private void EnsureDamageFlashHidden()
    {
        if (_damageFlashImage == null)
            _damageFlashImage = GetControl<Image>(IMG_DAMAGE_FLASH);

        if (_damageFlashImage != null)
        {
            _damageFlashImage.gameObject.SetActive(false);
        }
    }

    public override void Hide()
    {
        base.Hide();
        UnsubscribeEvents();
    }

    private void SubscribeEvents()
    {
        EventCenter.Instance.Subscribe<int>(GameEventID.OnGoldChanged, OnGoldChanged);
        EventCenter.Instance.Subscribe<WeaponBase>(GameEventID.OnCurrentWeaponChanged, OnWeaponChanged);
        EventCenter.Instance.Subscribe<WeaponBase>(GameEventID.OnAmmoChanged, OnAmmoChanged);
        EventCenter.Instance.Subscribe<ReloadProgressParams>(GameEventID.OnReloadProgress, OnReloadProgress);
        EventCenter.Instance.Subscribe<WaveStartParams>(GameEventID.OnWaveStarted, OnWaveStarted);
        EventCenter.Instance.Subscribe<WaveClearedParams>(GameEventID.OnWaveCleared, OnWaveCleared);
        EventCenter.Instance.Subscribe<WaveUpdateParams>(GameEventID.OnWaveUpdate, OnWaveUpdate);
        EventCenter.Instance.Subscribe<DamageParams>(GameEventID.OnPlayerDamaged, OnPlayerDamaged);
        EventCenter.Instance.Subscribe<RoomBehaviourEntry>(GameEventID.OnBehaviourEnd, OnBehaviourEnd);
        EventCenter.Instance.Subscribe<RoomEnterParams>(GameEventID.OnRoomEnter, OnRoomEnter);
        EventCenter.Instance.Subscribe<CorridorEnterParams>(GameEventID.OnCorridorEnter, OnCorridorEnter);
    }

    private void UnsubscribeEvents()
    {
        EventCenter.Instance.Unsubscribe<int>(GameEventID.OnGoldChanged, OnGoldChanged);
        EventCenter.Instance.Unsubscribe<WeaponBase>(GameEventID.OnCurrentWeaponChanged, OnWeaponChanged);
        EventCenter.Instance.Unsubscribe<WeaponBase>(GameEventID.OnAmmoChanged, OnAmmoChanged);
        EventCenter.Instance.Unsubscribe<ReloadProgressParams>(GameEventID.OnReloadProgress, OnReloadProgress);
        EventCenter.Instance.Unsubscribe<WaveStartParams>(GameEventID.OnWaveStarted, OnWaveStarted);
        EventCenter.Instance.Unsubscribe<WaveClearedParams>(GameEventID.OnWaveCleared, OnWaveCleared);
        EventCenter.Instance.Unsubscribe<WaveUpdateParams>(GameEventID.OnWaveUpdate, OnWaveUpdate);
        EventCenter.Instance.Unsubscribe<DamageParams>(GameEventID.OnPlayerDamaged, OnPlayerDamaged);
        EventCenter.Instance.Unsubscribe<RoomBehaviourEntry>(GameEventID.OnBehaviourEnd, OnBehaviourEnd);
        EventCenter.Instance.Unsubscribe<RoomEnterParams>(GameEventID.OnRoomEnter, OnRoomEnter);
        EventCenter.Instance.Unsubscribe<CorridorEnterParams>(GameEventID.OnCorridorEnter, OnCorridorEnter);
    }

    #region 事件处理

    private void OnGoldChanged(int gold)
    {
        var text = GetControl<Text>(TXT_GOLD);
        if (text != null)
            text.text = gold.ToString();
    }

    private void OnWeaponChanged(WeaponBase weapon)
    {
        var weaponImg = GetControl<Image>(IMG_WEAPON);
        var weaponText = GetControl<Text>(TXT_WEAPON);
        var ammoImg = GetControl<Image>(IMG_AMMO);
        var ammoText = GetControl<Text>(TXT_AMMO);

        if (weaponImg != null && weapon != null && weapon.Config.itemConfig?.Icon != null)
        {
            weaponImg.sprite = weapon.Config.itemConfig.Icon;
            weaponImg.preserveAspect = true;
        }

        if (weaponText != null && weapon != null)
            weaponText.text = weapon.Config.itemConfig?.Name ?? "未知武器";

        UpdateAmmoDisplay(weapon, ammoImg, ammoText);
    }

    private void OnAmmoChanged(WeaponBase weapon)
    {
        var ammoImg = GetControl<Image>(IMG_AMMO);
        var ammoText = GetControl<Text>(TXT_AMMO);
        UpdateAmmoDisplay(weapon, ammoImg, ammoText);
    }

    private void OnReloadProgress(ReloadProgressParams p)
    {
        var ammoImg = GetControl<Image>(IMG_AMMO);
        if (ammoImg != null)
        {
            ammoImg.fillAmount = p.progress;
            ammoImg.color = Color.yellow;
        }
    }

    private void UpdateAmmoDisplay(WeaponBase weapon, Image ammoImg, Text ammoText)
    {
        if (weapon == null) return;

        if (ammoText != null)
            ammoText.text = $"{weapon.CurrentAmmo}/{weapon.Config.magazineSize}";

        if (ammoImg != null && weapon.Config.bulletConfig != null && weapon.Config.bulletConfig.bulletPrefab != null)
        {
            var bullet = weapon.Config.bulletConfig.bulletPrefab.GetComponent<Bullet>();
            if (bullet != null && bullet.Icon != null)
            {
                ammoImg.sprite = bullet.Icon;
                ammoImg.preserveAspect = true;
            }

            ammoImg.fillAmount = 1f;

            float ratio = (float)weapon.CurrentAmmo / weapon.Config.magazineSize;
            ammoImg.color = ratio <= 0.2f ? Color.red.WithAlpha(125) : Color.white;
        }
    }

    private void OnWaveStarted(WaveStartParams p)
    {
        var text = GetControl<Text>(TXT_WAVE);
        if (text != null)
        {
            text.gameObject.SetActive(true);
            text.text = $"{p.behaviourName} {p.currentWave}/{p.totalWaves} ({p.enemiesInWave})";
            FadeInWave(text);
        }
    }

    private void OnWaveCleared(WaveClearedParams p)
    {
        var text = GetControl<Text>(TXT_WAVE);
        if (text != null)
        {
            // 异步模式下 OnWaveCleared 可能不会被调用，这里只淡出
            FadeOutWave(text);
        }
    }

    private void OnWaveUpdate(WaveUpdateParams p)
    {
        var text = GetControl<Text>(TXT_WAVE);
        if (text != null)
        {
            // 异步模式下更新：行为名 波次/总波次 (剩余敌人)
            text.text = $"{p.behaviourName} {p.currentWave}/{p.totalWaves} ({p.remainingEnemies})";
        }
    }

    private void OnBehaviourEnd(RoomBehaviourEntry behaviour)
    {
        var text = GetControl<Text>(TXT_WAVE);
        if (text != null)
        {
            // 立即隐藏，不等待淡出
            text.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 波次UI淡入
    /// </summary>
    private void FadeInWave(Text text)
    {
        // 停止之前的淡入/淡出协程
        if (_waveFadeInCoroutine != null)
            MonoManager.Instance.StopCoroutine(_waveFadeInCoroutine);
        if (_waveFadeOutCoroutine != null)
            MonoManager.Instance.StopCoroutine(_waveFadeOutCoroutine);

        text.color = new Color(text.color.r, text.color.g, text.color.b, 0f);
        text.gameObject.SetActive(true);
        _waveFadeInCoroutine = FadeTextAlpha(text, 0f, 1f, WAVE_FADE_DURATION);
        MonoManager.Instance.StartCoroutine(_waveFadeInCoroutine);
    }

    /// <summary>
    /// 波次UI淡出（延迟后隐藏）
    /// </summary>
    private void FadeOutWave(Text text)
    {
        // 停止之前的淡入/淡出协程
        if (_waveFadeInCoroutine != null)
            MonoManager.Instance.StopCoroutine(_waveFadeInCoroutine);
        if (_waveFadeOutCoroutine != null)
            MonoManager.Instance.StopCoroutine(_waveFadeOutCoroutine);

        // 先完成淡入，再淡出
        _waveFadeOutCoroutine = FadeOutSequence(text);
        MonoManager.Instance.StartCoroutine(_waveFadeOutCoroutine);
    }

    private IEnumerator FadeOutSequence(Text text)
    {
        // 先完成淡入（如果有淡入在进行中）
        if (_waveFadeInCoroutine != null)
        {
            yield return _waveFadeInCoroutine;
        }

        // 延迟1秒后开始淡出
        yield return new WaitForSeconds(1f);

        yield return FadeTextAlpha(text, 1f, 0f, WAVE_FADE_DURATION);
    }

    /// <summary>
    /// 文本透明度渐变协程
    /// </summary>
    private IEnumerator FadeTextAlpha(Text text, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(from, to, t);
            text.color = new Color(text.color.r, text.color.g, text.color.b, alpha);
            yield return null;
        }
        text.color = new Color(text.color.r, text.color.g, text.color.b, to);
    }

    private void OnPlayerDamaged(DamageParams p)
    {
        TriggerDamageFlash();
    }

    // 红屏特效相关
    private Image _damageFlashImage;
    private Material _damageFlashMaterial;
    private IEnumerator _damageFlashCoroutine;
    private const string IMG_DAMAGE_FLASH = "img_damageFlash";

    // Shader 属性名称（与 UIRadialGradient.shader 中的 Properties 对应）
    private const string K_RADIUS = "_Radius";
    private const string K_ALPHA = "_Alpha";

    /// <summary>
    /// 触发受伤红屏闪烁（径向渐变：从边缘红到中心透明）
    /// </summary>
    private void TriggerDamageFlash()
    {
        if (_damageFlashImage == null)
            _damageFlashImage = GetControl<Image>(IMG_DAMAGE_FLASH);

        if (_damageFlashImage == null) return;

        // 初始化材质（使用 material 获取可修改的实例）
        if (_damageFlashMaterial == null)
            _damageFlashMaterial = _damageFlashImage.material;

        // 停止之前的闪烁协程
        if (_damageFlashCoroutine != null)
            MonoManager.Instance.StopCoroutine(_damageFlashCoroutine);

        _damageFlashCoroutine = DamageFlashSequence();
        MonoManager.Instance.StartCoroutine(_damageFlashCoroutine);
    }

    /// <summary>
    /// 红屏闪烁序列（径向渐变 Shader 版本）
    /// 边缘红色高 → 中心渐变透明 → 整体淡出
    /// </summary>
    private IEnumerator DamageFlashSequence()
    {
        _damageFlashImage.gameObject.SetActive(true);

        // 从编辑器设置的当前值开始动画（不重置）
        float startRadius = _damageFlashMaterial.GetFloat(K_RADIUS);

        // 阶段1：淡入（快速）
        float fadeInDuration = 0.05f;
        float fadeInElapsed = 0f;
        while (fadeInElapsed < fadeInDuration)
        {
            fadeInElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(fadeInElapsed / fadeInDuration);
            _damageFlashMaterial.SetFloat(K_ALPHA, t);
            yield return null;
        }
        _damageFlashMaterial.SetFloat(K_ALPHA, 1f);

        // 阶段2：半径收缩（保持显示一段时间）
        float holdDuration = 0.15f;
        float shrinkDuration = 0.2f;
        float shrinkElapsed = 0f;
        while (shrinkElapsed < shrinkDuration + holdDuration)
        {
            shrinkElapsed += Time.deltaTime;
            if (shrinkElapsed < shrinkDuration)
            {
                float t = shrinkElapsed / shrinkDuration;
                float radius = Mathf.Lerp(startRadius, 0f, t);
                _damageFlashMaterial.SetFloat(K_RADIUS, radius);
            }
            yield return null;
        }

        // 阶段3：淡出
        float fadeOutDuration = 0.15f;
        float fadeOutElapsed = 0f;
        while (fadeOutElapsed < fadeOutDuration)
        {
            fadeOutElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(fadeOutElapsed / fadeOutDuration);
            _damageFlashMaterial.SetFloat(K_ALPHA, 1f - t);
            yield return null;
        }

        _damageFlashImage.gameObject.SetActive(false);
    }

    private void OnRoomEnter(RoomEnterParams p)
    {
        var text = GetControl<Text>(TXT_ROOM);
        if (text != null)
        {
            text.text = GetRoomDisplayName(p.roomType);
        }
    }

    private void OnCorridorEnter(CorridorEnterParams p)
    {
        var text = GetControl<Text>(TXT_ROOM);
        if (text != null)
        {
            text.text = "走廊";
        }
    }

    private string GetRoomDisplayName(RoomType roomType)
    {
        return roomType switch
        {
            RoomType.Start => "起始房间",
            RoomType.Normal => "普通房间",
            RoomType.Goal => "终点房间",
            RoomType.Treasure => "宝藏间",
            RoomType.Shop => "商店",
            RoomType.Elite => "精英房",
            RoomType.Rest => "休息室",
            RoomType.Event => "事件房",
            RoomType.Boss => "Boss房",
            _ => "未知房间"
        };
    }

    #endregion

    protected override void OnClick(string btnName)
    {
        // 可扩展：快捷键响应等
    }
}
