using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SpriteCreator : MonoBehaviour
{
    public Vector2Int SpriteSize = new Vector2Int(512, 512);

    void Start()
    {
        var camera = Camera.main;

        var renderTexture = new RenderTexture(SpriteSize.x * 4, SpriteSize.y * 4, depth: 32, RenderTextureFormat.ARGB32);
        var targetTexture = new Texture2D(SpriteSize.x * 4, SpriteSize.y * 4);

        camera.targetTexture = renderTexture;
        RenderTexture.active = renderTexture;

        camera.Render();
        targetTexture.ReadPixels(new Rect(0, 0, SpriteSize.x * 4, SpriteSize.y * 4), 0, 0);
        targetTexture.Apply();

        RenderTexture.active = null;
        camera.targetTexture = null;

        targetTexture = FilteredDownscale(targetTexture, SpriteSize.x, SpriteSize.y);

        var fileBytes = targetTexture.EncodeToPNG();
        var sceneLocation = SceneManager.GetActiveScene().path;

        Debug.Log(sceneLocation);

        string directory = Path.GetDirectoryName(sceneLocation);
        string sceneName = Path.GetFileNameWithoutExtension(sceneLocation);
        string filePath = Path.Combine(directory, sceneName + ".png");
        File.WriteAllBytes(filePath, fileBytes);

        Debug.Log("Sprite saved to: " + filePath);
    }

    // source: https://discussions.unity.com/t/cant-find-a-way-to-downscale-a-560x560-texture2d-image-to-a-28x28-resolution-without-losing-lots-of-pixels/249425/2
    public Texture2D FilteredDownscale(Texture2D a_Source, int a_NewWidth, int a_NewHeight)
    {
        // Keep the last active RT
        RenderTexture _LastActiveRT = RenderTexture.active;

        // Start by halving the source dimensions
        int _Width = a_Source.width / 2;
        int _Height = a_Source.height / 2;

        // Cap to the target dimensions.
        // This could be done with Mathf.Max() but that wouldn't take into account aspect ratio.
        if (_Width < a_NewWidth || _Height < a_NewHeight)
        {
            _Width = a_NewWidth;
            _Height = a_NewHeight;
        }

        // Create a temporary downscaled RT
        RenderTexture _Tmp1 = RenderTexture.GetTemporary(_Width, _Height, 0, RenderTextureFormat.ARGB32);

        // Copy the source into our temporary RT
        Graphics.Blit(a_Source, _Tmp1);

        // Loop until our target dimensions have been reached
        while (_Width > a_NewWidth && _Height > a_NewHeight)
        {
            // Keep halving our current dimensions
            _Width /= 2;
            _Height /= 2;

            // And match our target dimensions once small enough
            if (_Width < a_NewWidth || _Height < a_NewHeight)
            {
                _Width = a_NewWidth;
                _Height = a_NewHeight;
            }

            // Downscale again into a smaller RT
            RenderTexture _Tmp2 = RenderTexture.GetTemporary(_Width, _Height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(_Tmp1, _Tmp2);

            // Swap our temporary RTs and release the oldest one
            (_Tmp1, _Tmp2) = (_Tmp2, _Tmp1);
            RenderTexture.ReleaseTemporary(_Tmp2);
        }

        // At this point _Tmp1 should hold our fully downscaled image,
        // so set it as the active RT
        RenderTexture.active = _Tmp1;

        // Create a new texture of the desired dimensions and copy our data into it
        Texture2D _Tex = new Texture2D(a_NewWidth, a_NewHeight);
        _Tex.ReadPixels(new Rect(0, 0, a_NewWidth, a_NewHeight), 0, 0);
        _Tex.Apply();

        // Reset the active RT and release our last temporary copy
        RenderTexture.active = _LastActiveRT;
        RenderTexture.ReleaseTemporary(_Tmp1);

        return _Tex;
    }
}
