using UnityEngine;

namespace Nova
{
    public static class RealScreen
    {
        public static int height = Screen.height;
        public static int width = Screen.width;
        public static Vector2 uiSize = new Vector2(Screen.width, Screen.height);
        public static float fHeight = Screen.height;
        public static float fWidth = Screen.width;

        private const int referenceWidth = 1920;
        public static float scale => fWidth / referenceWidth;

        public static Vector2 offset =>
            new Vector2(((float)Screen.width - width) / 2, ((float)Screen.height - height) / 2);

        public static bool isScreenInitialized = false;
        public static bool isUIInitialized = false;
    }
}
