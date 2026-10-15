using System.Collections;
using System.IO;
using UnityEngine;

// Opt-in capture used to document this exact build; normal players never start it.
public sealed class RecoveryReleaseCapture : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (Application.isEditor) return;
        string directory = RecoveryStandaloneValidation.Argument("-recovery-capture-dir");
        if (string.IsNullOrEmpty(directory)) return;
        var capture = new GameObject("Release screenshot capture").AddComponent<RecoveryReleaseCapture>();
        DontDestroyOnLoad(capture.gameObject);
        capture.StartCoroutine(capture.CaptureFrames(Path.GetFullPath(directory)));
    }

    private IEnumerator CaptureFrames(string directory)
    {
        Directory.CreateDirectory(directory);
        yield return new WaitForSecondsRealtime(4f);
        yield return Capture(directory, "main-menu.png");
        while (!Player.LocalPlayer || MainMenuManager.IsInMenu)
            yield return null;
        yield return new WaitForSecondsRealtime(5f);
        yield return Capture(directory, "local-gameplay.png");
        yield return new WaitForSecondsRealtime(8f);
        yield return Capture(directory, "local-world.png");
    }

    private IEnumerator Capture(string directory, string name)
    {
        yield return new WaitForEndOfFrame();
        var screenshot = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Path.Combine(directory, name), screenshot.EncodeToPNG());
        Destroy(screenshot);
    }
}
