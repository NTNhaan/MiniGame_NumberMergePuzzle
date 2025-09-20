using UnityEngine;
using System.IO;
using Data;

public class ScreenshotCapture : Singleton<ScreenshotCapture>
{
    [Header("Screenshot Settings")]
    public KeyCode captureKey = KeyCode.F12;
    public int width = 1080;
    public int height = 1920;
    public string folderName = "Screenshots";
    public string baseFileName = "screenshot";

    private Camera targetCamera;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void Update()
    {
        if (Input.GetKeyDown(captureKey))
        {
            TakeScreenshot();
        }
    }

    public void SetCamera(Camera cam)
    {
        targetCamera = cam;
    }

    private void TakeScreenshot()
    {
        if (targetCamera == null)
        {
            Debug.LogWarning("⚠ Không có Camera để chụp! Gọi ScreenshotCapture.Instance.SetCamera(Camera) sau khi load scene.");
            return;
        }

        // Render camera vào RenderTexture
        RenderTexture rt = new RenderTexture(width, height, 24);
        targetCamera.targetTexture = rt;
        Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGB24, false);

        targetCamera.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenShot.Apply();

        // Reset lại camera
        targetCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        // Tạo folder nếu chưa có
        string folderPath = Path.Combine(Application.dataPath, folderName);
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        // Tạo tên file unique
        string fileName = baseFileName + ".png";
        string fullPath = Path.Combine(folderPath, fileName);
        int index = 1;
        while (File.Exists(fullPath))
        {
            fileName = $"{baseFileName}{index}.png";
            fullPath = Path.Combine(folderPath, fileName);
            index++;
        }

        // Ghi file
        File.WriteAllBytes(fullPath, screenShot.EncodeToPNG());
        Debug.Log($"📸 Screenshot saved: {fullPath}");

#if UNITY_EDITOR
        // Mở folder và highlight file trong Windows
        System.Diagnostics.Process.Start("explorer.exe", "/select," + fullPath.Replace("/", "\\"));
#endif
    }
}
