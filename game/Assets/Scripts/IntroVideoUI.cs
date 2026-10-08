using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class IntroVideoUI : MonoBehaviour
{
    [Header("Bağlantılar")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private StoryIntroUI storyIntroUI;
    [SerializeField] private CharacterSelectionUI characterSelectionUI;
    [SerializeField] private GameObject mainMenuCanvas;
    [SerializeField] private GameObject characterSelectionCanvas;
    [SerializeField] private GameObject videoRoot;
    [SerializeField] private RawImage videoImage;
    [SerializeField] private AspectRatioFitter videoAspectFitter;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Button skipButton;

    [Header("Karakter Videoları")]
    [SerializeField] private VideoClip maleIntroClip;
    [SerializeField] private VideoClip femaleIntroClip;

    private const float PlaybackProgressTimeout = 3f;

    private bool transitionHandled;
    private int decodedFrameEvents;
    private RenderTexture playbackTexture;
    private Coroutine playbackWatchdog;

    private void Awake()
    {
        if (skipButton != null)
        {
            skipButton.onClick.AddListener(SkipIntro);
        }

        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted += HandlePrepared;
            videoPlayer.loopPointReached += HandleVideoCompleted;
            videoPlayer.errorReceived += HandleVideoError;
            videoPlayer.frameReady += HandleFrameReady;
        }
    }

    private void Start()
    {
        if (videoRoot != null)
        {
            videoRoot.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(SkipIntro);
        }

        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= HandlePrepared;
            videoPlayer.loopPointReached -= HandleVideoCompleted;
            videoPlayer.errorReceived -= HandleVideoError;
            videoPlayer.frameReady -= HandleFrameReady;
        }

        StopPlaybackAndReleaseTexture();
    }

    public void PlaySelectedCharacterIntro()
    {
        if (!HasRequiredReferences())
        {
            Debug.LogError("IntroVideoUI bağlantıları eksik.");
            MoveSafelyToStoryIntro();
            return;
        }

        VideoClip selectedClip = GetSelectedCharacterClip();
        if (selectedClip == null)
        {
            if (!SelectedCharacterState.HasSelection)
            {
                ReturnToCharacterSelection();
                return;
            }

            Debug.LogError(
                "Seçilen karakter için intro VideoClip referansı bulunamadı: " +
                SelectedCharacterState.SelectedCharacter
            );
            MoveSafelyToStoryIntro();
            return;
        }

        transitionHandled = false;
        decodedFrameEvents = 0;
        SetPlayerControl(false);
        mainMenuCanvas.SetActive(false);
        characterSelectionCanvas.SetActive(false);
        videoRoot.SetActive(true);
        skipButton.interactable = true;

        StopPlaybackAndReleaseTexture();
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = selectedClip;

        if (!CreateAndConnectPlaybackTexture(selectedClip))
        {
            Debug.LogError(
                "Intro videosu için RenderTexture oluşturulamadı: " +
                selectedClip.name
            );
            CompleteIntroOnce();
            return;
        }

        ConfigureVideoAspect(selectedClip.width, selectedClip.height);
        ConfigurePlayback();
        ConfigureAudio(selectedClip);
        Debug.Log(
            "Intro videosu hazırlanıyor: " +
            SelectedCharacterState.SelectedCharacter + " -> " +
            selectedClip.name
        );
        videoPlayer.Prepare();
    }

    private VideoClip GetSelectedCharacterClip()
    {
        string selectedCharacter = SelectedCharacterState.SelectedCharacter;

        if (selectedCharacter == SelectedCharacterState.Male)
        {
            return maleIntroClip;
        }

        if (selectedCharacter == SelectedCharacterState.Female)
        {
            return femaleIntroClip;
        }

        Debug.LogWarning(
            "Intro videosu başlatılamadı. Geçersiz karakter seçimi: " +
            selectedCharacter
        );
        return null;
    }

    private void HandlePrepared(VideoPlayer source)
    {
        if (transitionHandled || !videoRoot.activeSelf)
        {
            return;
        }

        if (!source.isPrepared ||
            playbackTexture == null ||
            !playbackTexture.IsCreated())
        {
            Debug.LogError(
                "Intro videosu hazırlandı ancak görüntü hedefi kullanılamıyor."
            );
            CompleteIntroOnce();
            return;
        }

        ConfigureVideoAspect(source.width, source.height);
        source.Play();
        StartPlaybackWatchdog(source);
        Debug.Log("Intro videosu oynatılıyor: " + source.clip.name);
    }

    private void HandleFrameReady(VideoPlayer source, long frameIndex)
    {
        decodedFrameEvents++;
    }

    private void HandleVideoCompleted(VideoPlayer source)
    {
        CompleteIntroOnce();
    }

    private void HandleVideoError(VideoPlayer source, string message)
    {
        Debug.LogError("Intro videosu oynatılamadı: " + message);
        CompleteIntroOnce();
    }

    private void SkipIntro()
    {
        CompleteIntroOnce();
    }

    private void CompleteIntroOnce()
    {
        if (transitionHandled)
        {
            return;
        }

        transitionHandled = true;
        skipButton.interactable = false;
        StopPlaybackAndReleaseTexture();
        videoRoot.SetActive(false);
        storyIntroUI.ShowIntro();
    }

    private void ReturnToCharacterSelection()
    {
        transitionHandled = true;
        StopPlaybackAndReleaseTexture();
        videoRoot.SetActive(false);
        SetPlayerControl(false);
        characterSelectionUI.ShowSelection();
    }

    private void MoveSafelyToStoryIntro()
    {
        StopPlaybackAndReleaseTexture();

        if (videoRoot != null)
        {
            videoRoot.SetActive(false);
        }

        SetPlayerControl(false);
        if (storyIntroUI != null)
        {
            storyIntroUI.ShowIntro();
        }
    }

    private void ConfigurePlayback()
    {
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.waitForFirstFrame = true;
        // Unity 6000.3.14f1 can freeze video decoding with Skip On Drop
        // enabled (UUM-142660 / UUM-140747), overflowing the audio buffer.
        videoPlayer.skipOnDrop = false;
        videoPlayer.playbackSpeed = 1f;
        videoPlayer.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
        videoPlayer.sendFrameReadyEvents = true;
    }

    private void ConfigureAudio(VideoClip clip)
    {
        audioSource.enabled = true;
        audioSource.Stop();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;

        ushort trackCount = clip.audioTrackCount > 0
            ? (ushort)1
            : (ushort)0;
        videoPlayer.controlledAudioTrackCount = trackCount;

        if (trackCount == 0)
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            return;
        }

        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetTargetAudioSource(0, audioSource);
    }

    private void StartPlaybackWatchdog(VideoPlayer source)
    {
        StopPlaybackWatchdog();
        playbackWatchdog = StartCoroutine(EnsurePlaybackProgresses(source));
    }

    private IEnumerator EnsurePlaybackProgresses(VideoPlayer source)
    {
        int initialFrameEvents = decodedFrameEvents;
        long initialFrame = source.frame;
        yield return new WaitForSecondsRealtime(PlaybackProgressTimeout);
        playbackWatchdog = null;

        if (transitionHandled || source == null)
        {
            yield break;
        }

        bool hasProgressed =
            decodedFrameEvents >= initialFrameEvents + 2 ||
            source.frame >= initialFrame + 2;
        if (hasProgressed)
        {
            yield break;
        }

        Debug.LogError(
            "Transcode edilmiş intro videosunda kare ilerlemesi başlamadı: " +
            source.clip.name
        );
        CompleteIntroOnce();
    }

    private void StopPlaybackWatchdog()
    {
        if (playbackWatchdog == null)
        {
            return;
        }

        StopCoroutine(playbackWatchdog);
        playbackWatchdog = null;
    }

    private bool CreateAndConnectPlaybackTexture(VideoClip clip)
    {
        int maximumSize = Mathf.Max(1024, SystemInfo.maxTextureSize);
        int width = clip.width > 0
            ? Mathf.Min((int)clip.width, maximumSize)
            : 1920;
        int height = clip.height > 0
            ? Mathf.Min((int)clip.height, maximumSize)
            : 1080;

        playbackTexture = new RenderTexture(
            width,
            height,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.sRGB
        )
        {
            name = "LumoraIntroVideoTexture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            autoGenerateMips = false,
            antiAliasing = 1
        };

        if (!playbackTexture.Create())
        {
            ReleasePlaybackTexture();
            return false;
        }

        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = playbackTexture;
        videoImage.texture = playbackTexture;
        videoImage.enabled = true;
        return true;
    }

    private void StopPlaybackAndReleaseTexture()
    {
        StopPlaybackWatchdog();

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.targetTexture = null;
            videoPlayer.clip = null;
        }

        if (audioSource != null)
        {
            audioSource.Stop();
        }

        if (videoImage != null)
        {
            videoImage.texture = null;
        }

        ReleasePlaybackTexture();
    }

    private void ReleasePlaybackTexture()
    {
        if (playbackTexture == null)
        {
            return;
        }

        if (playbackTexture.IsCreated())
        {
            playbackTexture.Release();
        }

        Destroy(playbackTexture);
        playbackTexture = null;
    }

    private void ConfigureVideoAspect(ulong width, ulong height)
    {
        videoAspectFitter.aspectRatio = width == 0 || height == 0
            ? 16f / 9f
            : (float)width / height;
    }

    private void SetPlayerControl(bool isEnabled)
    {
        if (playerController != null)
        {
            playerController.enabled = isEnabled;
        }
    }

    private bool HasRequiredReferences()
    {
        return playerController != null &&
               storyIntroUI != null &&
               characterSelectionUI != null &&
               mainMenuCanvas != null &&
               characterSelectionCanvas != null &&
               videoRoot != null &&
               videoImage != null &&
               videoAspectFitter != null &&
               videoPlayer != null &&
               audioSource != null &&
               skipButton != null;
    }
}
