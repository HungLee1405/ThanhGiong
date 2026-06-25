using System.Collections;
using System.IO;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class EndingVideoPlayer : MonoBehaviour
{
    private const string EndingSceneName = "EndingScene";
    private const string VideoFileName = "ThanhGiong_Ending_Final.mp4";
    private const string ReturnSceneName = "StartScene";

    private VideoPlayer videoPlayer;
    private AudioSource videoAudioSource;
    private RenderTexture videoTexture;
    private RawImage videoImage;
    private AspectRatioFitter videoAspectFitter;
    private string statusMessage = "Đang tải video ending...";
    private bool isReturningToMenu;
    private bool canSkip;
    private static bool sceneHookRegistered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        if (sceneHookRegistered)
            return;

        SceneManager.sceneLoaded += OnSceneLoaded;
        sceneHookRegistered = true;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != EndingSceneName)
            return;

        if (FindFirstObjectByType<EndingVideoPlayer>() != null)
            return;

        GameObject player = new GameObject("Ending Video Player");
        player.AddComponent<EndingVideoPlayer>();
    }

    private void Start()
    {
        StartCoroutine(AllowSkipAfterDelay());
        SetupVideoPlayer();
    }

    private void Update()
    {
        if (!canSkip || isReturningToMenu)
            return;

        if (SkipPressedThisFrame())
        {
            ReturnToMenu();
        }
    }

    private static bool SkipPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null &&
            (keyboard.escapeKey.wasPressedThisFrame ||
             keyboard.spaceKey.wasPressedThisFrame ||
             keyboard.enterKey.wasPressedThisFrame ||
             keyboard.numpadEnterKey.wasPressedThisFrame))
        {
            return true;
        }

        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            return true;
        }

        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Escape) ||
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetMouseButtonDown(0))
        {
            return true;
        }
#endif

        return false;
    }

    private void OnGUI()
    {
        if (!string.IsNullOrEmpty(statusMessage))
        {
            GUIStyle statusStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 28,
                wordWrap = true
            };
            statusStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(0f, Screen.height * 0.45f, Screen.width, 100f), statusMessage, statusStyle);
        }

        if (canSkip && !isReturningToMenu)
        {
            GUIStyle skipStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.LowerRight,
                fontSize = 18
            };
            skipStyle.normal.textColor = new Color(1f, 1f, 1f, 0.65f);
            GUI.Label(new Rect(0f, Screen.height - 48f, Screen.width - 24f, 32f), "Nhấn Esc / Space để bỏ qua", skipStyle);
        }
    }

    private void SetupVideoPlayer()
    {
        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.None;

        CreateEndingCamera();
        CreateVideoCanvas();
        videoAudioSource = gameObject.AddComponent<AudioSource>();
        videoAudioSource.playOnAwake = false;

        StopExistingAudioSources();

        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = BuildStreamingAssetsUrl(VideoFileName);
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoTexture = CreateVideoTexture(1920, 1080);
        videoPlayer.targetTexture = videoTexture;
        if (videoImage != null)
        {
            videoImage.texture = videoTexture;
        }
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.controlledAudioTrackCount = 1;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetTargetAudioSource(0, videoAudioSource);
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.errorReceived += OnVideoError;

        Debug.Log("Ending video preparing: " + videoPlayer.url);
        videoPlayer.Prepare();
    }

    private void CreateVideoCanvas()
    {
        GameObject canvasObject = new GameObject("Ending Video Canvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject backgroundObject = new GameObject("Black Background");
        backgroundObject.transform.SetParent(canvasObject.transform, false);
        Image background = backgroundObject.AddComponent<Image>();
        background.color = Color.black;
        StretchToFullScreen(background.rectTransform);

        GameObject videoObject = new GameObject("Ending Video");
        videoObject.transform.SetParent(canvasObject.transform, false);
        videoImage = videoObject.AddComponent<RawImage>();
        videoImage.color = Color.white;
        StretchToFullScreen(videoImage.rectTransform);

        videoAspectFitter = videoObject.AddComponent<AspectRatioFitter>();
        videoAspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        videoAspectFitter.aspectRatio = 16f / 9f;
    }

    private static void StretchToFullScreen(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static RenderTexture CreateVideoTexture(int width, int height)
    {
        RenderTexture texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
        {
            name = "Ending Video Render Texture",
            useMipMap = false,
            autoGenerateMips = false
        };
        texture.Create();
        return texture;
    }

    private void ResizeVideoTexture(VideoPlayer preparedPlayer)
    {
        if (preparedPlayer == null || preparedPlayer.width == 0 || preparedPlayer.height == 0)
            return;

        int width = Mathf.Max(1, (int)preparedPlayer.width);
        int height = Mathf.Max(1, (int)preparedPlayer.height);

        if (videoAspectFitter != null)
        {
            videoAspectFitter.aspectRatio = width / (float)height;
        }

        if (videoTexture != null && videoTexture.width == width && videoTexture.height == height)
            return;

        ReleaseVideoTexture();

        videoTexture = CreateVideoTexture(width, height);
        preparedPlayer.targetTexture = videoTexture;

        if (videoImage != null)
        {
            videoImage.texture = videoTexture;
        }
    }

    private Camera CreateEndingCamera()
    {
        Camera endingCamera = Camera.main;
        if (endingCamera == null)
        {
            endingCamera = FindFirstObjectByType<Camera>();
        }

        if (endingCamera != null)
        {
            ConfigureEndingCamera(endingCamera);
            return endingCamera;
        }

        GameObject cameraObject = new GameObject("Ending Camera");
        endingCamera = cameraObject.AddComponent<Camera>();
        cameraObject.tag = "MainCamera";
        ConfigureEndingCamera(endingCamera);

        return endingCamera;
    }

    private static void ConfigureEndingCamera(Camera endingCamera)
    {
        if (endingCamera == null)
            return;

        endingCamera.clearFlags = CameraClearFlags.SolidColor;
        endingCamera.backgroundColor = Color.black;
        endingCamera.cullingMask = 0;
        endingCamera.depth = 100f;

        if (FindFirstObjectByType<AudioListener>() == null)
        {
            endingCamera.gameObject.AddComponent<AudioListener>();
        }
    }

    private void StopExistingAudioSources()
    {
        AudioSource[] audioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);

        for (int i = 0; i < audioSources.Length; i++)
        {
            if (audioSources[i] == null || audioSources[i] == videoAudioSource)
                continue;

            audioSources[i].Stop();
        }
    }

    private static string BuildStreamingAssetsUrl(string fileName)
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);

        if (path.Contains("://"))
            return path;

        return "file://" + path.Replace("\\", "/");
    }

    private void OnVideoPrepared(VideoPlayer preparedPlayer)
    {
        ResizeVideoTexture(preparedPlayer);
        statusMessage = "";
        preparedPlayer.Play();
    }

    private void OnVideoFinished(VideoPlayer finishedPlayer)
    {
        ReturnToMenu();
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        statusMessage = "Không phát được video ending:\n" + message;
        StartCoroutine(ReturnToMenuAfterError());
    }

    private IEnumerator AllowSkipAfterDelay()
    {
        yield return new WaitForSecondsRealtime(1.5f);
        canSkip = true;
    }

    private IEnumerator ReturnToMenuAfterError()
    {
        yield return new WaitForSecondsRealtime(3f);
        ReturnToMenu();
    }

    private void ReturnToMenu()
    {
        if (isReturningToMenu)
            return;

        isReturningToMenu = true;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        ReleaseVideoTexture();

        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager != null && networkManager.IsListening)
        {
            networkManager.Shutdown();
        }

        SceneManager.LoadScene(ReturnSceneName, LoadSceneMode.Single);
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.errorReceived -= OnVideoError;
        }

        ReleaseVideoTexture();
    }

    private void ReleaseVideoTexture()
    {
        if (videoTexture == null)
            return;

        if (videoImage != null && videoImage.texture == videoTexture)
        {
            videoImage.texture = null;
        }

        videoTexture.Release();
        Destroy(videoTexture);
        videoTexture = null;
    }
}
