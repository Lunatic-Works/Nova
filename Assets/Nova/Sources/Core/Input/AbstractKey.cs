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
        ReturnTitle,
        QuitGame,
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
        GameHold = 1 << 1,
        UI = 1 << 2,
        GamePress = Game | GameHold,
        Always = Game | UI,
        All = Game | GameHold | UI
    }
}
