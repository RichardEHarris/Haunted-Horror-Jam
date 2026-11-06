using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    private WaveManager waveManager;
    [SerializeField] private GameObject healthBar;
    [SerializeField] private GameObject healthDisplay;
    [SerializeField] private GameObject currencyDisplay;
    [SerializeField] private GameObject waveDisplay;
    [SerializeField] private GameObject waveTitleDisplay;
    [SerializeField] private GameObject waveSkipButton;

    private RectTransform healthBarFill;
    private TMP_Text healthText;
    private TMP_Text currencyText;
    private TMP_Text waveCountdownText;
    private TMP_Text waveTitleText;
    private Button skipButton;
    private int displayedHealth = int.MinValue;
    private int displayedCurrency = int.MinValue;
    private int displayedWaveNumber = int.MinValue;
    private int displayedCountdown = int.MinValue;
    private string displayedWaveTitle;
    private bool displayedCanSkip;

    private void Start()
    {
        if (gameManager == null)
        {
            gameManager = FindAnyObjectByType<GameManager>();
        }

        if (gameManager == null)
        {
            Debug.LogError("UIManager requires a GameManager.", this);
            return;
        }

        waveManager = gameManager.GetComponent<WaveManager>();
        if (waveManager == null)
        {
            waveManager = FindAnyObjectByType<WaveManager>();
        }

        if (waveManager == null)
        {
            Debug.LogError("UIManager requires a WaveManager.", this);
        }

        if (healthBar == null)
        {
            Debug.LogError("Health bar UI element is not assigned.", this);
        }
        else
        {
            healthBarFill = healthBar.GetComponent<RectTransform>();
            if (healthBarFill == null)
            {
                Debug.LogError("Health bar UI element must have a RectTransform component.", healthBar);
            }
        }

        healthText = GetTextComponent(healthDisplay, "health display");

        currencyText = GetTextComponent(currencyDisplay, "currency display");
        waveCountdownText = GetTextComponent(waveDisplay, "wave countdown display");
        waveTitleText = GetTextComponent(waveTitleDisplay, "wave title display");

        if (waveSkipButton == null)
        {
            Debug.LogError("Wave skip button is not assigned.", this);
        }
        else
        {
            skipButton = waveSkipButton.GetComponent<Button>();
            if (skipButton == null)
            {
                Debug.LogError("Wave skip button object must have a Button component.", waveSkipButton);
            }
            else
            {
                skipButton.interactable = false;
                if (waveManager != null)
                {
                    skipButton.onClick.AddListener(waveManager.SkipWaveTimer);
                }
            }
        }

        RefreshHealth();
        RefreshCurrency();
        RefreshWaveDisplay();
    }

    private void Update()
    {
        if (gameManager == null)
        {
            return;
        }

        RefreshHealth();
        RefreshCurrency();
        RefreshWaveDisplay();
    }

    private static TMP_Text GetTextComponent(GameObject target, string label)
    {
        if (target == null)
        {
            Debug.LogError($"The {label} UI element is not assigned.");
            return null;
        }

        TMP_Text text = target.GetComponent<TMP_Text>();
        if (text == null)
        {
            Debug.LogError($"The {label} UI element must use TextMeshPro.", target);
        }

        return text;
    }

    private void RefreshHealth()
    {
        int currentHealth = Mathf.CeilToInt(gameManager.Health);
        if (currentHealth != displayedHealth)
        {
            displayedHealth = currentHealth;
            if (healthText != null)
            {
                healthText.text = currentHealth.ToString();
            }
        }

        float healthBarWidth = healthBar.transform.parent.GetComponent<RectTransform>()?.sizeDelta.x ?? 200f;
        if (healthBarFill != null)
        {
            healthBarFill.sizeDelta = new Vector2(Mathf.Clamp01(gameManager.Health / gameManager.MaxHealth) * healthBarWidth, healthBarFill.sizeDelta.y);
        }
    }

    private void RefreshCurrency()
    {
        if (currencyText == null || displayedCurrency == gameManager.Currency)
        {
            return;
        }

        displayedCurrency = gameManager.Currency;
        currencyText.text = displayedCurrency.ToString();
    }

    private void RefreshWaveDisplay()
    {
        if (waveManager == null)
        {
            return;
        }

        if (waveTitleText != null
            && (displayedWaveNumber != waveManager.CurrentWaveNumber
                || displayedWaveTitle != waveManager.CurrentWaveTitle))
        {
            displayedWaveNumber = waveManager.CurrentWaveNumber;
            displayedWaveTitle = waveManager.CurrentWaveTitle;
            waveTitleText.text = displayedWaveNumber > 0
                ? $"Wave {displayedWaveNumber}: {displayedWaveTitle}"
                : "Waiting for wave";
        }

        int remainingSeconds = Mathf.CeilToInt(waveManager.TimeUntilNextWave);
        if (waveCountdownText != null && displayedCountdown != remainingSeconds)
        {
            displayedCountdown = remainingSeconds;
            waveCountdownText.text = waveManager.CanSkipWaveTimer
                ? $"Next wave: {remainingSeconds / 60:00}:{remainingSeconds % 60:00}"
                : waveManager.CurrentWaveNumber > 0 ? "Final wave" : "Waiting";
        }

        bool canSkip = waveManager.CanSkipWaveTimer;
        if (skipButton != null && displayedCanSkip != canSkip)
        {
            displayedCanSkip = canSkip;
            skipButton.interactable = canSkip;
        }
    }

    private void OnDestroy()
    {
        if (skipButton != null && waveManager != null)
        {
            skipButton.onClick.RemoveListener(waveManager.SkipWaveTimer);
        }
    }
}
