using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Launcher Android BYOG (Bring Your Own Game) embutido no APK.
/// Usa os AssetBundles convertidos para Android (utl_bundles) e abre o seletor nativo
/// de arquivos do Android (SAF) para escolher o .zip direto de Downloads sem permissoes extras.
/// </summary>
public class UTLAndroidBYOGLauncher : MonoBehaviour
{
    private const string PREF_ALWAYS_SHOW = "UTL_Launcher_AlwaysShow";
    private const string PREF_PERF_MODE = "UTL_Launcher_PerfMode"; // 0=AUTO, 1=60FPS, 2=30FPS_2GB

    private static UTLAndroidBYOGLauncher _instance;
    private static TaskCompletionSource<bool> _readyTcs;

    private Canvas _canvas;
    private TextMeshProUGUI _statusText;
    private TextMeshProUGUI _pathHintText;
    private TextMeshProUGUI _progressText;
    private Image _progressFill;
    private Button _btnPlay;
    private Button _btnImportZip;
    private Button _btnRescan;
    private Button _btnPerfMode;
    private Button _btnCharmScale;
    private Button _btnAlwaysShow;

    private volatile bool _isExtracting;
    private volatile float _extractProgress;
    private volatile string _extractStatus = "";
    private volatile bool _extractDoneFlag;
    private volatile bool _extractSuccess;

    public static string ExternalBundlesDir
    {
        get { return Path.Combine(Application.persistentDataPath, "utl_bundles"); }
    }

    public static void CleanupOldPCBundlesIfPresent()
    {
        try
        {
            string oldPcDir = Path.Combine(Application.persistentDataPath, "AssetBundles");
            if (Directory.Exists(oldPcDir))
            {
                Directory.Delete(oldPcDir, true);
                Debug.Log("[UTL BYOG] Pasta antiga AssetBundles (PC BuildTarget 5) removida.");
            }
        }
        catch { }
    }

    public static bool HasValidExternalBundles()
    {
        try
        {
            string dir = ExternalBundlesDir;
            if (!Directory.Exists(dir)) return false;
            return File.Exists(Path.Combine(dir, "stage_common"))
                || File.Exists(Path.Combine(dir, "gui_dialogue"));
        }
        catch { return false; }
    }

    public static bool HasEmbeddedStreamingBundles()
    {
        try
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (AndroidJavaClass up = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject act = up.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject am = act.Call<AndroidJavaObject>("getAssets"))
            {
                string[] list = am.Call<string[]>("list", "utl_bundles");
                return list != null && list.Length >= 3;
            }
#else
            string uDir = Path.Combine(Application.streamingAssetsPath, "utl_bundles");
            if (Directory.Exists(uDir) && Directory.GetFiles(uDir).Length >= 3) return true;
            string sDir = Path.Combine(Application.streamingAssetsPath, "AssetBundles", "StandaloneWindows");
            return Directory.Exists(sDir) && Directory.GetFiles(sDir).Length >= 3;
#endif
        }
        catch { return false; }
    }

    public static bool HasAnyReadyBundles()
    {
        return HasValidExternalBundles() || HasEmbeddedStreamingBundles();
    }

    public static void ApplySavedPerformanceProfile()
    {
        int mode = PlayerPrefs.GetInt(PREF_PERF_MODE, 0);
        if (mode == 1)
        {
            Application.targetFrameRate = 60;
            QualitySettings.masterTextureLimit = 0;
        }
        else if (mode == 2)
        {
            Application.targetFrameRate = 30;
            QualitySettings.masterTextureLimit = 1;
        }
    }

    public static Task WaitUntilReadyAsync()
    {
        CleanupOldPCBundlesIfPresent();
        ApplySavedPerformanceProfile();
        bool alwaysShow = PlayerPrefs.GetInt(PREF_ALWAYS_SHOW, 0) == 1;
        bool hasBundles = HasAnyReadyBundles();

        if (hasBundles && !alwaysShow)
        {
            return Task.CompletedTask;
        }

        if (_readyTcs != null && !_readyTcs.Task.IsCompleted)
        {
            return _readyTcs.Task;
        }

        _readyTcs = new TaskCompletionSource<bool>();
        GameObject go = new GameObject("UTLAndroidBYOGLauncher");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<UTLAndroidBYOGLauncher>();
        return _readyTcs.Task;
    }

    private void Start()
    {
        EnsureEventSystem();
        BuildUI();
        RefreshStatus();
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current == null && FindObjectOfType<EventSystem>() == null)
        {
            GameObject es = new GameObject("LauncherEventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
    }

    private void Update()
    {
        if (_isExtracting)
        {
            if (_progressFill != null)
            {
                _progressFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(_extractProgress), 1f);
            }
            if (_progressText != null)
            {
                _progressText.text = _extractStatus;
            }
        }

        if (_extractDoneFlag)
        {
            _extractDoneFlag = false;
            _isExtracting = false;
            SetButtonsInteractable(true);
            RefreshStatus();
            if (_extractSuccess)
            {
                UTLAssetManager.ResetDirectManifestForBYOG();
            }
        }
    }

    // Callbacks chamados por UTLZipPickerFragment.java via UnitySendMessage
    public void OnNativeExtractProgress(string payload)
    {
        _isExtracting = true;
        int sep = payload.IndexOf('|');
        if (sep > 0)
        {
            int cnt = 0;
            int.TryParse(payload.Substring(0, sep), out cnt);
            _extractProgress = Mathf.Clamp(cnt / 120f, 0.05f, 0.95f);
            _extractStatus = payload.Substring(sep + 1);
        }
        else
        {
            _extractStatus = payload;
        }
    }

    public void OnNativeExtractDone(string countStr)
    {
        _extractProgress = 1f;
        _extractStatus = "Extracao concluida (" + countStr + " arquivos)!";
        _extractSuccess = true;
        _extractDoneFlag = true;
    }

    public void OnNativeExtractError(string err)
    {
        _extractProgress = 0f;
        _extractStatus = err;
        _extractSuccess = false;
        _extractDoneFlag = true;
    }

    public void OnNativeExtractCancelled(string _)
    {
        _extractStatus = "Selecao cancelada.";
        _extractSuccess = false;
        _extractDoneFlag = true;
    }

    private void BuildUI()
    {
        GameObject cGo = new GameObject("UTLBYOGLauncherCanvas");
        cGo.transform.SetParent(transform, false);
        _canvas = cGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 500;

        CanvasScaler scaler = cGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        cGo.AddComponent<GraphicRaycaster>();

        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(cGo.transform, false);
        StretchFill(bg.AddComponent<RectTransform>());
        bg.AddComponent<Image>().color = new Color(0.08f, 0.10f, 0.20f, 1f);

        GameObject card = new GameObject("Card");
        card.transform.SetParent(bg.transform, false);
        RectTransform crt = card.AddComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.08f, 0.06f);
        crt.anchorMax = new Vector2(0.92f, 0.94f);
        crt.offsetMin = Vector2.zero;
        crt.offsetMax = Vector2.zero;
        card.AddComponent<Image>().color = new Color(0.12f, 0.16f, 0.30f, 0.98f);
        Outline border = card.AddComponent<Outline>();
        border.effectColor = new Color(0.36f, 0.80f, 1f, 1f);
        border.effectDistance = new Vector2(4f, -4f);

        GameObject header = new GameObject("Header");
        header.transform.SetParent(card.transform, false);
        RectTransform hrt = header.AddComponent<RectTransform>();
        hrt.anchorMin = new Vector2(0f, 1f);
        hrt.anchorMax = new Vector2(1f, 1f);
        hrt.pivot = new Vector2(0.5f, 1f);
        hrt.sizeDelta = new Vector2(0f, 92f);
        header.AddComponent<Image>().color = new Color(0.17f, 0.24f, 0.44f, 1f);

        TextMeshProUGUI title = CreateTMP(header.transform, "Title", "STEVEN UNIVERSE: UNLEASH THE LIGHT — ANDROID LAUNCHER", 26f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
        RectTransform trt = title.rectTransform;
        trt.anchorMin = new Vector2(0.03f, 0.42f);
        trt.anchorMax = new Vector2(0.97f, 0.95f);
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        TextMeshProUGUI sub = CreateTMP(header.transform, "Sub", "Toque em SELECIONAR ZIP para importar os bundles Android (UTL_Android_Bundles.zip)", 17f, TextAlignmentOptions.Center, new Color(0.75f, 0.92f, 1f, 1f), FontStyles.Normal);
        RectTransform srt = sub.rectTransform;
        srt.anchorMin = new Vector2(0.03f, 0.06f);
        srt.anchorMax = new Vector2(0.97f, 0.44f);
        srt.offsetMin = Vector2.zero;
        srt.offsetMax = Vector2.zero;

        _statusText = CreateTMP(card.transform, "Status", "Verificando dados...", 22f, TextAlignmentOptions.Center, new Color(1f, 0.92f, 0.45f, 1f), FontStyles.Bold);
        RectTransform stRt = _statusText.rectTransform;
        stRt.anchorMin = new Vector2(0.04f, 0.70f);
        stRt.anchorMax = new Vector2(0.96f, 0.83f);
        stRt.offsetMin = Vector2.zero;
        stRt.offsetMax = Vector2.zero;

        _pathHintText = CreateTMP(
            card.transform,
            "Hint",
            "Destino dos bundles Android:\n" + ExternalBundlesDir,
            15f,
            TextAlignmentOptions.Center,
            new Color(0.80f, 0.86f, 0.98f, 1f),
            FontStyles.Normal
        );
        RectTransform hintRt = _pathHintText.rectTransform;
        hintRt.anchorMin = new Vector2(0.04f, 0.56f);
        hintRt.anchorMax = new Vector2(0.96f, 0.70f);
        hintRt.offsetMin = Vector2.zero;
        hintRt.offsetMax = Vector2.zero;
        _pathHintText.enableWordWrapping = true;

        GameObject barBg = new GameObject("ProgressBg");
        barBg.transform.SetParent(card.transform, false);
        RectTransform bbrt = barBg.AddComponent<RectTransform>();
        bbrt.anchorMin = new Vector2(0.06f, 0.49f);
        bbrt.anchorMax = new Vector2(0.94f, 0.54f);
        bbrt.offsetMin = Vector2.zero;
        bbrt.offsetMax = Vector2.zero;
        barBg.AddComponent<Image>().color = new Color(0.06f, 0.08f, 0.16f, 1f);

        GameObject barFill = new GameObject("ProgressFill");
        barFill.transform.SetParent(barBg.transform, false);
        RectTransform bfrt = barFill.AddComponent<RectTransform>();
        bfrt.anchorMin = Vector2.zero;
        bfrt.anchorMax = new Vector2(0f, 1f);
        bfrt.offsetMin = Vector2.zero;
        bfrt.offsetMax = Vector2.zero;
        _progressFill = barFill.AddComponent<Image>();
        _progressFill.color = new Color(0.24f, 0.84f, 0.56f, 1f);

        _progressText = CreateTMP(barBg.transform, "ProgressTxt", "", 15f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
        StretchFill(_progressText.rectTransform);

        GameObject rowImport = new GameObject("RowImport");
        rowImport.transform.SetParent(card.transform, false);
        RectTransform riRt = rowImport.AddComponent<RectTransform>();
        riRt.anchorMin = new Vector2(0.06f, 0.36f);
        riRt.anchorMax = new Vector2(0.94f, 0.46f);
        riRt.offsetMin = Vector2.zero;
        riRt.offsetMax = Vector2.zero;
        HorizontalLayoutGroup h1 = rowImport.AddComponent<HorizontalLayoutGroup>();
        h1.spacing = 14f;
        h1.childControlWidth = true;
        h1.childControlHeight = true;
        h1.childForceExpandWidth = true;
        h1.childForceExpandHeight = true;

        _btnImportZip = CreateButton(rowImport.transform, "SELECIONAR ZIP (ANDROID)", new Color(0.20f, 0.52f, 0.88f, 1f), OnImportZipClicked);
        _btnRescan = CreateButton(rowImport.transform, "VERIFICAR PASTA", new Color(0.32f, 0.38f, 0.58f, 1f), RefreshStatus);

        GameObject rowOpts = new GameObject("RowOpts");
        rowOpts.transform.SetParent(card.transform, false);
        RectTransform roRt = rowOpts.AddComponent<RectTransform>();
        roRt.anchorMin = new Vector2(0.06f, 0.22f);
        roRt.anchorMax = new Vector2(0.94f, 0.32f);
        roRt.offsetMin = Vector2.zero;
        roRt.offsetMax = Vector2.zero;
        HorizontalLayoutGroup h2 = rowOpts.AddComponent<HorizontalLayoutGroup>();
        h2.spacing = 12f;
        h2.childControlWidth = true;
        h2.childControlHeight = true;
        h2.childForceExpandWidth = true;
        h2.childForceExpandHeight = true;

        _btnPerfMode = CreateButton(rowOpts.transform, "PERFIL: AUTO", new Color(0.24f, 0.30f, 0.50f, 1f), OnCyclePerfMode);
        _btnCharmScale = CreateButton(rowOpts.transform, "CAIXAS: NORMAL", new Color(0.24f, 0.30f, 0.50f, 1f), OnCycleCharmScale);
        _btnAlwaysShow = CreateButton(rowOpts.transform, "ABRIR SEMPRE: NAO", new Color(0.24f, 0.30f, 0.50f, 1f), OnToggleAlwaysShow);

        GameObject playRow = new GameObject("PlayRow");
        playRow.transform.SetParent(card.transform, false);
        RectTransform prRt = playRow.AddComponent<RectTransform>();
        prRt.anchorMin = new Vector2(0.22f, 0.05f);
        prRt.anchorMax = new Vector2(0.78f, 0.17f);
        prRt.offsetMin = Vector2.zero;
        prRt.offsetMax = Vector2.zero;
        _btnPlay = CreateButton(playRow.transform, "JOGAR", new Color(0.94f, 0.22f, 0.48f, 1f), OnPlayClicked);
        StretchFill(_btnPlay.GetComponent<RectTransform>());
        TextMeshProUGUI playLabel = _btnPlay.GetComponentInChildren<TextMeshProUGUI>();
        if (playLabel != null) playLabel.fontSize = 28f;

        RefreshOptionLabels();
    }

    private void RefreshOptionLabels()
    {
        int perf = PlayerPrefs.GetInt(PREF_PERF_MODE, 0);
        string[] perfNames = { "PERFIL: AUTO", "PERFIL: 60 FPS", "PERFIL: 2GB / 30 FPS" };
        SetBtnText(_btnPerfMode, perfNames[Mathf.Clamp(perf, 0, 2)]);
        SetBtnText(_btnCharmScale, "CAIXAS: " + UTLCharmBoxScaler.CurrentLabel);
        bool always = PlayerPrefs.GetInt(PREF_ALWAYS_SHOW, 0) == 1;
        SetBtnText(_btnAlwaysShow, always ? "ABRIR SEMPRE: SIM" : "ABRIR SEMPRE: NAO");
    }

    private void RefreshStatus()
    {
        bool ext = HasValidExternalBundles();
        bool emb = HasEmbeddedStreamingBundles();
        if (ext)
        {
            int count = Directory.GetFiles(ExternalBundlesDir).Length;
            _statusText.text = string.Format("PRONTO PARA JOGAR! ({0} bundles Android importados)", count);
            _statusText.color = new Color(0.32f, 0.94f, 0.62f, 1f);
            _btnPlay.interactable = true;
            _progressFill.rectTransform.anchorMax = Vector2.one;
            _progressText.text = "Bundles Android validados";
        }
        else if (emb)
        {
            _statusText.text = "PRONTO PARA JOGAR! (Bundles embutidos no APK)";
            _statusText.color = new Color(0.32f, 0.94f, 0.62f, 1f);
            _btnPlay.interactable = true;
            _progressFill.rectTransform.anchorMax = Vector2.one;
            _progressText.text = "Modo APK completo";
        }
        else
        {
            _statusText.text = "AGUARDANDO BUNDLES ANDROID (UTL_Android_Bundles.zip)";
            _statusText.color = new Color(1f, 0.80f, 0.30f, 1f);
            _btnPlay.interactable = false;
            _progressFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            _progressText.text = "Toque em SELECIONAR ZIP para importar";
        }
    }

    private void SetButtonsInteractable(bool state)
    {
        if (_btnImportZip != null) _btnImportZip.interactable = state;
        if (_btnRescan != null) _btnRescan.interactable = state;
        if (_btnPlay != null) _btnPlay.interactable = state && HasAnyReadyBundles();
    }

    private void OnImportZipClicked()
    {
        if (_isExtracting) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            SetButtonsInteractable(false);
            _isExtracting = true;
            _extractProgress = 0.02f;
            _extractStatus = "Abrindo seletor de arquivos do Android...";
            using (AndroidJavaClass cls = new AndroidJavaClass("com.utlport.launcher.UTLZipPickerFragment"))
            {
                cls.CallStatic("openZipPicker", ExternalBundlesDir, gameObject.name);
            }
            return;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[UTL BYOG] Seletor nativo indisponivel, usando fallback local: " + ex.Message);
            _isExtracting = false;
            SetButtonsInteractable(true);
        }
#endif
        // Fallback local (Editor ou caso o seletor nativo falhe)
        string[] names = { "UTL_Android_Bundles.zip", "UTL_AssetBundles.zip", "utl_bundles.zip" };
        string found = null;
        foreach (string n in names)
        {
            string p = Path.Combine(Application.persistentDataPath, n);
            if (File.Exists(p)) { found = p; break; }
        }
        if (string.IsNullOrEmpty(found))
        {
            _progressText.text = "Coloque UTL_Android_Bundles.zip em " + Application.persistentDataPath;
            return;
        }
        ExtractZipFallback(found);
    }

    private void ExtractZipFallback(string zipPath)
    {
        string destDir = ExternalBundlesDir;
        _isExtracting = true;
        _extractProgress = 0f;
        _extractStatus = "Abrindo " + Path.GetFileName(zipPath) + "...";
        SetButtonsInteractable(false);

        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                Directory.CreateDirectory(destDir);
                using (FileStream zipFs = File.OpenRead(zipPath))
                using (ZipArchive archive = new ZipArchive(zipFs, ZipArchiveMode.Read))
                {
                    int total = Math.Max(1, archive.Entries.Count);
                    int done = 0;
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        done++;
                        if (string.IsNullOrEmpty(entry.Name) || entry.Name.EndsWith(".meta")) continue;
                        string outFile = Path.Combine(destDir, entry.Name);
                        using (Stream es = entry.Open())
                        using (FileStream ofs = File.Create(outFile))
                        {
                            es.CopyTo(ofs);
                        }
                        _extractProgress = (float)done / total;
                        _extractStatus = string.Format("Extraindo ({0}/{1}): {2}", done, total, entry.Name);
                    }
                }
                _extractSuccess = true;
                _extractProgress = 1f;
                _extractStatus = "Extracao concluida!";
            }
            catch (Exception ex)
            {
                _extractSuccess = false;
                _extractStatus = "Erro ao extrair: " + ex.Message;
            }
            finally
            {
                _extractDoneFlag = true;
            }
        });
    }

    private void OnCyclePerfMode()
    {
        int next = (PlayerPrefs.GetInt(PREF_PERF_MODE, 0) + 1) % 3;
        PlayerPrefs.SetInt(PREF_PERF_MODE, next);
        PlayerPrefs.Save();
        ApplySavedPerformanceProfile();
        RefreshOptionLabels();
    }

    private void OnCycleCharmScale()
    {
        UTLCharmBoxScaler.CycleMode();
        RefreshOptionLabels();
    }

    private void OnToggleAlwaysShow()
    {
        int next = PlayerPrefs.GetInt(PREF_ALWAYS_SHOW, 0) == 1 ? 0 : 1;
        PlayerPrefs.SetInt(PREF_ALWAYS_SHOW, next);
        PlayerPrefs.Save();
        RefreshOptionLabels();
    }

    private void OnPlayClicked()
    {
        if (!HasAnyReadyBundles()) return;
        ApplySavedPerformanceProfile();
        UTLAssetManager.ResetDirectManifestForBYOG();
        if (_canvas != null) _canvas.gameObject.SetActive(false);
        var tcs = _readyTcs;
        Destroy(gameObject);
        _instance = null;
        if (tcs != null && !tcs.Task.IsCompleted)
        {
            tcs.TrySetResult(true);
        }
    }

    private Button CreateButton(Transform parent, string label, Color color, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
        TextMeshProUGUI txt = CreateTMP(go.transform, "Label", label, 18f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
        StretchFill(txt.rectTransform);
        return btn;
    }

    private void SetBtnText(Button btn, string text)
    {
        if (btn == null) return;
        TextMeshProUGUI t = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (t != null) t.text = text;
    }

    private TextMeshProUGUI CreateTMP(Transform parent, string name, string text, float size, TextAlignmentOptions align, Color color, FontStyles style)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void StretchFill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
