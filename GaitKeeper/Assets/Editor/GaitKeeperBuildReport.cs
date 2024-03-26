using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

class CantTouchThisBuildReport : IPostprocessBuildWithReport
{
    public int callbackOrder { get { return 0; } }
    public void OnPostprocessBuild(BuildReport report)
    {
        UnityEngine.Debug.Log("CantTouchThisBuildReport.OnPostprocessBuild for target " + report.summary.platform + " at path " + report.summary.outputPath);
        EditorSceneManager.OpenScene(EditorBuildSettings.scenes[0].path);
        Scene builtScene = SceneManager.GetSceneByBuildIndex(0);
        var logDir = Directory.CreateDirectory(Path.GetDirectoryName(report.summary.outputPath) + @"\logs");
        UnityEngine.Debug.Log(logDir.FullName);
        foreach (string assemblyName in new[] { "ModularAgents", "Mujoco", "MLAgents", "CSharp" })
        {
            WriteSceneSummaryForAssemblyName(builtScene, assemblyName, logDir.FullName + $"/summary_{assemblyName}.json");
        }
        string gitPath = Directory.GetParent(Application.dataPath).Parent.FullName;
        WriteGitCommitForBuild(gitPath, logDir.FullName + $"/git_commit_hash.txt");
        WriteDateForBuild(logDir.FullName + $"/build_date.txt");
    }

    private static void WriteSceneSummaryForAssemblyName(Scene builtScene, string assemblyName, string filePath)
    {
        StringBuilder stringBuilder = new StringBuilder();
        foreach (var rootObject in builtScene.GetRootGameObjects())
        {
            
            foreach (var go in rootObject.GetComponentsInChildren<Transform>().Select(t => t.gameObject))
            {
                if (go.GetComponent<MonoBehaviour>() &&
                    go.GetComponents<MonoBehaviour>()
                    .Where(comp => comp.GetType().AssemblyQualifiedName.Contains(assemblyName))
                    .Count() < 1) continue;

                stringBuilder.Append($"\"{go.name}\":\n{{");
                foreach (var comp in go.GetComponents<MonoBehaviour>())
                {
                    if (comp.GetType().AssemblyQualifiedName.Contains(assemblyName))
                    {
                        var jsonString = UnityEngine.JsonUtility.ToJson(comp, true);
                        jsonString = jsonString.Replace("\n", "\n    ");
                        jsonString = $"\n    \"{comp.GetType().Name}\": \n    {{\n        \"instanceID\": {comp.GetInstanceID()},\n" + jsonString[1..];
                        stringBuilder.Append(jsonString);
                        stringBuilder.Append(",\n\n");
                    }
                }
                stringBuilder = new StringBuilder(stringBuilder.ToString().TrimEnd(','));
                stringBuilder.Append("\n},\n\n");
            }
            stringBuilder = new StringBuilder(stringBuilder.ToString().TrimEnd(','));
        }
        stringBuilder = new StringBuilder(stringBuilder.ToString().TrimEnd(','));
        File.WriteAllText(filePath, stringBuilder.ToString());
    }

    private static void WriteGitCommitForBuild(string gitRepoPath, string filePath)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = "cmd.exe";
        startInfo.Arguments = "/c git rev-parse HEAD";
        startInfo.WorkingDirectory = gitRepoPath;
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        Process process = new Process();
        process.StartInfo = startInfo;
        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        File.WriteAllText(filePath, output);
    }

    private static void WriteDateForBuild(string filePath)
    {
        File.WriteAllText(filePath, System.DateTime.Now.ToString());
    }
}