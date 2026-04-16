using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GunConfig))]
public class GunConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var config = (GunConfig)target;

        // 基础信息
        EditorGUILayout.LabelField("基础信息");
        config.Id = EditorGUILayout.IntField("Id", config.Id);
        config.gunName = EditorGUILayout.TextField("武器名称", config.gunName);
        config.gunPrefab = (GameObject)EditorGUILayout.ObjectField("武器预设体", config.gunPrefab, typeof(GameObject), false);
        config.icon = (Sprite)EditorGUILayout.ObjectField("图标", config.icon, typeof(Sprite), false);
        EditorGUILayout.Space();

        // 开火模式（始终显示）
        config.fireMode = (FireMode)EditorGUILayout.EnumPopup("开火模式", config.fireMode);
        EditorGUILayout.Space();

        // 射速与散布（始终显示）
        EditorGUILayout.LabelField("射速与散布");
        config.fireRate = EditorGUILayout.FloatField("Fire Rate", config.fireRate);
        config.randomSpreadAngle = EditorGUILayout.FloatField("Random Spread Angle", config.randomSpreadAngle);
        EditorGUILayout.Space();

        // 按 fireMode 显示特有字段
        switch (config.fireMode)
        {
            case FireMode.Spread:
                EditorGUILayout.LabelField("霰弹（Spread）");
                config.bulletCount = EditorGUILayout.IntField("Bullet Count", config.bulletCount);
                config.shotgunSpreadAngle = EditorGUILayout.FloatField("Shotgun Spread Angle", config.shotgunSpreadAngle);
                break;

            case FireMode.Burst:
                EditorGUILayout.LabelField("连发（Burst）");
                config.burstCount = EditorGUILayout.IntField("Burst Count", config.burstCount);
                config.burstSpeed = EditorGUILayout.FloatField("Burst Speed", config.burstSpeed);
                break;

            case FireMode.Charge:
                EditorGUILayout.LabelField("蓄力（Charge）");
                config.chargeTime = EditorGUILayout.FloatField("Charge Time", config.chargeTime);
                break;

            case FireMode.Single:
            case FireMode.Continuous:
                // 无特有字段
                break;
        }
        EditorGUILayout.Space();

        // 弹药
        EditorGUILayout.LabelField("弹药");
        config.magazineSize = EditorGUILayout.IntField("Magazine Size", config.magazineSize);
        config.reloadTime = EditorGUILayout.FloatField("Reload Time", config.reloadTime);
        EditorGUILayout.Space();

        // 后坐力
        EditorGUILayout.LabelField("后坐力");
        config.recoilForce = EditorGUILayout.FloatField("Recoil Force", config.recoilForce);
        EditorGUILayout.Space();

        // 随机权重
        EditorGUILayout.LabelField("随机权重");
        config.weight = EditorGUILayout.IntField("Weight", config.weight);
        EditorGUILayout.Space();

        // 子弹配置
        config.bulletConfig = (BulletConfig)EditorGUILayout.ObjectField("子弹配置", config.bulletConfig, typeof(BulletConfig), false);

        serializedObject.ApplyModifiedProperties();
    }
}
