using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Boo.Lang.Runtime;
using UnityEngine;
using UnityEngine.Assertions;

namespace Nova
{
	#region SerializeUtil

	[Serializable]
	public class SerializableHashSet<T> : HashSet<T>
	{
		public SerializableHashSet() { }

		protected SerializableHashSet(SerializationInfo info, StreamingContext context)
		{
			foreach (var val in (List<T>) info.GetValue("values", typeof(List<T>)))
				Add(val);
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("values", this.ToList());
		}
	}

	#endregion

	#region SaveTypes

	[Serializable]
	public class StringHashSet : SerializableHashSet<string>
	{
		public StringHashSet() { }

		protected StringHashSet(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}

	[Serializable]
	public class NodeSaveInfo
	{
		public readonly Dictionary<int, GameStateStepRestoreEntry> DialogueRestoreEntries;
		public readonly StringHashSet ReachedBranches;

		public NodeSaveInfo()
		{
			DialogueRestoreEntries = new Dictionary<int, GameStateStepRestoreEntry>();
			ReachedBranches = new StringHashSet();
		}
	}

	[Serializable]
	public class GlobalSave
	{
		public readonly Dictionary<string, NodeSaveInfo> SavedNodes = new Dictionary<string, NodeSaveInfo>();
		public readonly StringHashSet ReachedEndings = new StringHashSet();
		public readonly long GlobalSaveIdentifier = DateTime.Now.ToBinary();
	}

	[Serializable]
	public class Bookmark
	{
		public readonly string NodeName;
		public readonly int DialogueIndex;
		public long GlobalSaveIdentifier;

		public Bookmark(string nodeName, int dialogueIndex)
		{
			NodeName = nodeName;
			DialogueIndex = dialogueIndex;
		}
	}

	#endregion

	public class CheckpointManager : MonoBehaviour
	{
		private const int Version = 1;

		private string _savePathBase;
		private string _globalSavePath;
		private byte[] _fileHeader;

		private DESCryptoServiceProvider _cryptic;
		private BinaryFormatter _formatter;
		
		private GlobalSave _globalSave;


		public HashSet<int> UsedSaveSlots { get; private set; }

		public void InitVariables()
		{
			_savePathBase = Application.persistentDataPath + "/Save/";
			_globalSavePath = _savePathBase + "global.nsav";
			_fileHeader = Encoding.ASCII.GetBytes("NOVASAVE");
			_cryptic = new DESCryptoServiceProvider()
			{
				Key = Encoding.ASCII.GetBytes("NovaSave"),
				IV = Encoding.ASCII.GetBytes("novasave")
			};
			_formatter = new BinaryFormatter();
			UsedSaveSlots = new HashSet<int>();
		}

		public void Awake()
		{
			InitVariables();

			Directory.CreateDirectory(_savePathBase);
			if (File.Exists(_globalSavePath))
				using (var fs = File.OpenRead(_globalSavePath))
					_globalSave = ReadSave<GlobalSave>(fs);
			else
				ResetGlobalSave();

			var regex = new Regex("sav([0-9]+).nsav");
			foreach (var name in Directory.GetFiles(_savePathBase, "sav*.nsav"))
			{
				var result = regex.Match(name);
				if (result.Groups.Count > 1)
				{
					int id;
					if (int.TryParse(result.Groups[1].Value, out id))
						UsedSaveSlots.Add(id);
				}
			}
			Debug.Log("CheckpointManager Initialized");
		}

		private NodeSaveInfo EnsureSavedNode(string nodeName)
		{
			NodeSaveInfo info;
			if (!_globalSave.SavedNodes.TryGetValue(nodeName, out info))
				_globalSave.SavedNodes[nodeName] = new NodeSaveInfo();
			return info;
		}

		public void SetReached(string nodeName, int dialogueIndex, GameStateStepRestoreEntry entry)
		{
			EnsureSavedNode(nodeName).DialogueRestoreEntries[dialogueIndex] = entry;
		}

		public void SetReached(string nodeName, string branchName)
		{
			EnsureSavedNode(nodeName).ReachedBranches.Add(branchName);
		}

		public void SetReached(string endName)
		{
			_globalSave.ReachedEndings.Add(endName);
		}

		public GameStateStepRestoreEntry IsReached(string nodeName, int dialogueIndex)
		{
			NodeSaveInfo info;
			GameStateStepRestoreEntry entry;
			if (_globalSave.SavedNodes.TryGetValue(nodeName, out info))
				if (info.DialogueRestoreEntries.TryGetValue(dialogueIndex, out entry))
					return entry;
			return null;
		}

		public bool IsReached(string nodeName, string branchName)
		{
			NodeSaveInfo info;
			if (_globalSave.SavedNodes.TryGetValue(nodeName, out info))
				return info.ReachedBranches.Contains(branchName);
			return false;
		}

		public bool IsReached(string endName)
		{
			return _globalSave.ReachedEndings.Contains(endName);
		}

		private string ComposeFileName(int saveId)
		{
			return string.Format("{0}sav{1:D3}.nsav", _savePathBase, saveId);
		}

		public void ResetGlobalSave()
		{
			using (var fs = File.OpenWrite(_globalSavePath))
				WriteSave(_globalSave = new GlobalSave(), fs);
		}

		private void WriteSave<T>(T obj, Stream s)
		{
			var bw = new BinaryWriter(s);
			bw.Write(_fileHeader);
			bw.Write(Version);
			using (var stream = new CryptoStream(s, _cryptic.CreateEncryptor(), CryptoStreamMode.Write))
				_formatter.Serialize(stream, obj);
		}

		private T ReadSave<T>(Stream s)
		{
			var bw = new BinaryReader(s);
			Assert.IsTrue(_fileHeader.SequenceEqual(bw.ReadBytes(_fileHeader.Length)), "Nova: Invalid save file format");
			Assert.IsTrue(Version >= bw.ReadInt32(), "Nova: Save file is incompatible with the current version of engine");
			using (var stream = new CryptoStream(s, _cryptic.CreateDecryptor(), CryptoStreamMode.Read))
				return (T) _formatter.Deserialize(stream);
		}

		public void SaveBookmark(int saveId, Bookmark save)
		{
			using (var fs = File.OpenWrite(ComposeFileName(saveId)))
			{
				save.GlobalSaveIdentifier = _globalSave.GlobalSaveIdentifier;
				WriteSave(save, fs);
			}
			using (var fs = File.OpenWrite(_globalSavePath))
				WriteSave(_globalSave, fs);
		}

		public Bookmark LoadBookmark(int saveId)
		{
			using (var fs = File.OpenRead(ComposeFileName(saveId)))
			{
				Bookmark result = ReadSave<Bookmark>(fs);
				if (result.GlobalSaveIdentifier != _globalSave.GlobalSaveIdentifier)
					throw new RuntimeException("Nova: Save file is incompatible with the global save file");
				return result;
			}
		}

		public void DeleteBookmark(int saveId)
		{
			File.Delete(ComposeFileName(saveId));
		}
	}
}
