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
        Game = 1,
        UI = 2,
        All = 3
    }
}