using UnityEngine;
using TMPro;

public class LevelTimer : MonoBehaviour
{
    public TMP_Text timerText;

    private float currentTime;
    private bool isRunning;

    void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += OnGameStateChanged;

        if (timerText == null)
            timerText = GetComponent<TMP_Text>();

        Hide();
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;
    }

    void OnGameStateChanged(GameState state, ushort countdown)
    {
        // Start timer for time-limited states
        switch (state)
        {
            case GameState.Preparing:
            case GameState.Hiding:
            case GameState.Seeking:
            case GameState.RoundEnd:
                StartCountdown(countdown);
                break;
            default:
                Stop();
                break;
        }
    }

    void Update()
    {
        if (!isRunning) return;

        currentTime -= Time.deltaTime;
        if (currentTime < 0) currentTime = 0;

        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);

        if (timerText != null)
        {
            timerText.text = $"{minutes:D2}:{seconds:D2}";

            if (currentTime > 30f)
                timerText.color = Color.green;
            else if (currentTime > 10f)
                timerText.color = Color.yellow;
            else
            {
                timerText.color = Color.red;
                // Pulse effect
                float pulse = Mathf.Abs(Mathf.Sin(Time.time * 4f));
                timerText.transform.localScale = Vector3.one * (1f + pulse * 0.1f);
            }
        }
    }

    public void StartCountdown(ushort seconds)
    {
        currentTime = seconds;
        isRunning = true;
        if (timerText != null) timerText.gameObject.SetActive(true);
    }

    public void Stop()
    {
        isRunning = false;
        Hide();
    }

    public void Hide()
    {
        if (timerText != null) timerText.gameObject.SetActive(false);
    }

    public float CurrentTime => currentTime;
    public bool IsRunning => isRunning;
}
