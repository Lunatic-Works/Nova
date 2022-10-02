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
        ShowConfig,
        ToggleFullScreen,
        SwitchLanguage,
        LeaveView,
        EditorBackward,
        EditorBeginChapter,
        EditorPreviousChapter,
        EditorNextChapter,
        EditorReloadScripts,
        EditorUnlock
    }

    // If groups a & b != 0, actions in a and b can conflict
    [Flags]
    public enum AbstractKeyGroup
    {
        None = 0,
        Game = 1 << 0,
        UI = 1 << 1,
        All = Game | UI
    }
}
