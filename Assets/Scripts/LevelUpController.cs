using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpController : MonoBehaviour
{
    [SerializeField] private GameObject VFX_effectShine;
    [SerializeField] private GameObject VFX_effectFlare;
    [Header("Shape Visuals")]
    [SerializeField] private BrickSet brickSet;
    [SerializeField] private Image imgShape;
    [SerializeField] private Text textShape;
    [SerializeField] private Text textContinue;
    [Header("Timings")]
    [SerializeField] private float continueFadeDuration = 0.6f;
    [SerializeField] private float panelShownAlphaThreshold = 0.95f;
    [SerializeField] private bool closeOnAnyTap = true;
    [Header("VFX Wait (seconds)")]
    [SerializeField] private float shineMaxWait = 2.0f;
    [SerializeField] private float flareMaxWait = 2.0f;
    [Header("Shape Transition")]
    [SerializeField] private float shapeShrinkDuration = 0.25f;
    [SerializeField] private float shapeGrowDuration = 0.35f;
    [SerializeField] private float shapeMinScale = 0.3f;

    private CanvasGroup _continueCg;
    private bool _readyToClose;

    private void Awake()
    {
        // Ensure continue text has a CanvasGroup for fading
        if (textContinue != null)
        {
            _continueCg = textContinue.GetComponent<CanvasGroup>();
            if (_continueCg == null) _continueCg = textContinue.gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void OnEnable()
    {
        // Initialize UI state each time the panel becomes active
        SafeSetActive(VFX_effectShine, false);
        SafeSetActive(VFX_effectFlare, false);
        _readyToClose = false;
        if (_continueCg != null)
        {
            _continueCg.alpha = 0f;
            _continueCg.interactable = false;
            _continueCg.blocksRaycasts = false;
        }
        SetupInitialShapeVisuals();
        RunSequenceAsync().Forget();
    }

    private void SetupInitialShapeVisuals()
    {
        // Start by showing the OLD mission (achieved) value to transition from
        if (MissionController.Instance == null) return;
        int newMission = MissionController.Instance.CurrentMission;
        int oldMission = Mathf.Max(2, newMission / 2);
        if (imgShape != null)
        {
            Sprite oldSprite = null;
            if (brickSet != null)
                oldSprite = brickSet.GetSprite(oldMission);
            if (oldSprite == null)
                oldSprite = MissionController.Instance.GetCurrentMissionSprite(); // fallback to new sprite
            imgShape.sprite = oldSprite;
            imgShape.color = new Color(imgShape.color.r, imgShape.color.g, imgShape.color.b, 1f);
            imgShape.rectTransform.localScale = Vector3.one;
        }
        if (textShape != null)
        {
            textShape.text = oldMission.ToString();
            var c = textShape.color; c.a = 1f; textShape.color = c;
            textShape.rectTransform.localScale = Vector3.one;
        }
    }

    private async UniTaskVoid RunSequenceAsync()
    {
        var ct = this.GetCancellationTokenOnDestroy();
        // Prefer an explicit flag from PopupController to know when its fade-in completes
        var popup = PopupController.Instance;
        bool waited = false;
        if (popup != null)
        {
            float timeout = 2.0f; float start = Time.unscaledTime;
            await UniTask.WaitUntil(() => popup.LevelUpShown || Time.unscaledTime - start > timeout,
                cancellationToken: ct);
            waited = true;
        }
        if (!waited)
        {
            // Fallback: Wait until the local CanvasGroup is fully shown
            var cg = GetComponent<CanvasGroup>();
            if (cg != null)
            {
                float timeout = 2.0f; float start = Time.unscaledTime;
                await UniTask.WaitUntil(() => cg.alpha >= panelShownAlphaThreshold || Time.unscaledTime - start > timeout,
                    cancellationToken: ct);
            }
            else
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(0.5f), ignoreTimeScale: true, cancellationToken: ct);
            }
        }

        // First, run the shape transition from OLD mission -> NEW mission
        await RunShapeTransitionAsync(ct);

        // Activate and play VFX after shape transition and popup shown
        await PlayVfxAsync(VFX_effectShine, shineMaxWait, ct);
        await PlayVfxAsync(VFX_effectFlare, flareMaxWait, ct);

        // Fade in the continue text after both effects complete
        if (textContinue != null) textContinue.gameObject.SetActive(true);
        if (_continueCg != null)
        {
            _continueCg.DOKill();
            await _continueCg.DOFade(1f, continueFadeDuration).SetUpdate(true).ToUniTask(cancellationToken: ct);
            _continueCg.interactable = true;
            _continueCg.blocksRaycasts = true;
            _readyToClose = true;
        }
    }

    private async UniTask RunShapeTransitionAsync(System.Threading.CancellationToken ct)
    {
        if (MissionController.Instance == null || imgShape == null || textShape == null)
            return;
        int newMission = MissionController.Instance.CurrentMission;
        int oldMission = Mathf.Max(2, newMission / 2);
        // Shrink and fade out old visuals
        var img = imgShape;
        var txt = textShape;
        img.DOKill(); txt.DOKill();
        // Parallel shrink/fade
        var imgShrink = img.rectTransform.DOScale(shapeMinScale, shapeShrinkDuration).SetUpdate(true).ToUniTask(cancellationToken: ct);
        var imgFade = img.DOFade(0f, shapeShrinkDuration).SetUpdate(true).ToUniTask(cancellationToken: ct);
        var txtShrink = txt.rectTransform.DOScale(shapeMinScale, shapeShrinkDuration).SetUpdate(true).ToUniTask(cancellationToken: ct);
        var txtFade = txt.DOFade(0f, shapeShrinkDuration).SetUpdate(true).ToUniTask(cancellationToken: ct);
        await UniTask.WhenAll(imgShrink, imgFade, txtShrink, txtFade);

        // Swap to NEW mission visuals
        Sprite newSprite = MissionController.Instance.GetCurrentMissionSprite();
        if (newSprite != null) img.sprite = newSprite;
        txt.text = newMission.ToString();

        // Grow and fade in to full
        img.DOKill(); txt.DOKill();
        img.rectTransform.localScale = Vector3.one * shapeMinScale;
        txt.rectTransform.localScale = Vector3.one * shapeMinScale;
        var ic = img.color; ic.a = 0f; img.color = ic;
        var tc = txt.color; tc.a = 0f; txt.color = tc;
        var imgGrow = img.rectTransform.DOScale(1f, shapeGrowDuration).SetEase(Ease.OutBack).SetUpdate(true).ToUniTask(cancellationToken: ct);
        var imgIn = img.DOFade(1f, shapeGrowDuration).SetUpdate(true).ToUniTask(cancellationToken: ct);
        var txtGrow = txt.rectTransform.DOScale(1f, shapeGrowDuration).SetEase(Ease.OutBack).SetUpdate(true).ToUniTask(cancellationToken: ct);
        var txtIn = txt.DOFade(1f, shapeGrowDuration).SetUpdate(true).ToUniTask(cancellationToken: ct);
        await UniTask.WhenAll(imgGrow, imgIn, txtGrow, txtIn);
    }

    private async UniTask PlayVfxAsync(GameObject go, float maxWaitSeconds, System.Threading.CancellationToken ct)
    {
        if (go == null) return;
        SafeSetActive(go, true);
        var particles = go.GetComponentsInChildren<ParticleSystem>(true);
        if (particles != null && particles.Length > 0)
        {
            foreach (var ps in particles)
            {
                if (ps == null) continue;
                ps.Clear(true);
                ps.Play(true);
            }
            // Wait until all particles finish or timeout to avoid blocking on looping effects
            float start = Time.unscaledTime;
            await UniTask.WaitUntil(() =>
            {
                bool anyAlive = false;
                foreach (var ps in particles)
                {
                    if (ps != null && ps.IsAlive(true)) { anyAlive = true; break; }
                }
                return !anyAlive || (Time.unscaledTime - start) >= maxWaitSeconds;
            }, PlayerLoopTiming.Update, ct);
        }
        else
        {
            // If there are no particle systems, keep it visible for a brief moment
            await UniTask.Delay(System.TimeSpan.FromSeconds(0.3f), ignoreTimeScale: true, cancellationToken: ct);
        }
    }

    private void SafeSetActive(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active) go.SetActive(active);
    }

    private void Update()
    {
        if (!closeOnAnyTap || !_readyToClose) return;
        bool tapped = false;
        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began) tapped = true;
        }
        else if (Input.GetMouseButtonDown(0))
        {
            tapped = true;
        }
        if (tapped)
        {
            PopupController.Instance?.HideLevelUpPopup();
            _readyToClose = false;
        }
    }

}
