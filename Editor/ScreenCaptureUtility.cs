using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ScreenCaptureUtility : EditorWindow
{
    public Camera targetCamera;
    public int textureWidth = 1024;
    public int textureHeight = 512;
    public string directory = "";
    public string fileName = "screenshot";
    public string path = "";

    public (string, Vector2)[] resolutions = new (string, Vector2)[]
    {
        // iPhone
        ("iPhone 13", new Vector2(1180, 2532)), ("iPhone 13 mini", new Vector2(1080, 2340)), ("iPhone 13 Pro", new Vector2(1170, 2532)), ("iPhone 13 Pro Max", new Vector2(1284, 2778)),
        ("iPhone 12", new Vector2(1170, 2532)), ("iPhone 12 mini", new Vector2(1080, 2340)), ("iPhone 12 Pro", new Vector2(1170, 2532)), ("iPhone 12 Pro Max", new Vector2(1284, 2778)),
        ("iPhone 11", new Vector2(828, 1792)), ("iPhone 11 Pro", new Vector2(1125, 2436)), ("iPhone 11 Pro Max", new Vector2(1242, 2688)), ("iPhone SE 2nd Gen", new Vector2(750, 1334)),
        ("iPhone X", new Vector2(1125, 2436)), ("iPhone XS", new Vector2(1125, 2436)), ("iPhone XS Max", new Vector2(1242, 2688)), ("iPhone XR", new Vector2(828, 1792)),
        ("iPhone 8", new Vector2(750, 1334)), ("iPhone 8 Plus", new Vector2(1080, 1920)),
        ("iPhone 7", new Vector2(750, 1334)), ("iPhone 7 Plus", new Vector2(1242, 2208)), ("iPhone SE 1st Gen", new Vector2(640, 1136)),
        ("iPhone 6", new Vector2(750, 1334)), ("iPhone 6 Plus", new Vector2(1242, 2208)), ("iPhone 6s", new Vector2(750, 1334)), ("iPhone 6s Plus", new Vector2(1242, 2208)),
        ("iPhone 5", new Vector2(640, 1136)), ("iPhone 5s", new Vector2(640, 1136)), ("iPhone 5C", new Vector2(640, 1136)),
        // iPad
        ("iPad 5", new Vector2(1536, 2048)), ("iPad 6", new Vector2(1536, 2048)), ("iPad 7", new Vector2(1620, 2160)), ("iPad 8", new Vector2(1620, 2160)), ("iPad 9", new Vector2(1620, 2160)),
        ("iPad mini 4", new Vector2(1536, 2048)), ("iPad mini 5", new Vector2(1536, 2048)), ("iPad mini 6", new Vector2(1488, 2266)),
        ("iPad Air 3", new Vector2(1668, 2224)), ("iPad Air 4", new Vector2(1640, 2360)),
        ("iPad Pro 1st 9.7\"", new Vector2(1536, 2048)), ("iPad Pro 1st 12.9\"", new Vector2(2048, 2732)), ("iPad Pro 2nd 10.5\"", new Vector2(1668, 2224)), ("iPad Pro 2nd 12.9\"", new Vector2(2048, 2732)),
        ("iPad Pro 3rd 11\"", new Vector2(1668, 2388)), ("iPad Pro 3rd 12.9\"", new Vector2(2048, 2732)), ("iPad Pro 4th 11\"", new Vector2(1668, 2388)), ("iPad Pro 4th 12.9\"", new Vector2(2048, 2732)),
        ("iPad Pro 5th 11\"", new Vector2(1668, 2388)), ("iPad Pro 5th 12.9\"", new Vector2(2048, 2732)),
    };

    [MenuItem("Tools/Screen capture utility")]
    internal static void OpenUtilityWindow()
    {
        ScreenCaptureUtility window = GetWindow<ScreenCaptureUtility>(true, "Screen Capture Utility");
        window.position = new Rect(window.position.x, window.position.y, 560, 230);
        window.minSize = new Vector2(560, 230);
        window.maxSize = new Vector2(560, 230);
        window.ShowUtility();
    }

    public void OnGUI()
    {
        targetCamera = (Camera) EditorGUILayout.ObjectField("Render Camera", targetCamera, typeof(Camera), true);

        if (GUILayout.Button("Use Sceneview camera"))
        {
            targetCamera = GetSceneViewCamera();
        }

        EditorGUILayout.LabelField("Texture Size", EditorStyles.boldLabel);
        textureWidth = EditorGUILayout.IntField("Width", textureWidth);
        textureHeight = EditorGUILayout.IntField("Height", textureHeight);

        EditorGUILayout.LabelField("Screenshot Save Path", EditorStyles.boldLabel);
        using (EditorGUI.ChangeCheckScope scope = new EditorGUI.ChangeCheckScope())
        {
            directory = EditorGUILayout.TextField("Directory", directory);
            if (GUILayout.Button("Open Folder Panel"))
            {
                directory = EditorUtility.OpenFolderPanel("Select Screenshot save directory", "", "");
            }

            fileName = EditorGUILayout.TextField("File Name", fileName);

            if (scope.changed)
            {
                string folder = GetSafePath(directory.Trim('/'));
                string fileNamePrefix = GetSafeFileName(fileName);
                path = GetScreenshotSaveDir(folder, fileNamePrefix);
            }
        }

        EditorGUI.BeginDisabledGroup(true);
        path = EditorGUILayout.TextField("Output File Path", path);
        EditorGUI.EndDisabledGroup();

        if (GUILayout.Button("Capture!"))
        {
            CaptureScreenShot(targetCamera, textureWidth, textureHeight, directory, fileName, false);
        }
    }

    private Camera GetSceneViewCamera()
    {
        SceneView sw = SceneView.lastActiveSceneView;
        if (sw == null)
        {
            Debug.LogError("Active scene view not found.");
            return null;
        }
        return sw.camera;
    }

    internal static string GetSafePath(string path)
    {
        return string.Join("_", path.Split(Path.GetInvalidPathChars()));
    }

    internal static string GetSafeFileName(string fileName)
    {
        return string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
    }

    internal static string GetScreenshotSaveDir(string directory, string fileName)
    {
        fileName = fileName + "_" + DateTime.Now.ToString("yyMMdd_HHmmss") + ".png";
        return directory + "/" + fileName;
    }

    internal static void CaptureScreenShot(Camera camera, int width, int height, string directory, string fileName, bool ensureTransparentBackground)
    {
        directory = GetSafePath(directory.Trim('/'));
        fileName = GetSafeFileName(fileName);
        string path = GetScreenshotSaveDir(directory, fileName);

        // SceneView sw = SceneView.lastActiveSceneView;
        // if (sw == null)
        // {
        //     Debug.LogError("Active scene view not found. Unable to capture editor screenshot.");
        //     return;
        // }

        // Camera camera = sw.camera;
        if (camera == null)
        {
            Debug.LogError("No camera attached to current scene view. Unable to capture editor screenshot.");
            return;
        }

        RenderTexture renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        camera.targetTexture = renderTexture;

        CameraClearFlags clearFlags = camera.clearFlags;
        Color backgroundColor = camera.backgroundColor;

        if (ensureTransparentBackground)
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color();
        }

        camera.Render();

        if (ensureTransparentBackground)
        {
            camera.clearFlags = clearFlags;
            camera.backgroundColor = backgroundColor;
        }

        RenderTexture currentRenderTexture = RenderTexture.active;
        RenderTexture.active = camera.targetTexture;
        Texture2D screenshot = new Texture2D(width, height, TextureFormat.ARGB32, false);
        screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);

        if (QualitySettings.activeColorSpace == ColorSpace.Linear)
        {
            Color[] pixels = screenshot.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = pixels[i].gamma;
            }
            screenshot.SetPixels(pixels);
        }

        screenshot.Apply(false);

        Directory.CreateDirectory(directory);
        byte[] png = screenshot.EncodeToPNG();
        File.WriteAllBytes(path, png);

        camera.targetTexture = null;
        RenderTexture.active = currentRenderTexture;

        Debug.Log($"Screenshot saved to {path}");
    }
}
