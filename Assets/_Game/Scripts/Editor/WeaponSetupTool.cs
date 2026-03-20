using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using TowerBreakers.Player.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace TowerBreakers.Editor
{
    public class WeaponSetupTool : EditorWindow
    {
        private const string SAVE_PATH = "Assets/_Game/Data/Equipment";
        private const string SPUM_ROOT = "Assets/SPUM/Resources/Addons";

        [MenuItem("Tools/TowerBreakers/Weapon Setup Tool")]
        public static void ShowWindow() => GetWindow<WeaponSetupTool>("Weapon Setup");

        private void OnGUI()
        {
            GUILayout.Label("SPUM Asset Auto Creator (Multi-Part Support)", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Create & Sync All Equipment"))
            {
                ExecuteFullSync();
            }
        }

        private void ExecuteFullSync()
        {
            if (!Directory.Exists(SAVE_PATH)) Directory.CreateDirectory(SAVE_PATH);

            var db = GetDatabase();
            if (db == null) return;

            // 모든 SPUM 리소스 사전 캐싱
            var allPngs = AssetDatabase.FindAssets("t:Sprite", new[] { SPUM_ROOT })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.EndsWith(".png"))
                .ToList();

            SyncCategory(allPngs, "7_Armor", EquipmentType.Armor, db);
            SyncCategory(allPngs, "8_Weapons", EquipmentType.Weapon, db);
            SyncCategory(allPngs, "4_Helmet", EquipmentType.Helmet, db);

            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Full Sync Complete!");
        }

        private void SyncCategory(List<string> allPngs, string folderKeyword, EquipmentType type, EquipmentDatabase db)
        {
            var targetFiles = allPngs.Where(p => p.Contains(folderKeyword)).ToList();

            foreach (var file in targetFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                string assetPath = $"{SAVE_PATH}/{fileName}.asset";
                
                EquipmentData data = AssetDatabase.LoadAssetAtPath<EquipmentData>(assetPath);
                if (data == null)
                {
                    data = CreateInstance<EquipmentData>();
                    AssetDatabase.CreateAsset(data, assetPath);
                }

                UpdateAssetData(data, fileName, type, file, allPngs);
                RegisterToDatabase(db, data, type);
                
                EditorUtility.SetDirty(data);
            }
        }

        private void UpdateAssetData(EquipmentData data, string name, EquipmentType type, string spritePath, List<string> allPngs)
        {
            var so = new SerializedObject(data);
            so.FindProperty("m_id").stringValue = name;
            string cleanName = name.Replace("New_", "").Replace("Weapon_", "Weapon ").Replace("Armor_", "Armor ").Replace("Helmet_", "Helmet ");
            so.FindProperty("m_itemName").stringValue = cleanName;
            so.FindProperty("m_type").enumValueIndex = (int)type;
            so.FindProperty("m_icon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

            var partsProp = so.FindProperty("m_spumParts");
            partsProp.ClearArray();

            // 1. 기본 부위 추가
            string category = Path.GetFileName(Path.GetDirectoryName(spritePath));
            AddPart(partsProp, category, spritePath);

            // 2. 갑옷인 경우 어깨 부위 자동 검색 (인덱스 매칭)
            if (type == EquipmentType.Armor)
            {
                string index = ExtractIndex(name);
                if (!string.IsNullOrEmpty(index))
                {
                    // 어깨 부위 키워드: Shoulder, L_Shoulder, R_Shoulder 등
                    var shoulders = allPngs.Where(p => (p.Contains("Shoulder") || p.Contains("Arm")) && p.Contains(index)).ToList();
                    foreach (var sPath in shoulders)
                    {
                        string sCat = Path.GetFileName(Path.GetDirectoryName(sPath));
                        // 중복 방지
                        bool alreadyAdded = false;
                        for (int i = 0; i < partsProp.arraySize; i++)
                        {
                            if (partsProp.GetArrayElementAtIndex(i).FindPropertyRelative("SpritePath").stringValue == sPath)
                            {
                                alreadyAdded = true;
                                break;
                            }
                        }
                        if (!alreadyAdded) AddPart(partsProp, sCat, sPath);
                    }
                }
            }

            if (type == EquipmentType.Weapon)
            {
                WeaponType wType = WeaponType.None;
                if (category.Contains("Sword")) wType = WeaponType.Sword;
                else if (category.Contains("Axe")) wType = WeaponType.Axe;
                else if (category.Contains("Bow")) wType = WeaponType.Bow;
                so.FindProperty("m_weaponType").enumValueIndex = (int)wType;
            }
            
            so.ApplyModifiedProperties();
        }

        private void AddPart(SerializedProperty list, string structure, string path)
        {
            list.InsertArrayElementAtIndex(list.arraySize);
            var element = list.GetArrayElementAtIndex(list.arraySize - 1);
            element.FindPropertyRelative("Structure").stringValue = structure;
            element.FindPropertyRelative("SpritePath").stringValue = path;
            element.FindPropertyRelative("Sprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private string ExtractIndex(string name)
        {
            Match match = Regex.Match(name, @"\d+$");
            return match.Success ? match.Value : "";
        }

        private void RegisterToDatabase(EquipmentDatabase db, EquipmentData data, EquipmentType type)
        {
            var so = new SerializedObject(db);
            string propName = type == EquipmentType.Weapon ? "m_weapons" : (type == EquipmentType.Armor ? "m_armors" : "m_helmets");
            var listProp = so.FindProperty(propName);

            bool exists = false;
            for (int i = 0; i < listProp.arraySize; i++)
            {
                if (listProp.GetArrayElementAtIndex(i).objectReferenceValue == data) { exists = true; break; }
            }

            if (!exists)
            {
                listProp.InsertArrayElementAtIndex(listProp.arraySize);
                listProp.GetArrayElementAtIndex(listProp.arraySize - 1).objectReferenceValue = data;
            }
            so.ApplyModifiedProperties();
        }

        private EquipmentDatabase GetDatabase()
        {
            string[] guids = AssetDatabase.FindAssets("t:EquipmentDatabase");
            return guids.Length > 0 ? AssetDatabase.LoadAssetAtPath<EquipmentDatabase>(AssetDatabase.GUIDToAssetPath(guids[0])) : null;
        }
    }
}