using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ProcGen.Config;
using ProcGen.Core;

namespace ProcGen.Editor
{
    /// <summary>地牢配置编辑器窗口
    /// 在单一面板中管理 DungeonModel_SO 和 RoomTemplateConfig_SO 的创建与配置
    /// </summary>
    public class DungeonConfigEditorWindow : EditorWindow
    {
        // ==================== 静态入口 ====================

        [MenuItem("KikoSurge/Dungeon/地牢配置编辑器")]
        public static void Open()
        {
            var window = GetWindow<DungeonConfigEditorWindow>("地牢配置");
            window.minSize = new Vector2(700, 500);
        }

        // ==================== 视图模式 ====================

        private enum ViewMode
        {
            DungeonModel,
            RoomTemplate
        }

        private ViewMode _currentMode = ViewMode.DungeonModel;

        // ==================== 数据列表 ====================

        private List<DungeonModel_SO> _dungeonModels;
        private List<RoomTemplateConfig_SO> _roomTemplates;
        private DungeonModel_SO _selectedDungeonModel;
        private RoomTemplateConfig_SO _selectedRoomTemplate;

        // ==================== SerializedObject 缓存（避免每次访问重新创建实例）====================

        private SerializedObject _dungeonModelSO;

        // ==================== 滚动区域 ====================

        private Vector2 _leftScroll;
        private Vector2 _rightScroll;

        // ==================== Foldout 状态 ====================

        private List<bool> _templateFoldouts = new List<bool>();

        // ==================== 延迟操作 ====================

        private int _pendingRemoveIndex = -1;  // 避免在 GUI 回调中途修改列表导致布局状态错乱
        private System.Action _pendingAction;   // 延迟到下一帧执行的操作（避免修改列表导致布局 Begin/End 错乱）

        // ==================== Unity 事件 ====================

        private void OnEnable()
        {
            RefreshAssets();
            _dungeonModelSO = null;
            _pendingRemoveIndex = -1;
            _pendingAction = null;
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space(5);
            DrawContent();
        }

        // ==================== 工具栏 ====================

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();

            EditorGUI.BeginChangeCheck();
            _currentMode = (ViewMode)GUILayout.Toolbar((int)_currentMode,
                new[] { "地牢配置 (DungeonModel)", "房间模板 (RoomTemplate)" },
                GUILayout.Width(350));
            bool modeChanged = EditorGUI.EndChangeCheck();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (modeChanged)
            {
                _templateFoldouts.Clear();
                _dungeonModelSO = null;
                _pendingAction = null;
                _pendingRemoveIndex = -1;
            }
        }

        // ==================== 主内容区 ====================

        private void DrawContent()
        {
            EditorGUILayout.BeginHorizontal();

            // 左侧：资源列表
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(220), GUILayout.ExpandHeight(true));
            DrawLeftPanel();
            EditorGUILayout.EndVertical();

            // 右侧：配置详情
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            DrawRightPanel();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        // ==================== 左侧：资源列表 ====================

        private void DrawLeftPanel()
        {
            EditorGUILayout.LabelField("资源列表", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);

            // 创建按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ 新建", GUILayout.Height(22)))
            {
                if (_currentMode == ViewMode.DungeonModel)
                    CreateDungeonModel();
                else
                    CreateRoomTemplate();
            }
            if (GUILayout.Button("↺", GUILayout.Width(25), GUILayout.Height(22)))
            {
                RefreshAssets();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 资源列表
            _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll);

            if (_currentMode == ViewMode.DungeonModel)
            {
                DrawDungeonModelList();
            }
            else
            {
                DrawRoomTemplateList();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawDungeonModelList()
        {
            if (_dungeonModels == null || _dungeonModels.Count == 0)
            {
                EditorGUILayout.HelpBox("尚未创建任何 DungeonModel。", MessageType.Info);
                return;
            }

            foreach (var model in _dungeonModels)
            {
                if (model == null) continue;

                bool isSelected = model == _selectedDungeonModel;
                string label = string.IsNullOrEmpty(model.name) ? "(未命名)" : model.name;
                label = $"  {label}";

                bool selected = GUILayout.Toggle(isSelected, label,
                    isSelected ? EditorStyles.foldoutHeader : EditorStyles.label,
                    GUILayout.Height(20));

                if (selected && !isSelected)
                {
                    _selectedDungeonModel = model;
                    Selection.activeObject = model;
                }
            }
        }

        private void DrawRoomTemplateList()
        {
            if (_roomTemplates == null || _roomTemplates.Count == 0)
            {
                EditorGUILayout.HelpBox("尚未创建任何 RoomTemplate。", MessageType.Info);
                return;
            }

            foreach (var template in _roomTemplates)
            {
                if (template == null) continue;

                bool isSelected = template == _selectedRoomTemplate;
                string label = string.IsNullOrEmpty(template.name) ? "(未命名)" : template.name;
                label = $"  {label}";

                bool selected = GUILayout.Toggle(isSelected, label,
                    isSelected ? EditorStyles.foldoutHeader : EditorStyles.label,
                    GUILayout.Height(20));

                if (selected && !isSelected)
                {
                    _selectedRoomTemplate = template;
                    Selection.activeObject = template;
                    _templateFoldouts.Clear();
                    if (template.templates != null)
                        _templateFoldouts.AddRange(new bool[template.templates.Count]);
                }
            }
        }

        // ==================== 右侧：配置详情 ====================

        private void DrawRightPanel()
        {
            _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll);

            if (_currentMode == ViewMode.DungeonModel)
                DrawDungeonModelDetail();
            else
                DrawRoomTemplateDetail();

            EditorGUILayout.EndScrollView();
        }

        // ==================== DungeonModel 详情 ====================

        private void DrawDungeonModelDetail()
        {
            if (_selectedDungeonModel == null)
            {
                EditorGUILayout.HelpBox("从左侧选择一个 DungeonModel 进行编辑，或点击 \"+ 新建\" 创建。", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"编辑：{_selectedDungeonModel.name}", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            Undo.RecordObject(_selectedDungeonModel, "Edit DungeonModel");
            _dungeonModelSO = new SerializedObject(_selectedDungeonModel);

            // 地图尺寸
            DrawSectionHeader("地图尺寸", () =>
            {
                _selectedDungeonModel.mapWidth = EditorGUILayout.IntField("地图宽度（格）", _selectedDungeonModel.mapWidth);
                _selectedDungeonModel.mapHeight = EditorGUILayout.IntField("地图高度（格）", _selectedDungeonModel.mapHeight);
                _selectedDungeonModel.borderSize = EditorGUILayout.IntField("边界留空（格）", _selectedDungeonModel.borderSize);
            });

            // 房间模板引用
            DrawSectionHeader("房间模板配置", () =>
            {
                EditorGUILayout.PropertyField(_dungeonModelSO.FindProperty("roomTemplateConfig"), true);
            });

            // 走廊
            DrawSectionHeader("走廊", () =>
            {
                _selectedDungeonModel.corridorWidth = EditorGUILayout.IntField("走廊宽度（格）", _selectedDungeonModel.corridorWidth);
            });

            // 普通房间
            DrawSectionHeader("普通房间", () =>
            {
                _selectedDungeonModel.normalRoomCount = EditorGUILayout.IntField("保证数量", _selectedDungeonModel.normalRoomCount);
                DrawExtraChanceField(_dungeonModelSO.FindProperty("normalExtraChance"));
            });

            // 特殊房间（批量）
            DrawSectionHeader("特殊房间", () =>
            {
                DrawSpecialRoomField("精英房（Elite）", _dungeonModelSO.FindProperty("eliteRoomCount"), _dungeonModelSO.FindProperty("eliteExtraChance"));
                DrawSpecialRoomField("宝藏间（Treasure）", _dungeonModelSO.FindProperty("treasureRoomCount"), _dungeonModelSO.FindProperty("treasureExtraChance"));
                DrawSpecialRoomField("商店（Shop）", _dungeonModelSO.FindProperty("shopRoomCount"), _dungeonModelSO.FindProperty("shopExtraChance"));
                DrawSpecialRoomField("休息室（Rest）", _dungeonModelSO.FindProperty("restRoomCount"), _dungeonModelSO.FindProperty("restExtraChance"));
                DrawSpecialRoomField("事件房（Event）", _dungeonModelSO.FindProperty("eventRoomCount"), _dungeonModelSO.FindProperty("eventExtraChance"));
                DrawSpecialRoomField("Boss房（Boss）", _dungeonModelSO.FindProperty("bossRoomCount"), _dungeonModelSO.FindProperty("bossExtraChance"));
            });

            _dungeonModelSO.ApplyModifiedProperties();

            EditorGUILayout.Space(10);
            DrawAssetButtons(_selectedDungeonModel);
        }

        private void DrawSpecialRoomField(string label, SerializedProperty countProp, SerializedProperty chanceProp)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(130));

            EditorGUILayout.PropertyField(countProp, GUIContent.none, GUILayout.Width(60));
            EditorGUILayout.LabelField("个", GUILayout.Width(15));
            EditorGUILayout.PropertyField(chanceProp, GUIContent.none, GUILayout.Width(50));
            EditorGUILayout.LabelField("%", GUILayout.Width(15));
            EditorGUILayout.LabelField($"额外概率", GUILayout.Width(55));

            EditorGUILayout.EndHorizontal();
        }

        private void DrawExtraChanceField(SerializedProperty prop)
        {
            EditorGUILayout.PropertyField(prop, true);
        }

        // ==================== RoomTemplate 详情 ====================

        private void DrawRoomTemplateDetail()
        {
            if (_selectedRoomTemplate == null)
            {
                EditorGUILayout.HelpBox("从左侧选择一个 RoomTemplate 进行编辑，或点击 \"+ 新建\" 创建。", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"编辑：{_selectedRoomTemplate.name}", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            Undo.RecordObject(_selectedRoomTemplate, "Edit RoomTemplate");

            // 模板列表
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("房间模板列表", EditorStyles.boldLabel, GUILayout.Width(120));
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("+ 添加模板", GUILayout.Height(20), GUILayout.Width(100)))
            {
                AddTemplate();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);

            if (_selectedRoomTemplate.templates == null)
                _selectedRoomTemplate.templates = new List<RoomConfigData>();

            // 确保 foldout 状态数量一致
            while (_templateFoldouts.Count < _selectedRoomTemplate.templates.Count)
                _templateFoldouts.Add(false);
            while (_templateFoldouts.Count > _selectedRoomTemplate.templates.Count)
                _templateFoldouts.RemoveAt(_templateFoldouts.Count - 1);

            for (int i = 0; i < _selectedRoomTemplate.templates.Count; i++)
            {
                DrawTemplateItem(i);
            }

            // 延迟删除：使用 delayCall 推迟到下一帧执行，避免修改列表导致当前帧布局 Begin/End 错乱
            if (_pendingRemoveIndex >= 0)
            {
                int toRemove = _pendingRemoveIndex;
                _pendingRemoveIndex = -1;
                _pendingAction = () =>
                {
                    if (toRemove < _selectedRoomTemplate.templates.Count)
                    {
                        Undo.RecordObject(_selectedRoomTemplate, "Remove Template");
                        _selectedRoomTemplate.templates.RemoveAt(toRemove);
                        _templateFoldouts.RemoveAt(toRemove);
                    }
                    _pendingAction = null;
                };
                EditorApplication.delayCall += () =>
                {
                    _pendingAction?.Invoke();
                };
            }

            // 批量工具
            if (_selectedRoomTemplate.templates.Count > 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("全部折叠", GUILayout.Width(90)))
                {
                    for (int i = 0; i < _templateFoldouts.Count; i++)
                        _templateFoldouts[i] = false;
                }
                if (GUILayout.Button("全部展开", GUILayout.Width(90)))
                {
                    for (int i = 0; i < _templateFoldouts.Count; i++)
                        _templateFoldouts[i] = true;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(10);
            DrawAssetButtons(_selectedRoomTemplate);
        }

        private void DrawTemplateItem(int index)
        {
            if (index >= _selectedRoomTemplate.templates.Count) return;

            EditorGUILayout.BeginVertical(GUI.skin.box);

            var data = _selectedRoomTemplate.templates[index];

            // Foldout 标题栏
            EditorGUILayout.BeginHorizontal();
            _templateFoldouts[index] = EditorGUILayout.Foldout(_templateFoldouts[index],
                $"[{index}] {data.displayName} — {data.roomType}", true);

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("×", GUILayout.Width(20), GUILayout.Height(16)))
            {
                // 标记延迟删除（下一帧执行，避免当前帧修改列表导致布局 Begin/End 错乱）
                _pendingRemoveIndex = index;
            }
            EditorGUILayout.EndHorizontal();

            // 展开内容
            if (_templateFoldouts[index])
            {
                EditorGUI.indentLevel++;

                data.displayName = EditorGUILayout.TextField("显示名称", data.displayName);
                data.roomType = (RoomType)EditorGUILayout.EnumPopup("房间类型", data.roomType);

                EditorGUILayout.BeginHorizontal();
                data.minSize = EditorGUILayout.Vector2IntField("最小尺寸", data.minSize);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                data.maxSize = EditorGUILayout.Vector2IntField("最大尺寸", data.maxSize);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("距起点最小距离", GUILayout.Width(130));
                data.minDistFromStart = EditorGUILayout.IntField(data.minDistFromStart, GUILayout.Width(60));
                EditorGUILayout.LabelField("（0=不限制）", GUILayout.Width(80));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("距起点最大距离", GUILayout.Width(130));
                data.maxDistFromStart = EditorGUILayout.IntField(data.maxDistFromStart, GUILayout.Width(60));
                EditorGUILayout.LabelField("（-1=不限制）", GUILayout.Width(80));
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;
            }

            // 写回
            Undo.RecordObject(_selectedRoomTemplate, "Edit Template");
            _selectedRoomTemplate.templates[index] = data;

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private void AddTemplate()
        {
            Undo.RecordObject(_selectedRoomTemplate, "Add Template");
            var newData = new RoomConfigData
            {
                displayName = $"模板_{_selectedRoomTemplate.templates.Count + 1}",
                roomType = RoomType.Normal,
                minSize = new Vector2Int(5, 5),
                maxSize = new Vector2Int(10, 10)
            };
            _selectedRoomTemplate.templates.Add(newData);
            _templateFoldouts.Add(true);
        }

        // ==================== 通用 UI 组件 ====================

        private void DrawSectionHeader(string title, System.Action content)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            content();
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(3);
        }

        private void DrawAssetButtons(Object asset)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("在 Inspector 中打开", GUILayout.Height(22)))
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
            if (GUILayout.Button("定位资源", GUILayout.Height(22), GUILayout.Width(80)))
            {
                EditorGUIUtility.PingObject(asset);
            }
            EditorGUILayout.EndHorizontal();
        }

        // ==================== 资源操作 ====================

        private void RefreshAssets()
        {
            string[] guids;

            guids = AssetDatabase.FindAssets($"t:{nameof(DungeonModel_SO)}");
            _dungeonModels = new List<DungeonModel_SO>();
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var obj = AssetDatabase.LoadAssetAtPath<DungeonModel_SO>(path);
                if (obj != null) _dungeonModels.Add(obj);
            }

            guids = AssetDatabase.FindAssets($"t:{nameof(RoomTemplateConfig_SO)}");
            _roomTemplates = new List<RoomTemplateConfig_SO>();
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var obj = AssetDatabase.LoadAssetAtPath<RoomTemplateConfig_SO>(path);
                if (obj != null) _roomTemplates.Add(obj);
            }

            // 清理空引用
            _dungeonModels.RemoveAll(x => x == null);
            _roomTemplates.RemoveAll(x => x == null);
        }

        private void CreateDungeonModel()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "创建 DungeonModel",
                "DungeonModel_New",
                "asset",
                "选择保存位置"
            );

            if (string.IsNullOrEmpty(path)) return;

            var model = CreateInstance<DungeonModel_SO>();
            AssetDatabase.CreateAsset(model, path);
            AssetDatabase.SaveAssets();

            RefreshAssets();
            _selectedDungeonModel = model;
            Selection.activeObject = model;
        }

        private void CreateRoomTemplate()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "创建 RoomTemplate",
                "RoomTemplate_New",
                "asset",
                "选择保存位置"
            );

            if (string.IsNullOrEmpty(path)) return;

            var template = CreateInstance<RoomTemplateConfig_SO>();
            template.templates = new List<RoomConfigData>();
            AssetDatabase.CreateAsset(template, path);
            AssetDatabase.SaveAssets();

            RefreshAssets();
            _selectedRoomTemplate = template;
            Selection.activeObject = template;
            _templateFoldouts.Clear();
            _templateFoldouts.Add(true);
        }

    }
}
