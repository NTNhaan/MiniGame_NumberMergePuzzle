using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ScreenShoot : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.T))
        {
            StartCoroutine(CaptureScreenshot());
        }
#endif
    }

#if UNITY_EDITOR
    private IEnumerator CaptureScreenshot()
    {
        yield return new WaitForEndOfFrame();

        // Capture the screenshot
        Texture2D screenImage = new Texture2D(Screen.width, Screen.height);
        screenImage.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenImage.Apply();

        // Encode the image to PNG
        byte[] imageBytes = screenImage.EncodeToPNG();

        // Open a file dialog to select the save location
        string path = EditorUtility.SaveFilePanel("Save Screenshot", "", "screenshot.png", "png");

        if (!string.IsNullOrEmpty(path))
        {
            // Save the image to the selected path
            File.WriteAllBytes(path, imageBytes);
            Debug.Log("Screenshot saved to: " + path);
        }
    }
#endif
}
