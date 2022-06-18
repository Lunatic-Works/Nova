using System;

namespace Nova
{
    public enum AbstractKey
    {
        StepForward,
        Auto,
        FastForward,
        Save,
        Load,
        QuickSave,
        QuickLoad,
        ToggleDialogue,
        ShowLog,
        ToggleFullScreen,
        LeaveView,
        EditorBackward,
        EditorBeginChapter,
        EditorPreviousChapter,
        EditorNextChapter,
        EditorReloadScripts,
        EditorRerunAction
    }

    [Flags]
    public enum AbstractKeyGroup
    {
        None = 0,
        Game = 1 << 0,
        UI = 1 << 1,
        Editor = 1 << 2,
        Always = 1 << 3,
        All = Game | UI | Editor | Always
    }
}