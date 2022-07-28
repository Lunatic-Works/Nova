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
        ReturnTitle,
        QuitGame,
        ToggleFullScreen,
        LeaveView,
        EditorBackward,
        EditorBeginChapter,
        EditorPreviousChapter,
        EditorNextChapter,
        EditorReloadScripts,
        EditorRerunAction
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
