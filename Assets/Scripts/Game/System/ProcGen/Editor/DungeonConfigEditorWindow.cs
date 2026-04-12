using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ProcGen.Config;
using ProcGen.Core;
using UnityEngine.Tilemaps;

namespace ProcGen.Editor
{
    /// <summary>
    /// 地牢配置编辑器（统一视图）
    /// 支持拖入多个配置文件并持久化保存，下次打开无需重新拖入
    /// 菜单入口：Window → KikoSurge → 地牢配置编辑器
    /// </summary>
    public class DungeonConfigEditorWindow : EditorWindow
    {
        [MenuItem("Window/KikoSurge/Dungeon Config Editor")]
        public static void Open() => GetWindow<DungeonConfigEditorWindow>("地牢配置");

        // ==================== 持久化 ====================
        private const string PREF_KEY = "DungeonConfigEditor_AssetPaths";

        // ==================== 状态 ====================
        private List<ScriptableObject> _configs = new List<ScriptableObject>();
        private int _selectedIndex = -1;
        private Vector2 _leftScroll;
        private Vector2 _rightScroll;

        // ==================== 生命周期 ====================
        private void OnEnable() => LoadSavedPaths();

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel();
            DrawVerticalDivider();
            DrawRightPanel();
            EditorGUILayout.EndHorizontal();
        }

        // ==================== 左侧面板 ====================
        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(230), GUILayout.ExpandHeight(true));

            DrawHeader("配置列表");
            GUILayout.Space(2);

            DrawDropArea();
            GUILayout.Space(4);

            // 操作按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ 新建", GUILayout.Height(22)))
                ShowNewAssetMenu();
            if (GUILayout.Button("↺", GUILayout.Width(26), GUILayout.Height(22)))
            { LoadSavedPaths(); Repaint(); }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(6);
            DrawHorizontalLine();

            // 配置列表
            _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll);
            for (int i = 0; i < _configs.Count; i++)
                DrawConfigItem(i);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void DrawConfigItem(int index)
        {
            if (index >= _configs.Count) return;
            var cfg = _configs[index];

            if (cfg == null)
            {
                _configs.RemoveAt(index);
                if (_selectedIndex >= _configs.Count) _selectedIndex = _configs.Count - 1;
                SavePaths();
                return;
            }

            bool isSelected = index == _selectedIndex;
            string typeShort = cfg.GetType().Name.Replace("_SO", "");
            string label = $"  {cfg.name}";

            Color bgOrig = GUI.backgroundColor;
            if (isSelected) GUI.backgroundColor = new Color(0.4f, 0.6f, 1f, 0.3f);

            EditorGUILayout.BeginVertical(Styles.ItemBox);
            GUI.backgroundColor = bgOrig;

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            bool selected = GUILayout.Toggle(isSelected, "", GUILayout.Width(16));
            if (EditorGUI.EndChangeCheck()) _selectedIndex = selected ? index : -1;

            EditorGUILayout.LabelField(label, Styles.ItemLabel, GUILayout.Height(18));
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"[{typeShort}]", Styles.TypeLabel, GUILayout.Height(18));
            EditorGUILayout.EndHorizontal();

            // 右键菜单
            var e = Event.current;
            if (e.type == EventType.ContextClick)
            {
                var rect = GUILayoutUtility.GetLastRect();
                if (rect.Contains(e.mousePosition))
                {
                    e.Use();
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("在 Inspector 中打开"), false, () =>
                    { Selection.activeObject = cfg; EditorGUIUtility.PingObject(cfg); });
                    menu.AddItem(new GUIContent("定位资源"), false, () => EditorGUIUtility.PingObject(cfg));
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("移除"), false, () =>
                    {
                        _configs.RemoveAt(index);
                        if (_selectedIndex >= _configs.Count) _selectedIndex = _configs.Count - 1;
                        SavePaths();
                        Repaint();
                    });
                    menu.ShowAsContext();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDropArea()
        {
            var rect = GUILayoutUtility.GetRect(0, 52, GUILayout.ExpandWidth(true));
            GUI.Box(rect, "将 SO 配置文件拖入此处\n下次打开自动恢复", Styles.DropZone);
            Handles.DrawLine(new Vector2(rect.x + 4, rect.y + 4), new Vector2(rect.xMax - 4, rect.y + 4));

            if (rect.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.DragUpdated)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.DragPerform)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                    Event.current.Use();
                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        if (obj is ScriptableObject so && !_configs.Contains(so))
                        { _configs.Add(so); }
                    }
                    SavePaths();
                    Repaint();
                }
            }
        }

        // ==================== 右侧面板 ====================
        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            if (_selectedIndex < 0 || _selectedIndex >= _configs.Count || _configs[_selectedIndex] == null)
            {
                DrawWelcome();
            }
            else
            {
                DrawConfigDetail(_configs[_selectedIndex]);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawWelcome()
        {
            GUILayout.FlexibleSpace();
            GUILayout.Space(40);

            var rect = GUILayoutUtility.GetRect(0, 100, GUILayout.ExpandWidth(true));
            GUI.Box(rect, "拖入配置文件到左侧列表，或点击「+ 新建」创建", Styles.Welcome);

            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("支持的类型：", Styles.WelcomeSubtext);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("DungeonModel_SO", Styles.WelcomeType);
            GUILayout.Space(10);
            EditorGUILayout.LabelField("RoomTemplateConfig_SO", Styles.WelcomeType);
            GUILayout.Space(10);
            EditorGUILayout.LabelField("TileInfo_SO", Styles.WelcomeType);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(40);
            GUILayout.FlexibleSpace();
        }

        private void DrawConfigDetail(ScriptableObject cfg)
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

            // 标题栏
            EditorGUILayout.BeginHorizontal(Styles.TitleBar);
            EditorGUILayout.LabelField(cfg.name, Styles.TitleLabel);
            EditorGUILayout.LabelField(cfg.GetType().Name, Styles.TitleType);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("定位", GUILayout.Width(60), GUILayout.Height(20)))
                EditorGUIUtility.PingObject(cfg);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(6);

            _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll);

            if (cfg is DungeonModel_SO dm)
                DrawDungeonModel(dm);
            else if (cfg is RoomTemplateConfig_SO rt)
                DrawRoomTemplate(rt);
            else if (cfg is TileInfo_SO ti)
                DrawTileInfo(ti);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ==================== DungeonModel 编辑 ====================
        private SerializedObject _dmSO;

        private void DrawDungeonModel(DungeonModel_SO dm)
        {
            Undo.RecordObject(dm, "Edit DungeonModel");
            _dmSO ??= new SerializedObject(dm);

            DrawGroup("地图尺寸", () =>
            {
                EditorGUILayout.PropertyField(_dmSO.FindProperty("mapWidth"));
                EditorGUILayout.PropertyField(_dmSO.FindProperty("mapHeight"));
                EditorGUILayout.PropertyField(_dmSO.FindProperty("borderSize"));
            });

            DrawGroup("房间模板配置", () =>
            {
                EditorGUILayout.PropertyField(_dmSO.FindProperty("roomTemplateConfig"));
            });

            DrawGroup("走廊", () =>
            {
                EditorGUILayout.PropertyField(_dmSO.FindProperty("corridorWidth"));
                EditorGUILayout.IntSlider(_dmSO.FindProperty("extraCorridorChance"), 0, 100, new GUIContent("额外走廊概率（%）"));
                EditorGUILayout.PropertyField(_dmSO.FindProperty("extraCorridorMaxDistance"), new GUIContent("最大走廊距离（格）"));
            });

            DrawGroup("普通房间", () =>
            {
                EditorGUILayout.PropertyField(_dmSO.FindProperty("normalRoomCount"));
                EditorGUILayout.IntSlider(_dmSO.FindProperty("normalExtraChance"), 0, 100, new GUIContent("额外生成概率（%）"));
            });

            DrawGroup("特殊房间", () =>
            {
                DrawSpecialRoom("精英房", _dmSO.FindProperty("eliteRoomCount"), _dmSO.FindProperty("eliteExtraChance"));
                DrawSpecialRoom("宝藏间", _dmSO.FindProperty("treasureRoomCount"), _dmSO.FindProperty("treasureExtraChance"));
                DrawSpecialRoom("商店", _dmSO.FindProperty("shopRoomCount"), _dmSO.FindProperty("shopExtraChance"));
                DrawSpecialRoom("休息室", _dmSO.FindProperty("restRoomCount"), _dmSO.FindProperty("restExtraChance"));
                DrawSpecialRoom("事件房", _dmSO.FindProperty("eventRoomCount"), _dmSO.FindProperty("eventExtraChance"));
                DrawSpecialRoom("Boss房", _dmSO.FindProperty("bossRoomCount"), _dmSO.FindProperty("bossExtraChance"));
            });

            _dmSO.ApplyModifiedProperties();
        }

        private void DrawSpecialRoom(string label, SerializedProperty countProp, SerializedProperty chanceProp)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(countProp, new GUIContent("保证数量"));
            EditorGUILayout.IntSlider(chanceProp, 0, 100, new GUIContent("额外概率（%）"));
            EditorGUI.indentLevel--;
        }

        // ==================== RoomTemplate 编辑 ====================
        private List<bool> _foldouts = new List<bool>();

        private void DrawRoomTemplate(RoomTemplateConfig_SO rt)
        {
            Undo.RecordObject(rt, "Edit RoomTemplate");
            if (rt.templates == null) rt.templates = new List<RoomConfigData>();

            DrawGroup("", () =>
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"共 {rt.templates.Count} 个模板", GUILayout.Width(120));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("+ 添加", GUILayout.Width(70), GUILayout.Height(18)))
                {
                    Undo.RecordObject(rt, "Add Template");
                    rt.templates.Add(new RoomConfigData
                    {
                        displayName = $"模板_{rt.templates.Count + 1}",
                        roomType = RoomType.Normal,
                        minSize = new Vector2Int(5, 5),
                        maxSize = new Vector2Int(10, 10)
                    });
                    _foldouts.Add(true);
                }
                if (rt.templates.Count > 0)
                {
                    if (GUILayout.Button("全部折叠", GUILayout.Width(70), GUILayout.Height(18)))
                        for (int i = 0; i < _foldouts.Count; i++) _foldouts[i] = false;
                    if (GUILayout.Button("全部展开", GUILayout.Width(70), GUILayout.Height(18)))
                        for (int i = 0; i < _foldouts.Count; i++) _foldouts[i] = true;
                }
                EditorGUILayout.EndHorizontal();
            });

            while (_foldouts.Count < rt.templates.Count) _foldouts.Add(false);
            while (_foldouts.Count > rt.templates.Count) _foldouts.RemoveAt(_foldouts.Count - 1);

            for (int i = 0; i < rt.templates.Count; i++)
                DrawTemplateItem(rt, i);
        }

        private void DrawTemplateItem(RoomTemplateConfig_SO rt, int i)
        {
            if (i >= rt.templates.Count) return;
            var data = rt.templates[i];

            EditorGUILayout.BeginVertical(Styles.ItemBox);
            EditorGUILayout.BeginHorizontal();

            _foldouts[i] = EditorGUILayout.Foldout(_foldouts[i],
                $"[{i}] {data.displayName}  —  {data.roomType}", true);

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("×", GUILayout.Width(20), GUILayout.Height(16)))
            {
                Undo.RecordObject(rt, "Remove Template");
                rt.templates.RemoveAt(i);
                _foldouts.RemoveAt(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            EditorGUILayout.EndHorizontal();

            if (_foldouts[i])
            {
                EditorGUI.indentLevel++;
                data.displayName = EditorGUILayout.TextField("显示名称", data.displayName);
                data.roomType = (RoomType)EditorGUILayout.EnumPopup("房间类型", data.roomType);
                data.minSize = EditorGUILayout.Vector2IntField("最小尺寸", data.minSize);
                data.maxSize = EditorGUILayout.Vector2IntField("最大尺寸", data.maxSize);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("距起点", GUILayout.Width(80));
                data.minDistFromStart = EditorGUILayout.IntField(GUIContent.none, data.minDistFromStart, GUILayout.Width(70));
                EditorGUILayout.LabelField("~", GUILayout.Width(10));
                data.maxDistFromStart = EditorGUILayout.IntField(GUIContent.none, data.maxDistFromStart, GUILayout.Width(70));
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
            }

            rt.templates[i] = data;
            EditorGUILayout.EndVertical();
            GUILayout.Space(2);
        }

        // ==================== TileInfo 编辑 ====================
        private void DrawTileInfo(TileInfo_SO ti)
        {
            Undo.RecordObject(ti, "Edit TileInfo");
            if (ti.tiles == null) ti.tiles = new List<Tile>();

            DrawGroup("", () =>
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"共 {ti.tiles.Count} 个瓦片", GUILayout.Width(120));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("+ 添加", GUILayout.Width(70), GUILayout.Height(18)))
                {
                    Undo.RecordObject(ti, "Add Tile");
                    ti.tiles.Add(new Tile());
                }
                EditorGUILayout.EndHorizontal();
            });

            for (int i = 0; i < ti.tiles.Count; i++)
                DrawTileItem(ti, i);
        }

        private void DrawTileItem(TileInfo_SO ti, int i)
        {
            if (i >= ti.tiles.Count) return;
            var tile = ti.tiles[i];

            EditorGUILayout.BeginVertical(Styles.ItemBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"[{i}]  {tile.type}", Styles.ItemLabel, GUILayout.Height(18));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("×", GUILayout.Width(20), GUILayout.Height(16)))
            {
                Undo.RecordObject(ti, "Remove Tile");
                ti.tiles.RemoveAt(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel++;
            tile.type = (TileType)EditorGUILayout.EnumPopup("类型", tile.type);
            tile.tile = (TileBase)EditorGUILayout.ObjectField("瓦片资源", tile.tile, typeof(TileBase), false);
            EditorGUI.indentLevel--;

            ti.tiles[i] = tile;
            EditorGUILayout.EndVertical();
            GUILayout.Space(2);
        }

        // ==================== 新建菜单 ====================
        private void ShowNewAssetMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("DungeonModel_SO（地牢总配置）"), false, CreateDungeonModel);
            menu.AddItem(new GUIContent("RoomTemplateConfig_SO（房间模板）"), false, CreateRoomTemplate);
            menu.AddItem(new GUIContent("TileInfo_SO（瓦片配置）"), false, CreateTileInfo);
            menu.ShowAsContext();
        }

        private void CreateDungeonModel() => CreateAsset<DungeonModel_SO>("DungeonModel_New", "");
        private void CreateRoomTemplate() => CreateAsset<RoomTemplateConfig_SO>("RoomTemplate_New", "", obj => { ((RoomTemplateConfig_SO)obj).templates = new List<RoomConfigData>(); });
        private void CreateTileInfo() => CreateAsset<TileInfo_SO>("TileInfo_New", "", obj => { ((TileInfo_SO)obj).tiles = new List<Tile>(); });

        private void CreateAsset<T>(string defaultName, string subFolder, Action<ScriptableObject> init = null) where T : ScriptableObject
        {
            string folder = string.IsNullOrEmpty(subFolder) ? "Assets" : $"Assets/{subFolder}";
            string path = EditorUtility.SaveFilePanelInProject(
                $"创建 {typeof(T).Name}", $"{folder}/{defaultName}", "asset", "选择保存位置");
            if (string.IsNullOrEmpty(path)) return;

            var obj = CreateInstance<T>();
            AssetDatabase.CreateAsset(obj, path);
            init?.Invoke(obj);
            EditorUtility.SetDirty(obj);
            AssetDatabase.SaveAssets();

            if (!_configs.Contains(obj)) _configs.Add(obj);
            _selectedIndex = _configs.Count - 1;
            SavePaths();
            Repaint();
        }

        // ==================== 持久化 ====================
        private void SavePaths()
        {
            var paths = new List<string>();
            foreach (var cfg in _configs)
                if (cfg != null) paths.Add(AssetDatabase.GetAssetPath(cfg));
            EditorPrefs.SetString(PREF_KEY, string.Join("\n", paths));
        }

        private void LoadSavedPaths()
        {
            _configs.Clear();
            _selectedIndex = -1;
            string json = EditorPrefs.GetString(PREF_KEY, "");
            if (string.IsNullOrEmpty(json)) return;
            var paths = json.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var path in paths)
            {
                var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path.Trim());
                if (obj != null && !_configs.Contains(obj)) _configs.Add(obj);
            }
            if (_configs.RemoveAll(c => c == null) > 0) SavePaths();
        }

        // ==================== 布局工具 ====================
        private void DrawHeader(string title)
        {
            GUILayout.Space(4);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
        }

        private void DrawVerticalDivider()
        {
            var c = GUI.color;
            GUI.color = new Color(0.35f, 0.35f, 0.35f);
            GUILayout.Box(GUIContent.none, GUILayout.Width(3), GUILayout.ExpandHeight(true));
            GUI.color = c;
        }

        private void DrawHorizontalLine()
        {
            GUILayout.Space(2);
            var c = GUI.color;
            GUI.color = new Color(0.35f, 0.35f, 0.35f);
            GUILayout.Box(GUIContent.none, GUILayout.Height(1), GUILayout.ExpandWidth(true));
            GUI.color = c;
            GUILayout.Space(2);
        }

        private void DrawGroup(string title, Action content)
        {
            bool hasTitle = !string.IsNullOrEmpty(title);
            if (hasTitle)
            {
                EditorGUILayout.BeginVertical(Styles.GroupBox);
                EditorGUILayout.LabelField(title, Styles.GroupLabel);
                EditorGUI.indentLevel++;
            }
            else
            {
                EditorGUILayout.BeginVertical();
            }

            content();

            if (hasTitle)
            {
                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
                GUILayout.Space(4);
            }
            else
            {
                EditorGUILayout.EndVertical();
            }
        }

        // ==================== 样式定义 ====================
        private static class Styles
        {
            private static Texture2D _whiteTex;
            private static Texture2D WhiteTex => _whiteTex ??= Texture2D.whiteTexture;

            private static GUIStyle _headerBar, _titleBar;
            private static GUIStyle _headerLabel, _titleLabel, _titleType;
            private static GUIStyle _groupBox, _groupLabel;
            private static GUIStyle _itemBox, _itemLabel, _typeLabel;
            private static GUIStyle _dropZone, _welcome, _welcomeSubtext, _welcomeType;

            public static GUIStyle HeaderBar => _headerBar ??= new GUIStyle(GUI.skin.box) { padding = new RectOffset(8, 8, 4, 4), stretchWidth = true };
            public static GUIStyle HeaderLabel => _headerLabel ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 13, alignment = TextAnchor.MiddleLeft };

            public static GUIStyle TitleBar => _titleBar ??= new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 8, 8),
                stretchWidth = true,
                normal = { background = MakeTex(32, 32, new Color(0.22f, 0.22f, 0.22f)) }
            };
            public static GUIStyle TitleLabel => _titleLabel ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 15, alignment = TextAnchor.MiddleLeft };
            public static GUIStyle TitleType => _titleType ??= new GUIStyle(EditorStyles.miniLabel) { fontSize = 10, normal = { textColor = Color.gray }, alignment = TextAnchor.MiddleRight };

            public static GUIStyle GroupBox => _groupBox ??= new GUIStyle(GUI.skin.box) { padding = new RectOffset(8, 8, 6, 6) };
            public static GUIStyle GroupLabel => _groupLabel ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 };

            public static GUIStyle ItemBox => _itemBox ??= new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(6, 6, 4, 4),
                margin = new RectOffset(0, 0, 0, 0),
                stretchWidth = true
            };
            public static GUIStyle ItemLabel => _itemLabel ??= new GUIStyle(EditorStyles.label) { fontSize = 11, alignment = TextAnchor.MiddleLeft };
            public static GUIStyle TypeLabel => _typeLabel ??= new GUIStyle(EditorStyles.miniLabel) { fontSize = 9, normal = { textColor = Color.gray }, alignment = TextAnchor.MiddleRight };
            public static GUIStyle HelpLabel => _helpLabel ??= new GUIStyle(EditorStyles.miniLabel) { fontSize = 9, normal = { textColor = Color.gray }, alignment = TextAnchor.MiddleLeft };
            private static GUIStyle _helpLabel;

            public static GUIStyle DropZone => _dropZone ??= new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Italic,
                fontSize = 11,
                normal = { textColor = new Color(0.55f, 0.55f, 0.55f) },
                border = new RectOffset(4, 4, 4, 4)
            };
            public static GUIStyle Welcome => _welcome ??= new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 13,
                normal = { textColor = new Color(0.45f, 0.45f, 0.45f) }
            };
            public static GUIStyle WelcomeSubtext => _welcomeSubtext ??= new GUIStyle(EditorStyles.miniLabel) { fontSize = 10, normal = { textColor = new Color(0.5f, 0.5f, 0.5f) } };
            public static GUIStyle WelcomeType => _welcomeType ??= new GUIStyle(EditorStyles.miniLabel) { fontSize = 10, normal = { textColor = new Color(0.4f, 0.6f, 1f) } };

            private static Texture2D MakeTex(int w, int h, Color c)
            {
                var tex = new Texture2D(w, h);
                for (int x = 0; x < w; x++)
                    for (int y = 0; y < h; y++)
                        tex.SetPixel(x, y, c);
                tex.Apply();
                return tex;
            }
        }
    }
}
