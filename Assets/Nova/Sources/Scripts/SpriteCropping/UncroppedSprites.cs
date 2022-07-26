using System.IO;
using UnityEngine;

namespace Nova
{
    public class UncroppedSprites : MonoBehaviour
    {
        // relative to the project folder, e.g., "Assets/path/to/image"
        public string outputDirectory;

        public string absoluteOutputDirectory => Path.Combine(Path.GetDirectoryName(Application.dataPath), outputDirectory);
    }
}
