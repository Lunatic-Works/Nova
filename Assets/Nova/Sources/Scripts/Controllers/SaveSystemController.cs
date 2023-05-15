using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace Nova
{
    public class SaveSystemController : MonoBehaviour
    {
        private GameState gameState;
        private CheckpointManager checkpointManager;
        private ConcurrentDictionary<string, int> saveNameToIdMap; //用Map来处理ID与Name的关系，避免破坏现有的ID系统。

        private const string DateTimeFormat = "yyyy/MM/dd  HH:mm";
        public const int StartPos = 1000; //已将存档位数扩展到9999，1000-9999为程序内部存档范畴，不应该在UI访问。
        public const int EndPos = 9999;
        
        private void Awake()
        {
            gameState = Utils.FindNovaController().GameState;
            checkpointManager = Utils.FindNovaController().CheckpointManager;
            gameObject.AddComponent<SaveSystemLuaBind>();

            //开始构建 Name-ID Map
            saveNameToIdMap = new ConcurrentDictionary<string, int>();
            foreach (var tmp in checkpointManager.saveSlotsMetadata)
            {
                var id = tmp.Key;
                var metaName = tmp.Value.name;
                if (metaName != "") 
                {
                    saveNameToIdMap.AddOrUpdate(metaName, id, (key, oldValue) => id);
                }
            }
            
            Debug.Log("这里是SaveSystem被成功加载");
        }

        public void SaveBookmark(int id, DialogueDisplayData desc = null, Texture2D screenshot = null, string metaName = "")
        {
            var bookmark = gameState.GetBookmark();
            bookmark.description = desc;
            bookmark.screenshot = screenshot;
            checkpointManager.SaveBookmark(id, bookmark);
            if (metaName == "")
            {
                return;
            }

            checkpointManager.saveSlotsMetadata[id].name = metaName;
            BindIDToName(id, metaName);
        }

        public Bookmark LoadBookmark(int id)
        {
            var result = checkpointManager.LoadBookmark(id);
            return result;
        }
        
        public Bookmark LoadBookmark(string metaName)
        {
            if (!saveNameToIdMap.TryGetValue(metaName, out var id))
            {
                Debug.Log("SaveSystemController: 未找到对应的存档名绑定关系");
                return null;
            }
            
            var result = checkpointManager.LoadBookmark(id);
            return result;
        }
        
        public void DeleteBookmark(int id)
        {
            checkpointManager.DeleteBookmark(id);
        }
        
        public void DeleteBookmark(string metaName)
        {
            if (!saveNameToIdMap.TryGetValue(metaName, out var id))
            {
                Debug.Log("SaveSystemController: 未找到对应的存档名绑定关系");
                return;
            }
            
            checkpointManager.DeleteBookmark(id);
        }

        public int QueryIDByName(string saveName)
        {
            if (saveName == "")
            {
                return -1;
            }
            
            var id = saveNameToIdMap.TryGetValue(saveName, out var result) ? result : -1;
            return id;
        }

        private void BindIDToName(int saveID, string saveName)
        {
            saveNameToIdMap.AddOrUpdate(saveName, saveID, (key, oldValue) => saveID);
        }
    }

    [ExportCustomType]
    public class SaveSystemLuaBind : MonoBehaviour
    {
        private SaveSystemController saveSystemController;
        private GameState gameState;
        private CheckpointManager checkpointManager;
        
        private void Awake()
        {
            saveSystemController = Utils.FindNovaController().SaveSystemController;
            gameState = Utils.FindNovaController().GameState;
            checkpointManager = Utils.FindNovaController().CheckpointManager;
            
            LuaRuntime.Instance.BindObject("slSystem", this, "_G");
        }

        public void SaveBookmark(string saveName = "")
        {
            var tmp = saveSystemController.QueryIDByName(saveName);
            var id = checkpointManager.QueryMinUnusedSaveID(SaveSystemController.StartPos, SaveSystemController.EndPos);
            if (tmp != -1)
            {
                id = tmp;
            }
            
            var dialogue = new DialogueDisplayData(null, null);
            var screenshot = new Texture2D(1, 1);
            saveSystemController.SaveBookmark(id, dialogue, screenshot, saveName);
        }
        
        public void LoadBookmark(string saveName = "")
        {
            if (saveName == "")
            {
                return;
            }
            
            var id = saveSystemController.QueryIDByName(saveName);
            if (id != -1)
            {
                var tmp = saveSystemController.LoadBookmark(id);
                gameState.LoadBookmark(tmp);
            }
            else
            {
                Debug.Log("SaveSystemLuaBind: 未找到对应的存档名绑定关系");
            }
        }
        
        public void DeleteBookmark(string saveName)
        {
            if (saveName == "")
            {
                return;
            }
            
            var id = saveSystemController.QueryIDByName(saveName);
            if (id != -1)
            {
                saveSystemController.DeleteBookmark(id);
            }
        }
    }
}