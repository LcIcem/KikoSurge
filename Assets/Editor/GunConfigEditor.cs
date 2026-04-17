using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GunConfig))]
public class GunConfigEditor : Editor
{
    private SerializedProperty _spId;
    private SerializedProperty _spGunName;
    private SerializedProperty _spGunPrefab;
    private SerializedProperty _spIcon;
    private SerializedProperty _spFireMode;
    private SerializedProperty _spFireRate;
    private SerializedProperty _spRandomSpreadAngle;
    private SerializedProperty _spBulletCount;
    private SerializedProperty _spShotgunSpreadAngle;
    private SerializedProperty _spBurstCount;
    private SerializedProperty _spBurstSpeed;
    private SerializedProperty _spChargeTime;
    private SerializedProperty _spMagazineSize;
    private SerializedProperty _spReloadTime;
    private SerializedProperty _spRecoilForce;
    private SerializedProperty _spWeight;
    private SerializedProperty _spBulletConfig;

    private void OnEnable()
    {
        var so = serializedObject;
        _spId = so.FindProperty("Id");
        _spGunName = so.FindProperty("gunName");
        _spGunPrefab = so.FindProperty("gunPrefab");
        _spIcon = so.FindProperty("icon");
        _spFireMode = so.FindProperty("fireMode");
        _spFireRate = so.FindProperty("fireRate");
        _spRandomSpreadAngle = so.FindProperty("randomSpreadAngle");
        _spBulletCount = so.FindProperty("bulletCount");
        _spShotgunSpreadAngle = so.FindProperty("shotgunSpreadAngle");
        _spBurstCount = so.FindProperty("burstCount");
        _spBurstSpeed = so.FindProperty("burstSpeed");
        _spChargeTime = so.FindProperty("chargeTime");
        _spMagazineSize = so.FindProperty("magazineSize");
        _spReloadTime = so.FindProperty("reloadTime");
        _spRecoilForce = so.FindProperty("recoilForce");
        _spWeight = so.FindProperty("weight");
        _spBulletConfig = so.FindProperty("bulletConfig");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 基础信息
        EditorGUILayout.LabelField("基础信息");
        EditorGUILayout.PropertyField(_spId);
        EditorGUILayout.PropertyField(_spGunName);
        EditorGUILayout.PropertyField(_spGunPrefab);
        EditorGUILayout.PropertyField(_spIcon);
        EditorGUILayout.Space();

        // 开火模式（始终显示）
        EditorGUILayout.PropertyField(_spFireMode);
        EditorGUILayout.Space();

        // 射速与散布（始终显示）
        EditorGUILayout.LabelField("射速与散布");
        EditorGUILayout.PropertyField(_spFireRate);
        EditorGUILayout.PropertyField(_spRandomSpreadAngle);
        EditorGUILayout.Space();

        // 按 fireMode 显示特有字段
        switch ((FireMode)_spFireMode.enumValueIndex)
        {
            case FireMode.Spread:
                EditorGUILayout.LabelField("霰弹（Spread）");
                EditorGUILayout.PropertyField(_spBulletCount);
                EditorGUILayout.PropertyField(_spShotgunSpreadAngle);
                break;

            case FireMode.Burst:
                EditorGUILayout.LabelField("连发（Burst）");
                EditorGUILayout.PropertyField(_spBurstCount);
                EditorGUILayout.PropertyField(_spBurstSpeed);
                break;

            case FireMode.Charge:
                EditorGUILayout.LabelField("蓄力（Charge）");
                EditorGUILayout.PropertyField(_spChargeTime);
                break;

            case FireMode.Single:
            case FireMode.Continuous:
                // 无特有字段
                break;
        }
        EditorGUILayout.Space();

        // 弹药
        EditorGUILayout.LabelField("弹药");
        EditorGUILayout.PropertyField(_spMagazineSize);
        EditorGUILayout.PropertyField(_spReloadTime);
        EditorGUILayout.Space();

        // 后坐力
        EditorGUILayout.LabelField("后坐力");
        EditorGUILayout.PropertyField(_spRecoilForce);
        EditorGUILayout.Space();

        // 随机权重
        EditorGUILayout.LabelField("随机权重");
        EditorGUILayout.PropertyField(_spWeight);
        EditorGUILayout.Space();

        // 子弹配置
        EditorGUILayout.PropertyField(_spBulletConfig);

        serializedObject.ApplyModifiedProperties();
    }
}
