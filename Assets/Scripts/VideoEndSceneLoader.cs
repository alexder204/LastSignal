using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class VideoEndSceneLoader : MonoBehaviour
{
    [Header("Video Timing")]
    [Tooltip("Delay before the video starts playing (seconds)")]
    public float delayBeforeStart = 1.5f;

    [Tooltip("Delay after the video ends before loading next scene (seconds)")]
    public float delayAfterEnd = 2f;

    [Header("Scene")]
    public string nextSceneName;

    private VideoPlayer vp;
    private bool triggered;

    void Awake()
    {
        vp = GetComponent<VideoPlayer>();
        vp.playOnAwake = false;   // IMPORTANT
    }

    void OnEnable()
    {
        vp.loopPointReached += OnVideoFinished;
    }

    void OnDisable()
    {
        vp.loopPointReached -= OnVideoFinished;
    }

    void Start()
    {
        StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart()
    {
        if (delayBeforeStart > 0f)
            yield return new WaitForSeconds(delayBeforeStart);

        vp.Play();
    }

    private void OnVideoFinished(VideoPlayer source)
    {
        if (triggered) return;
        triggered = true;

        StartCoroutine(LoadNextSceneAfterDelay());
    }

    private IEnumerator LoadNextSceneAfterDelay()
    {
        if (delayAfterEnd > 0f)
            yield return new WaitForSeconds(delayAfterEnd);

        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogError("VideoEndSceneLoader: nextSceneName is empty.");
            yield break;
        }

        SceneManager.LoadScene(nextSceneName);
    }
}
