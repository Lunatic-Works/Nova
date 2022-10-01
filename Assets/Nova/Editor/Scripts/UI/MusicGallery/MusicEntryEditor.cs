using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Nova.Editor
{
    [CustomEditor(typeof(MusicEntry))]
    public class MusicEntryEditor : UnityEditor.Editor
    {
        private const string AudioPreviewerName = "_musicEntryPreviewer";
        private GameObject gameObject;
        private AudioLooperOld audioLooper;
        private AudioSource audioSource;
        private int previewSecondsBefore = 3;

        private void OnEnable()
        {
            gameObject = GameObject.Find(AudioPreviewerName);
            if (gameObject == null)
            {
                gameObject = EditorUtility.CreateGameObjectWithHideFlags(
                    AudioPreviewerName, HideFlags.DontSave, typeof(AudioLooperOld)
                );
                var gameObject2 = EditorUtility.CreateGameObjectWithHideFlags(
                    AudioPreviewerName + "2", HideFlags.DontSave, typeof(AudioSource)
                );
                gameObject2.transform.SetParent(gameObject.transform);
            }

            audioLooper = gameObject.GetComponent<AudioLooperOld>();
            audioSource = gameObject.transform.Find(AudioPreviewerName + "2").GetComponent<AudioSource>();
            path = null;
            sampleData = null;
        }

        private void OnDisable()
        {
            DestroyImmediate(gameObject);
        }

        private void Update()
        {
            if (audioLooper.enabled)
            {
                audioLooper.Update();
            }

            Repaint();
        }

        private string path;
        private AudioClip audioClip;
        private float[] sampleData;
        private float[] downSampledData;
        private int downSampleTo;

        private static readonly Color MidYellow = Color.Lerp(Color.clear, Color.yellow, 0.8f);
        private static readonly Color Orange = new Color(1, 0.5f, 0, 1);

        private void DrawCurve(Rect r, Func<float, int> toIndex)
        {
            AudioCurveRendering.DrawSymmetricFilledCurve(
                r,
                (float t, out Color col) =>
                {
                    col = Orange;
                    int i = toIndex(t) / downSampleTo;
                    return i >= downSampledData.Length ? 0 : downSampledData[i];
                }
            );
            AudioCurveRendering.DrawSymmetricFilledCurve(
                r,
                (float t, out Color col) =>
                {
                    col = MidYellow;
                    int i = toIndex(t);
                    return i >= sampleData.Length ? 0 : sampleData[i];
                }
            );
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("id"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("displayNames"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("resourcePath"), true);
            if (serializedObject.targetObject is MusicEntry)
            {
                var musicEntry = serializedObject.targetObject as MusicEntry;
                if (musicEntry.resourcePath != path)
                {
                    path = musicEntry.resourcePath;
                    audioClip = Resources.Load<AudioClip>(path);
                    if (audioClip != null)
                    {
                        sampleData = null;

                        audioLooper.clip = audioClip;
                        audioSource.clip = audioClip;
                    }
                }

                if (sampleData == null && GUILayout.Button("Load waveform (takes a while)"))
                {
                    var assetPath = AssetDatabase.GetAssetPath(audioClip);
                    var audioImporter = AssetImporter.GetAtPath(assetPath) as AudioImporter;
                    var prevSetting = audioImporter.defaultSampleSettings;
                    var newSetting = audioImporter.defaultSampleSettings;
                    audioImporter.forceToMono = true;
                    newSetting.loadType = AudioClipLoadType.DecompressOnLoad;
                    audioImporter.defaultSampleSettings = newSetting;

                    audioImporter.SaveAndReimport();

                    sampleData = new float[audioClip.samples];
                    audioClip.LoadAudioData();
                    audioClip.GetData(sampleData, 0);

                    downSampleTo = audioClip.frequency / 60;
                    downSampledData = new float[(int)Math.Ceiling(1.0f * audioClip.samples / downSampleTo)];
                    for (int i = 0; i < sampleData.Length; i += downSampleTo)
                    {
                        float ssum = 0;
                        int j;
                        for (j = i; j < sampleData.Length && j < i + downSampleTo; j++)
                        {
                            ssum += sampleData[j] * sampleData[j];
                        }

                        float rms = (float)Math.Sqrt(ssum / (j - i));
                        downSampledData[i / downSampleTo] = rms;
                    }

                    audioImporter.forceToMono = false;
                    audioImporter.defaultSampleSettings = prevSetting;

                    audioImporter.SaveAndReimport();
                }

                if (audioClip != null)
                {
                    GUILayout.Label("Loop Begin Sample", EditorStyles.boldLabel);
                    SerializedProperty sp1 = serializedObject.FindProperty("loopBeginSample");
                    sp1.intValue = EditorGUILayout.IntSlider(sp1.intValue, 0, audioClip.samples);
                    sp1.intValue = EditorGUILayout.IntField("fine adjust", sp1.intValue);
                    sp1.intValue = sp1.intValue / 100 * 100 +
                                   EditorGUILayout.IntField("fine adjust mod 100", sp1.intValue % 100);
                    GUILayout.Label("Loop End Sample", EditorStyles.boldLabel);
                    SerializedProperty sp2 = serializedObject.FindProperty("loopEndSample");
                    sp2.intValue = EditorGUILayout.IntSlider(sp2.intValue, 0, audioClip.samples);
                    sp2.intValue = EditorGUILayout.IntField("fine adjust", sp2.intValue);
                    sp2.intValue = sp2.intValue / 100 * 100 +
                                   EditorGUILayout.IntField("fine adjust mod 100", sp2.intValue % 100);
                    GUILayout.Label("Preview Loopback Point", EditorStyles.boldLabel);

                    previewSecondsBefore =
                        EditorGUILayout.IntSlider("Play at ? seconds before loop", previewSecondsBefore, 0, 20);

                    int previewBeforeSample = previewSecondsBefore * audioClip.frequency;
                    int previewBeginSample = musicEntry.loopEndSample - previewBeforeSample;

                    int pos1 = 0;
                    if (audioLooper.currentAudioSource != null)
                    {
                        pos1 = audioLooper.currentAudioSource.timeSamples;
                    }

                    if (sampleData != null)
                    {
                        Rect r = AudioCurveRendering.BeginCurveFrame(GUILayoutUtility.GetRect(1, 10000, 100, 100));
                        DrawCurve(
                            r,
                            t => t < 0.5
                                ? previewBeginSample + (int)(t / 0.5 * previewBeforeSample)
                                : musicEntry.loopBeginSample + (int)((t - 0.5) / 0.5 * previewBeforeSample)
                        );
                        EditorGUI.DrawRect(
                            new Rect(
                                r.x + r.width / 2,
                                r.y,
                                1,
                                r.height
                            ),
                            Color.blue
                        );
                        EditorGUI.DrawRect(
                            new Rect(
                                r.x + (pos1 > previewBeginSample
                                    ? (r.width / 2 * (pos1 - previewBeginSample) / previewBeforeSample)
                                    : (r.width / 2 + r.width / 2 * (pos1 - musicEntry.loopBeginSample) /
                                        previewBeforeSample)),
                                r.y,
                                1,
                                r.height
                            ),
                            Color.red
                        );
                        AudioCurveRendering.EndCurveFrame();
                    }

                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.IntSlider(pos1, 0, audioClip.samples);
                    EditorGUI.EndDisabledGroup();

                    bool btn1 = GUILayout.Button($"Loop, play at loopEndSample - {previewSecondsBefore}s");

                    if (sampleData != null)
                    {
                        Rect r = AudioCurveRendering.BeginCurveFrame(GUILayoutUtility.GetRect(1, 10000, 100, 100));
                        DrawCurve(
                            r,
                            t => previewBeginSample + (int)(t / 0.5 * previewBeforeSample)
                        );
                        EditorGUI.DrawRect(
                            new Rect(
                                r.x + r.width / 2 * (audioSource.timeSamples - previewBeginSample) /
                                previewBeforeSample,
                                r.y,
                                1,
                                r.height
                            ),
                            Color.red
                        );
                        EditorGUI.DrawRect(
                            new Rect(
                                r.x + r.width / 2,
                                r.y,
                                1,
                                r.height
                            ),
                            Color.blue
                        );
                        AudioCurveRendering.EndCurveFrame();
                    }

                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.IntSlider(audioSource.timeSamples, 0, audioClip.samples);
                    EditorGUI.EndDisabledGroup();

                    bool btn2 = GUILayout.Button($"No Loop, play at loopEndSample - {previewSecondsBefore}s");

                    bool btn3 = GUILayout.Button("Play Both");
                    if (btn1 || btn3)
                    {
                        EditorApplication.update -= Update;
                        EditorApplication.update += Update;
                        audioLooper.musicEntry = musicEntry;
                        audioLooper.SetProgress(previewBeginSample);
                    }

                    if (btn2 || btn3)
                    {
                        EditorApplication.update -= Update;
                        EditorApplication.update += Update;
                        audioSource.timeSamples = previewBeginSample;
                        audioSource.Play();
                    }

                    if (GUILayout.Button("Stop Both"))
                    {
                        EditorApplication.update -= Update;
                        audioLooper.Stop();
                        audioSource.Stop();
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void CreateMusicEntry(string path)
        {
            const string resFolderName = "/Resources/";
            var index = path.LastIndexOf(resFolderName, StringComparison.Ordinal);
            if (index != -1)
            {
                index += resFolderName.Length;
            }
            else
            {
                index = 0;
            }

            var assetPath = path.Substring(index);
            var fileName = Path.GetFileNameWithoutExtension(assetPath);
            var loadPath = Path.Combine(Path.GetDirectoryName(assetPath), fileName);
            var entryPath = Path.Combine(Path.GetDirectoryName(path), fileName + "_entry.asset");

            // create asset
            var entry = AssetDatabase.LoadAssetAtPath<MusicEntry>(entryPath);
            if (entry == null)
            {
                entry = CreateInstance<MusicEntry>();
                AssetDatabase.CreateAsset(entry, entryPath);
            }

            entry.id = fileName;
            entry.displayNames = new SerializableDictionary<SystemLanguage, string> {[I18n.DefaultLocale] = fileName};
            entry.resourcePath = Utils.ConvertPathSeparator(loadPath);

            // get loopBeginSample and loopEndSample from audio clips
            AudioClip clip = Resources.Load<AudioClip>(loadPath);
            if (clip == null)
            {
                Debug.LogError($"Nova: AudioClip {loadPath} not found.");
                return;
            }

            AudioClip headClip = Resources.Load<AudioClip>(loadPath + "_head");
            AudioClip loopClip = Resources.Load<AudioClip>(loadPath + "_loop");
            if (loopClip != null)
            {
                if (headClip != null)
                {
                    int headSamples = Utils.ConvertSamples(headClip, clip);
                    int loopSamples = Utils.ConvertSamples(loopClip, clip);
                    entry.loopBeginSample = headSamples;
                    entry.loopEndSample = headSamples + loopSamples;
                }
                else
                {
                    int loopSamples = Utils.ConvertSamples(loopClip, clip);
                    entry.loopBeginSample = 0;
                    entry.loopEndSample = loopSamples;
                }
            }
            else
            {
                if (headClip != null)
                {
                    int headSamples = Utils.ConvertSamples(headClip, clip);
                    entry.loopBeginSample = headSamples;
                    entry.loopEndSample = clip.samples;
                }
                else
                {
                    entry.loopBeginSample = 0;
                    entry.loopEndSample = clip.samples;
                }
            }

            // Debug.Log($"{entry.loopBeginSample} {entry.loopEndSample} {clip.samples}");

            EditorUtility.SetDirty(entry);
        }

        [MenuItem("Assets/Create/Nova/Music Entry", false)]
        public static void CreateMusicEntry()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            CreateMusicEntry(path);
        }

        [MenuItem("Assets/Create/Nova/Music Entry", true)]
        public static bool CreateMusicEntryValidation()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(AudioClip);
        }

        [MenuItem("Assets/Nova/Create Music Entries for All Audio Clips", false)]
        public static void CreateMusicEntryForAllAudioClips()
        {
            var dir = EditorUtils.GetSelectedDirectory();
            var guids = AssetDatabase.FindAssets("t:AudioClip", new[] {dir});
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.Contains("_head") && !path.Contains("_loop"))
                {
                    CreateMusicEntry(path);
                }
            }
        }
    }
}
