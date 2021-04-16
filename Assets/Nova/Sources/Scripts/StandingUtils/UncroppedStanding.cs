using System.IO;
using UnityEngine;

namespace Nova
{
    public class UncroppedStanding : MonoBehaviour
    {
        // relative to Assets folder
        public string outputDirectory;

        public string absoluteOutputDirectory => Path.Combine(Application.dataPath, outputDirectory);
    }
}