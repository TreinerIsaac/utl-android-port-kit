using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Submenu nativo de Conquistas (estilo Steam / Unleash the Light) exibido na TitleScreen.
/// Carrega icones sob demanda (zero impacto em RAM/FPS fora da TitleScreen) e sincroniza
/// conquistas dos Save Slots existentes + desbloqueios em tempo real no Android.
/// </summary>
public class UTLAchievementsMenuOverlay : MonoBehaviour
{
    private static UTLAchievementsMenuOverlay _instance;

    private const string PREF_DONE_PREFIX = "UTL_ACH_DONE_";
    private const string PREF_CNT_PREFIX = "UTL_ACH_CNT_";

    private static readonly Dictionary<string, string> IconFileMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "gui_settings_mainmenu_achievements", "GUI_Settings_MainMenu_Achievements" },
        { "utl_achieve_bannerborder", "UTL_Achieve_BannerBorder" },
        { "utl_achieveicon_acrosstheuniverse", "UTL_AchieveIcon_AcrossTheUniverse" },
        { "utl_achieveicon_attackedsavedunleashed", "UTL_AchieveIcon_AttackedSavedUnleashed" },
        { "utl_achieveicon_attackthelight", "UTL_AchieveIcon_AttackTheLight" },
        { "utl_achieveicon_bethereorbesquare", "UTL_AchieveIcon_BeThereOrBeSquare" },
        { "utl_achieveicon_cantbelievewevecomesofar", "UTL_AchieveIcon_CantBelieveWeveComeSoFar" },
        { "utl_achieveicon_captainuniverse", "UTL_AchieveIcon_CaptainUniverse" },
        { "utl_achieveicon_clodgoals", "UTL_AchieveIcon_ClodGoals" },
        { "utl_achieveicon_cookiecatrecall", "UTL_AchieveIcon_CookieCatRecall" },
        { "utl_achieveicon_cryingbreakfastfriend", "UTL_Achieveicon_CryingBreakfastFriend" },
        { "utl_achieveicon_era3winningspree", "UTL_AchieveIcon_Era3WinningSpree" },
        { "utl_achieveicon_giantwoman", "UTL_AchieveIcon_GiantWoman" },
        { "utl_achieveicon_gracedbyherpresence", "UTL_AchieveIcon_GracedByHerPresence" },
        { "utl_achieveicon_greenthumb", "UTL_AchieveIcon_GreenThumb" },
        { "utl_achieveicon_itsasecrettoeverybody", "UTL_AchieveIcon_ItsASecretToEverybody" },
        { "utl_achieveicon_itsawash", "UTL_AchieveIcon_ItsAWash" },
        { "utl_achieveicon_jamwithyouroldman", "UTL_AchieveIcon_JamWithYourOldMan" },
        { "utl_achieveicon_justperfect", "UTL_AchieveIcon_JustPerfect" },
        { "utl_achieveicon_katvalryhere", "UTL_Achieveicon_KatvalryHere" },
        { "utl_achieveicon_keysarethekeyyousee", "UTL_AchieveIcon_KeysAreTheKeyYouSee" },
        { "utl_achieveicon_littlehomeschool", "UTL_Achieveicon_LittleHomeSchool" },
        { "utl_achieveicon_over9000", "UTL_AchieveIcon_Over9000" },
        { "utl_achieveicon_perfectporkchop", "UTL_AchieveIcon_PerfectPorkchop" },
        { "utl_achieveicon_preciousstone", "UTL_AchieveIcon_PreciousStone" },
        { "utl_achieveicon_prismaticpro", "UTL_Achieveicon_PrismaticPro" },
        { "utl_achieveicon_riskybismuth", "UTL_AchieveIcon_RiskyBismuth" },
        { "utl_achieveicon_rockthevote", "UTL_AchieveIcon_RockTheVote" },
        { "utl_achieveicon_rosesroom", "UTL_AchieveIcon_RosesRoom" },
        { "utl_achieveicon_shortysquad", "UTL_AchieveIcon_ShortySquad" },
        { "utl_achieveicon_spacetraintothecosmos", "UTL_Achieveicon_SpaceTrainToTheCosmos" },
        { "utl_achieveicon_spectrumsnacks", "UTL_AchieveIcon_SpectrumSnacks" },
        { "utl_achieveicon_stevensrpg", "UTL_AchieveIcon_StevensRPG" },
        { "utl_achieveicon_theboxthatrocks", "UTL_AchieveIcon_TheBoxThatRocks" },
        { "utl_achieveicon_tinyfloatingwhale", "UTL_AchieveIcon_TinyFloatingWhale" },
        { "utl_achieveicon_toughstuff", "UTL_AchieveIcon_ToughStuff" },
    };

    private enum FilterMode { All, Unlocked, Locked }

    private Canvas _canvas;
    private GameObject _trophyBtnRoot;
    private TextMeshProUGUI _trophyBtnCountText;
    private GameObject _modalRoot;
    private TextMeshProUGUI _headerStatsText;
    private Image _progressFillImg;
    private RectTransform _listContent;
    private ScrollRect _scrollRect;
    private Button _btnTabAll;
    private Button _btnTabUnlocked;
    private Button _btnTabLocked;
    private FilterMode _currentFilter = FilterMode.All;

    private TMP_FontAsset _uiFont;
    private Sprite _trophySprite;
    private readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

    private static PropertyInfo _baAssetNameProp;
    private static FieldInfo _baAssetNameField;
    private static bool _baReflectionInit;

    private float _nextVisibilityCheck;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static UTLAchievementsMenuOverlay EnsureInstance()
    {
        if (_instance != null) return _instance;
        GameObject go = new GameObject("UTLAchievementsMenuOverlay");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<UTLAchievementsMenuOverlay>();
        return _instance;
    }

    /// <summary>
    /// Chamado por UTLAchievementsManager ao atualizar ou concluir qualquer conquista.
    /// </summary>
    public static void RecordProgress(int achievementId, int count, bool complete)
    {
        try
        {
            bool changed = false;
            string cntKey = PREF_CNT_PREFIX + achievementId;
            string doneKey = PREF_DONE_PREFIX + achievementId;

            int prevCnt = PlayerPrefs.GetInt(cntKey, 0);
            if (count > prevCnt)
            {
                PlayerPrefs.SetInt(cntKey, count);
                changed = true;
            }
            if (complete && PlayerPrefs.GetInt(doneKey, 0) == 0)
            {
                PlayerPrefs.SetInt(doneKey, 1);
                changed = true;
            }
            if (changed)
            {
                PlayerPrefs.Save();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[UTLAchievementsMenu] RecordProgress erro: " + ex.Message);
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (_instance == this) _instance = null;
    }

    private void Start()
    {
        EvaluateSceneVisibility(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EvaluateSceneVisibility(scene.name);
    }

    private bool IsTitleScene(string sceneName)
    {
        return !string.IsNullOrEmpty(sceneName)
            && sceneName.IndexOf("TitleScreen", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void EvaluateSceneVisibility(string sceneName)
    {
        bool onTitle = IsTitleScene(sceneName);
        if (!onTitle)
        {
            if (_modalRoot != null) _modalRoot.SetActive(false);
            if (_canvas != null) _canvas.gameObject.SetActive(false);
            return;
        }

        StopAllCoroutines();
        StartCoroutine(DelayedTitleInit());
    }

    private System.Collections.IEnumerator DelayedTitleInit()
    {
        yield return new WaitForSecondsRealtime(1.0f);
        if (!IsTitleScene(SceneManager.GetActiveScene().name)) yield break;
        SyncFromAllLocalSaveSlots();
        EnsureUI();
        if (_canvas != null) _canvas.gameObject.SetActive(true);
        UpdateTrophyBadgeCount();
    }

    private bool ShouldShowTrophyButton()
    {
        if (!IsTitleScene(SceneManager.GetActiveScene().name)) return false;

        // Esconde no Quarto da Rose (selecao de personagem faz parte da cena TitleScreen)
        UITitleScreenRosesRoomMenu rr = FindObjectOfType<UITitleScreenRosesRoomMenu>();
        if (rr != null && rr.gameObject.activeInHierarchy) return false;

        // Esconde se algum menu de configuracoes/loading/popup de transicao estiver cobrindo a tela
        Button[] btns = FindObjectsOfType<Button>();
        bool foundSettingsGear = false;
        for (int i = 0; i < btns.Length; i++)
        {
            Button b = btns[i];
            if (b == null || !b.gameObject.activeInHierarchy) continue;
            if (_canvas != null && b.transform.IsChildOf(_canvas.transform)) continue;
            string n = b.gameObject.name;
            if (n.IndexOf("Settings", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Gear", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Options", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                foundSettingsGear = true;
                break;
            }
        }
        return foundSettingsGear;
    }

    private void Update()
    {
        if (_canvas == null || !_canvas.gameObject.activeSelf) return;

        // Fecha o modal com o botao Voltar do Android / ESC
        if (_modalRoot != null && _modalRoot.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseModal();
            return;
        }

        if (Time.unscaledTime < _nextVisibilityCheck) return;
        _nextVisibilityCheck = Time.unscaledTime + 0.35f;

        if (_trophyBtnRoot != null && (_modalRoot == null || !_modalRoot.activeSelf))
        {
            bool showBtn = ShouldShowTrophyButton();
            if (_trophyBtnRoot.activeSelf != showBtn)
            {
                _trophyBtnRoot.SetActive(showBtn);
            }
        }
    }

    /// <summary>
    /// Le os 3 Save Slots locais (PlayerPrefs) e o PlayerInfo ativo para garantir
    /// que conquistas ja feitas em saves existentes aparecam desbloqueadas na hora.
    /// </summary>
    private void SyncFromAllLocalSaveSlots()
    {
        try
        {
            // 1. PlayerInfo ativo em memoria (se ja carregado)
            if (false && PlayerInfo.Instance != null && GameResources.Instance != null && GameResources.Instance.achievementDatabase != null)
            {
                List<UTLAchievement> list = GameResources.Instance.achievementDatabase.achievements;
                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        UTLAchievement a = list[i];
                        if (a == null) continue;
                        AchievementData d = PlayerInfo.Instance.GetAchievementData(a.id);
                        if (d != null && (d.achievementComplete || d.achievementCount > 0))
                        {
                            RecordProgress(a.id, d.achievementCount, d.achievementComplete);
                        }
                    }
                }
            }

            // 2. Varre o JSON salvo dos slots em PlayerPrefs ("SaveSlots")
            string slotsJson = PlayerPrefs.GetString("SaveSlots", "");
            if (!string.IsNullOrEmpty(slotsJson))
            {
                SaveSlotsData slotsData = new SaveSlotsData();
                JsonUtility.FromJsonOverwrite(slotsJson, slotsData);
                if (slotsData.slots != null)
                {
                    for (int i = 0; i < slotsData.slots.Count; i++)
                    {
                        string key = slotsData.slots[i].playerDataKey;
                        if (string.IsNullOrEmpty(key)) continue;
                        string raw = PlayerPrefs.GetString(key, "");
                        if (string.IsNullOrEmpty(raw)) continue;
                        ParseAchievementsFromRawSaveJson(raw);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[UTLAchievementsMenu] SyncFromAllLocalSaveSlots aviso: " + ex.Message);
        }
    }

    private static int ParseIntAfter(string s, int startIdx, int endIdx)
    {
        int colon = s.IndexOf(':', startIdx);
        if (colon < 0 || colon >= endIdx) return -1;
        int i = colon + 1;
        while (i < endIdx && (s[i] == ' ' || s[i] == '\t' || s[i] == '\r' || s[i] == '\n')) i++;
        if (i >= endIdx || s[i] < '0' || s[i] > '9') return -1;
        int val = 0;
        while (i < endIdx && s[i] >= '0' && s[i] <= '9')
        {
            val = val * 10 + (s[i] - '0');
            i++;
        }
        return val;
    }

    private void ParseAchievementsFromRawSaveJson(string rawJson)
    {
        if (string.IsNullOrEmpty(rawJson)) return;
        int pos = 0;
        int len = rawJson.Length;
        while (pos < len)
        {
            int idKey = rawJson.IndexOf("\"achievementID\"", pos, StringComparison.OrdinalIgnoreCase);
            if (idKey < 0) break;
            int closeBrace = rawJson.IndexOf('}', idKey);
            if (closeBrace < 0) closeBrace = Math.Min(len, idKey + 240);

            int id = ParseIntAfter(rawJson, idKey + 15, closeBrace);
            int cntKey = rawJson.IndexOf("\"achievementCount\"", idKey, closeBrace - idKey, StringComparison.OrdinalIgnoreCase);
            int cnt = (cntKey >= 0) ? ParseIntAfter(rawJson, cntKey + 18, closeBrace) : 0;

            bool done = false;
            int doneKey = rawJson.IndexOf("\"achievementComplete\"", idKey, closeBrace - idKey, StringComparison.OrdinalIgnoreCase);
            if (doneKey >= 0)
            {
                int tIdx = rawJson.IndexOf("true", doneKey + 21, Math.Min(16, closeBrace - (doneKey + 21)), StringComparison.OrdinalIgnoreCase);
                if (tIdx >= 0) done = true;
            }

            if (id >= 0 && (done || cnt > 0))
            {
                RecordProgress(id, cnt, done);
            }
            pos = closeBrace + 1;
        }
    }

    private void GetAchievementState(UTLAchievement ach, out int count, out bool unlocked)
    {
        count = PlayerPrefs.GetInt(PREF_CNT_PREFIX + ach.id, 0);
        unlocked = PlayerPrefs.GetInt(PREF_DONE_PREFIX + ach.id, 0) == 1;

        try
        {
            if (false && PlayerInfo.Instance != null)
            {
                AchievementData d = PlayerInfo.Instance.GetAchievementData(ach.id);
                if (d != null)
                {
                    if (d.achievementCount > count) count = d.achievementCount;
                    if (d.achievementComplete) unlocked = true;
                }
            }
        }
        catch { }

        int req = Mathf.Max(1, ach.countRequired);
        if (count >= req) unlocked = true;
        if (unlocked && count < req) count = req;
    }

    private void ResolveUIFont()
    {
        if (_uiFont != null) return;
        TextMeshProUGUI[] tmps = FindObjectsOfType<TextMeshProUGUI>(true);
        for (int i = 0; i < tmps.Length; i++)
        {
            if (tmps[i] != null && tmps[i].font != null)
            {
                if (tmps[i].font.name.IndexOf("FREDFRED", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _uiFont = tmps[i].font;
                    return;
                }
                if (_uiFont == null) _uiFont = tmps[i].font;
            }
        }
    }

    private Sprite LoadUISprite(string rawName)
    {
        if (string.IsNullOrEmpty(rawName)) return null;
        if (_spriteCache.TryGetValue(rawName, out Sprite cached)) return cached;

        string exactName = rawName;
        if (IconFileMap.TryGetValue(rawName, out string mapped))
        {
            exactName = mapped;
        }

        Sprite sp = Resources.Load<Sprite>("UTL_Sprites/" + exactName);
        if (sp == null)
        {
            Texture2D tex = Resources.Load<Texture2D>("UTL_Sprites/" + exactName);
            if (tex != null)
            {
                sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
        }
        _spriteCache[rawName] = sp;
        return sp;
    }

    private string ExtractBundleAssetName(object bundleAsset)
    {
        if (bundleAsset == null) return null;
        if (!_baReflectionInit)
        {
            _baReflectionInit = true;
            Type t = bundleAsset.GetType();
            _baAssetNameProp = t.GetProperty("assetName", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?? t.GetProperty("AssetName", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            _baAssetNameField = t.GetField("assetName", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?? t.GetField("_assetName", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?? t.GetField("AssetName", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }
        if (_baAssetNameProp != null) return _baAssetNameProp.GetValue(bundleAsset, null) as string;
        if (_baAssetNameField != null) return _baAssetNameField.GetValue(bundleAsset) as string;
        return null;
    }

    private Sprite GetAchievementSprite(UTLAchievement ach)
    {
        if (ach != null && ach.achievementIcon != null)
        {
            string an = ExtractBundleAssetName(ach.achievementIcon);
            if (!string.IsNullOrEmpty(an))
            {
                Sprite sp = LoadUISprite(an);
                if (sp != null) return sp;
            }
        }
        return _trophySprite;
    }

    private void EnsureUI()
    {
        if (_canvas != null) return;

        ResolveUIFont();
        _trophySprite = LoadUISprite("GUI_Settings_MainMenu_Achievements");

        GameObject canvasGo = new GameObject("UTLAchievementsCanvas");
        canvasGo.transform.SetParent(transform, false);
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 120;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        BuildTrophyButton(canvasGo.transform);
        BuildModal(canvasGo.transform);
    }

    private void BuildTrophyButton(Transform parent)
    {
        _trophyBtnRoot = new GameObject("TrophyMenuButton");
        _trophyBtnRoot.transform.SetParent(parent, false);
        RectTransform rt = _trophyBtnRoot.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        // Posicionado logo a esquerda do botao de engrenagem (canto inferior direito)
        rt.anchoredPosition = new Vector2(-118f, 14f);
        rt.sizeDelta = new Vector2(86f, 78f);

        Image bg = _trophyBtnRoot.AddComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.96f);

        Outline outline = _trophyBtnRoot.AddComponent<Outline>();
        outline.effectColor = new Color(0.95f, 0.35f, 0.58f, 1f);
        outline.effectDistance = new Vector2(3f, -3f);

        Button btn = _trophyBtnRoot.AddComponent<Button>();
        btn.targetGraphic = bg;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, 0.95f, 0.82f, 1f);
        cb.pressedColor = new Color(0.92f, 0.85f, 0.90f, 1f);
        btn.colors = cb;
        btn.onClick.AddListener(OpenModal);

        // Icone do trofeu
        GameObject iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(_trophyBtnRoot.transform, false);
        RectTransform irt = iconGo.AddComponent<RectTransform>();
        irt.anchorMin = new Vector2(0.5f, 0.62f);
        irt.anchorMax = new Vector2(0.5f, 0.62f);
        irt.sizeDelta = new Vector2(46f, 46f);
        Image iconImg = iconGo.AddComponent<Image>();
        iconImg.preserveAspect = true;
        if (_trophySprite != null)
        {
            iconImg.sprite = _trophySprite;
            iconImg.color = new Color(0.95f, 0.22f, 0.48f, 1f);
        }
        else
        {
            iconImg.color = new Color(0.98f, 0.76f, 0.14f, 1f);
        }

        // Contador pequeno embaixo do trofeu (ex: 12/34)
        _trophyBtnCountText = CreateTMP(_trophyBtnRoot.transform, "CountLabel", "0/0", 16f, TextAlignmentOptions.Center, new Color(0.18f, 0.20f, 0.36f, 1f), FontStyles.Bold);
        RectTransform crt = _trophyBtnCountText.rectTransform;
        crt.anchorMin = new Vector2(0f, 0f);
        crt.anchorMax = new Vector2(1f, 0.34f);
        crt.offsetMin = new Vector2(2f, 2f);
        crt.offsetMax = new Vector2(-2f, 0f);
    }

    private void BuildModal(Transform parent)
    {
        _modalRoot = new GameObject("AchievementsModal");
        _modalRoot.transform.SetParent(parent, false);
        RectTransform dimRt = _modalRoot.AddComponent<RectTransform>();
        dimRt.anchorMin = Vector2.zero;
        dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = Vector2.zero;
        dimRt.offsetMax = Vector2.zero;

        Image dimImg = _modalRoot.AddComponent<Image>();
        dimImg.color = new Color(0.05f, 0.06f, 0.14f, 0.80f);

        // Botao no fundo escuro para fechar ao tocar fora
        Button dimBtn = _modalRoot.AddComponent<Button>();
        dimBtn.transition = Selectable.Transition.None;
        dimBtn.onClick.AddListener(CloseModal);

        // Painel principal
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(_modalRoot.transform, false);
        RectTransform prt = panel.AddComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.08f, 0.07f);
        prt.anchorMax = new Vector2(0.92f, 0.93f);
        prt.offsetMin = Vector2.zero;
        prt.offsetMax = Vector2.zero;

        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0.10f, 0.14f, 0.28f, 0.98f);
        Outline panelBorder = panel.AddComponent<Outline>();
        panelBorder.effectColor = new Color(0.36f, 0.78f, 0.98f, 1f);
        panelBorder.effectDistance = new Vector2(4f, -4f);

        // Bloqueia clique de passar para o fundo
        panel.AddComponent<Button>().transition = Selectable.Transition.None;

        // Cabecalho
        GameObject header = new GameObject("Header");
        header.transform.SetParent(panel.transform, false);
        RectTransform hrt = header.AddComponent<RectTransform>();
        hrt.anchorMin = new Vector2(0f, 1f);
        hrt.anchorMax = new Vector2(1f, 1f);
        hrt.pivot = new Vector2(0.5f, 1f);
        hrt.anchoredPosition = Vector2.zero;
        hrt.sizeDelta = new Vector2(0f, 92f);
        Image hBg = header.AddComponent<Image>();
        hBg.color = new Color(0.15f, 0.22f, 0.42f, 1f);

        // Trofeu no cabecalho
        GameObject hIconGo = new GameObject("HeaderIcon");
        hIconGo.transform.SetParent(header.transform, false);
        RectTransform hirt = hIconGo.AddComponent<RectTransform>();
        hirt.anchorMin = new Vector2(0f, 0.5f);
        hirt.anchorMax = new Vector2(0f, 0.5f);
        hirt.pivot = new Vector2(0f, 0.5f);
        hirt.anchoredPosition = new Vector2(22f, 0f);
        hirt.sizeDelta = new Vector2(54f, 54f);
        Image hIcon = hIconGo.AddComponent<Image>();
        hIcon.preserveAspect = true;
        if (_trophySprite != null) hIcon.sprite = _trophySprite;
        else hIcon.color = new Color(0.98f, 0.76f, 0.14f, 1f);

        // Titulo
        TextMeshProUGUI titleTxt = CreateTMP(header.transform, "Title", "CONQUISTAS", 30f, TextAlignmentOptions.Left, Color.white, FontStyles.Bold);
        RectTransform trt = titleTxt.rectTransform;
        trt.anchorMin = new Vector2(0f, 0.45f);
        trt.anchorMax = new Vector2(0.65f, 0.95f);
        trt.offsetMin = new Vector2(90f, 0f);
        trt.offsetMax = Vector2.zero;

        // Estatistica + barra de progresso
        _headerStatsText = CreateTMP(header.transform, "Stats", "0 / 0 (0%)", 19f, TextAlignmentOptions.Left, new Color(0.76f, 0.92f, 1f, 1f), FontStyles.Bold);
        RectTransform srt = _headerStatsText.rectTransform;
        srt.anchorMin = new Vector2(0f, 0.08f);
        srt.anchorMax = new Vector2(0.35f, 0.48f);
        srt.offsetMin = new Vector2(90f, 0f);
        srt.offsetMax = Vector2.zero;

        GameObject barBgGo = new GameObject("ProgressBarBg");
        barBgGo.transform.SetParent(header.transform, false);
        RectTransform bbrt = barBgGo.AddComponent<RectTransform>();
        bbrt.anchorMin = new Vector2(0.34f, 0.18f);
        bbrt.anchorMax = new Vector2(0.84f, 0.40f);
        bbrt.offsetMin = Vector2.zero;
        bbrt.offsetMax = Vector2.zero;
        barBgGo.AddComponent<Image>().color = new Color(0.06f, 0.09f, 0.18f, 1f);

        GameObject barFillGo = new GameObject("ProgressBarFill");
        barFillGo.transform.SetParent(barBgGo.transform, false);
        RectTransform bfrt = barFillGo.AddComponent<RectTransform>();
        bfrt.anchorMin = Vector2.zero;
        bfrt.anchorMax = new Vector2(0f, 1f);
        bfrt.offsetMin = Vector2.zero;
        bfrt.offsetMax = Vector2.zero;
        _progressFillImg = barFillGo.AddComponent<Image>();
        _progressFillImg.color = new Color(0.28f, 0.85f, 0.56f, 1f);

        // Botao Fechar (X)
        GameObject closeGo = new GameObject("CloseButton");
        closeGo.transform.SetParent(header.transform, false);
        RectTransform clrt = closeGo.AddComponent<RectTransform>();
        clrt.anchorMin = new Vector2(1f, 0.5f);
        clrt.anchorMax = new Vector2(1f, 0.5f);
        clrt.pivot = new Vector2(1f, 0.5f);
        clrt.anchoredPosition = new Vector2(-18f, 0f);
        clrt.sizeDelta = new Vector2(56f, 56f);
        Image closeBg = closeGo.AddComponent<Image>();
        closeBg.color = new Color(0.92f, 0.25f, 0.45f, 1f);
        Button closeBtn = closeGo.AddComponent<Button>();
        closeBtn.targetGraphic = closeBg;
        closeBtn.onClick.AddListener(CloseModal);
        TextMeshProUGUI xTxt = CreateTMP(closeGo.transform, "X", "X", 28f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
        StretchFill(xTxt.rectTransform);

        // Barra de filtros (Todas / Desbloqueadas / Bloqueadas)
        GameObject tabs = new GameObject("FilterTabs");
        tabs.transform.SetParent(panel.transform, false);
        RectTransform tabRt = tabs.AddComponent<RectTransform>();
        tabRt.anchorMin = new Vector2(0f, 1f);
        tabRt.anchorMax = new Vector2(1f, 1f);
        tabRt.pivot = new Vector2(0.5f, 1f);
        tabRt.anchoredPosition = new Vector2(0f, -98f);
        tabRt.sizeDelta = new Vector2(-36f, 46f);
        HorizontalLayoutGroup hlg = tabs.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12f;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        _btnTabAll = CreateFilterButton(tabs.transform, "TODAS", FilterMode.All);
        _btnTabUnlocked = CreateFilterButton(tabs.transform, "DESBLOQUEADAS", FilterMode.Unlocked);
        _btnTabLocked = CreateFilterButton(tabs.transform, "BLOQUEADAS", FilterMode.Locked);

        // ScrollRect da lista de conquistas
        GameObject scrollGo = new GameObject("ScrollView");
        scrollGo.transform.SetParent(panel.transform, false);
        RectTransform scRt = scrollGo.AddComponent<RectTransform>();
        scRt.anchorMin = new Vector2(0f, 0f);
        scRt.anchorMax = new Vector2(1f, 1f);
        scRt.offsetMin = new Vector2(18f, 16f);
        scRt.offsetMax = new Vector2(-18f, -152f);

        Image scBg = scrollGo.AddComponent<Image>();
        scBg.color = new Color(0.07f, 0.10f, 0.20f, 0.92f);
        scrollGo.AddComponent<RectMask2D>();

        _scrollRect = scrollGo.AddComponent<ScrollRect>();
        _scrollRect.horizontal = false;
        _scrollRect.vertical = true;
        _scrollRect.movementType = ScrollRect.MovementType.Elastic;
        _scrollRect.scrollSensitivity = 32f;

        GameObject contentGo = new GameObject("Content");
        contentGo.transform.SetParent(scrollGo.transform, false);
        _listContent = contentGo.AddComponent<RectTransform>();
        _listContent.anchorMin = new Vector2(0f, 1f);
        _listContent.anchorMax = new Vector2(1f, 1f);
        _listContent.pivot = new Vector2(0.5f, 1f);
        _listContent.anchoredPosition = Vector2.zero;
        _listContent.sizeDelta = Vector2.zero;

        VerticalLayoutGroup vlg = contentGo.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(12, 12, 12, 12);
        vlg.spacing = 10f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = contentGo.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _scrollRect.viewport = scRt;
        _scrollRect.content = _listContent;

        _modalRoot.SetActive(false);
    }

    private Button CreateFilterButton(Transform parent, string label, FilterMode mode)
    {
        GameObject go = new GameObject("Tab_" + label);
        go.transform.SetParent(parent, false);
        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.26f, 0.48f, 1f);
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.onClick.AddListener(() =>
        {
            _currentFilter = mode;
            RefreshAchievementList();
        });
        TextMeshProUGUI txt = CreateTMP(go.transform, "Label", label, 18f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
        StretchFill(txt.rectTransform);
        return btn;
    }

    private void OpenModal()
    {
        SyncFromAllLocalSaveSlots();
        _modalRoot.SetActive(true);
        if (_trophyBtnRoot != null) _trophyBtnRoot.SetActive(false);
        RefreshAchievementList();
        if (_scrollRect != null) _scrollRect.verticalNormalizedPosition = 1f;
    }

    private void CloseModal()
    {
        if (_modalRoot != null) _modalRoot.SetActive(false);
        if (_trophyBtnRoot != null) _trophyBtnRoot.SetActive(true);
        UpdateTrophyBadgeCount();
    }

    private void UpdateTrophyBadgeCount()
    {
        if (_trophyBtnCountText == null) return;
        int unlocked = 0, total = 0;
        if (GameResources.Instance != null && GameResources.Instance.achievementDatabase != null && GameResources.Instance.achievementDatabase.achievements != null)
        {
            List<UTLAchievement> list = GameResources.Instance.achievementDatabase.achievements;
            total = list.Count;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null) continue;
                GetAchievementState(list[i], out _, out bool isDone);
                if (isDone) unlocked++;
            }
        }
        _trophyBtnCountText.text = unlocked + "/" + total;
    }

    private void UpdateTabVisuals()
    {
        Color activeCol = new Color(0.95f, 0.35f, 0.58f, 1f);
        Color idleCol = new Color(0.18f, 0.26f, 0.48f, 1f);
        if (_btnTabAll != null) _btnTabAll.targetGraphic.color = (_currentFilter == FilterMode.All) ? activeCol : idleCol;
        if (_btnTabUnlocked != null) _btnTabUnlocked.targetGraphic.color = (_currentFilter == FilterMode.Unlocked) ? activeCol : idleCol;
        if (_btnTabLocked != null) _btnTabLocked.targetGraphic.color = (_currentFilter == FilterMode.Locked) ? activeCol : idleCol;
    }

    private void RefreshAchievementList()
    {
        UpdateTabVisuals();
        for (int i = _listContent.childCount - 1; i >= 0; i--)
        {
            Destroy(_listContent.GetChild(i).gameObject);
        }

        if (GameResources.Instance == null || GameResources.Instance.achievementDatabase == null || GameResources.Instance.achievementDatabase.achievements == null)
        {
            TextMeshProUGUI empty = CreateTMP(_listContent, "Empty", "Banco de conquistas nao carregado.", 20f, TextAlignmentOptions.Center, Color.white, FontStyles.Normal);
            empty.rectTransform.sizeDelta = new Vector2(0f, 60f);
            return;
        }

        List<UTLAchievement> all = GameResources.Instance.achievementDatabase.achievements;
        int totalCount = all.Count;
        int unlockedCount = 0;

        // Ordena: desbloqueadas primeiro quando na aba TODAS
        List<UTLAchievement> unlockedList = new List<UTLAchievement>();
        List<UTLAchievement> lockedList = new List<UTLAchievement>();

        for (int i = 0; i < all.Count; i++)
        {
            UTLAchievement a = all[i];
            if (a == null) continue;
            GetAchievementState(a, out _, out bool done);
            if (done)
            {
                unlockedCount++;
                unlockedList.Add(a);
            }
            else
            {
                lockedList.Add(a);
            }
        }

        float pct = totalCount > 0 ? ((float)unlockedCount / totalCount) : 0f;
        _headerStatsText.text = string.Format("{0} / {1} ({2:0}%)", unlockedCount, totalCount, pct * 100f);
        if (_progressFillImg != null)
        {
            _progressFillImg.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(pct), 1f);
        }

        List<UTLAchievement> toShow = new List<UTLAchievement>();
        if (_currentFilter == FilterMode.All)
        {
            toShow.AddRange(unlockedList);
            toShow.AddRange(lockedList);
        }
        else if (_currentFilter == FilterMode.Unlocked)
        {
            toShow.AddRange(unlockedList);
        }
        else
        {
            toShow.AddRange(lockedList);
        }

        if (toShow.Count == 0)
        {
            string msg = (_currentFilter == FilterMode.Unlocked)
                ? "Nenhuma conquista desbloqueada ainda."
                : "Parabens! Todas as conquistas foram desbloqueadas!";
            TextMeshProUGUI noneTxt = CreateTMP(_listContent, "None", msg, 22f, TextAlignmentOptions.Center, new Color(0.80f, 0.88f, 1f, 1f), FontStyles.Normal);
            noneTxt.rectTransform.sizeDelta = new Vector2(0f, 80f);
            return;
        }

        for (int i = 0; i < toShow.Count; i++)
        {
            BuildAchievementCard(toShow[i]);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_listContent);
        if (_scrollRect != null) _scrollRect.verticalNormalizedPosition = 1f;
    }

    private void BuildAchievementCard(UTLAchievement ach)
    {
        GetAchievementState(ach, out int count, out bool unlocked);
        int req = Mathf.Max(1, ach.countRequired);

        GameObject card = new GameObject("Ach_" + ach.id);
        card.transform.SetParent(_listContent, false);
        RectTransform rt = card.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 94f);

        LayoutElement le = card.AddComponent<LayoutElement>();
        le.minHeight = 94f;
        le.preferredHeight = 94f;

        Image bg = card.AddComponent<Image>();
        bg.color = unlocked
            ? new Color(0.16f, 0.30f, 0.50f, 0.98f)
            : new Color(0.11f, 0.14f, 0.24f, 0.95f);

        Outline border = card.AddComponent<Outline>();
        border.effectColor = unlocked
            ? new Color(0.98f, 0.80f, 0.24f, 0.95f)
            : new Color(0.24f, 0.30f, 0.45f, 0.75f);
        border.effectDistance = new Vector2(2f, -2f);

        // Icone oficial da conquista
        GameObject iconFrame = new GameObject("IconFrame");
        iconFrame.transform.SetParent(card.transform, false);
        RectTransform ifrt = iconFrame.AddComponent<RectTransform>();
        ifrt.anchorMin = new Vector2(0f, 0.5f);
        ifrt.anchorMax = new Vector2(0f, 0.5f);
        ifrt.pivot = new Vector2(0f, 0.5f);
        ifrt.anchoredPosition = new Vector2(14f, 0f);
        ifrt.sizeDelta = new Vector2(70f, 70f);
        Image frameBg = iconFrame.AddComponent<Image>();
        frameBg.color = unlocked ? new Color(0.08f, 0.16f, 0.30f, 1f) : new Color(0.06f, 0.08f, 0.14f, 1f);

        GameObject iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(iconFrame.transform, false);
        RectTransform irt = iconGo.AddComponent<RectTransform>();
        irt.anchorMin = new Vector2(0.08f, 0.08f);
        irt.anchorMax = new Vector2(0.92f, 0.92f);
        irt.offsetMin = Vector2.zero;
        irt.offsetMax = Vector2.zero;
        Image iconImg = iconGo.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.sprite = GetAchievementSprite(ach);
        iconImg.color = unlocked ? Color.white : new Color(0.38f, 0.40f, 0.48f, 0.72f);

        // Nome traduzido
        string title = !string.IsNullOrEmpty(ach.achievementName.Key)
            ? ach.achievementName.GetTranslation()
            : ("Conquista #" + ach.id);
        if (string.IsNullOrEmpty(title)) title = ach.achievementName.Key;

        string desc = !string.IsNullOrEmpty(ach.achievementDescription.Key)
            ? ach.achievementDescription.GetTranslation()
            : "";
        if (string.IsNullOrEmpty(desc)) desc = ach.achievementDescription.Key;

        TextMeshProUGUI titleTxt = CreateTMP(
            card.transform,
            "Title",
            title,
            21f,
            TextAlignmentOptions.Left,
            unlocked ? new Color(1f, 0.96f, 0.72f, 1f) : new Color(0.76f, 0.80f, 0.88f, 1f),
            FontStyles.Bold
        );
        RectTransform trt = titleTxt.rectTransform;
        trt.anchorMin = new Vector2(0f, 0.54f);
        trt.anchorMax = new Vector2(0.76f, 0.92f);
        trt.offsetMin = new Vector2(98f, 0f);
        trt.offsetMax = new Vector2(-8f, 0f);
        titleTxt.enableWordWrapping = false;
        titleTxt.overflowMode = TextOverflowModes.Ellipsis;

        // Descricao traduzida
        TextMeshProUGUI descTxt = CreateTMP(
            card.transform,
            "Desc",
            desc,
            16f,
            TextAlignmentOptions.TopLeft,
            unlocked ? new Color(0.90f, 0.96f, 1f, 1f) : new Color(0.58f, 0.64f, 0.76f, 1f),
            FontStyles.Normal
        );
        RectTransform drt = descTxt.rectTransform;
        drt.anchorMin = new Vector2(0f, 0.08f);
        drt.anchorMax = new Vector2(0.76f, 0.54f);
        drt.offsetMin = new Vector2(98f, 0f);
        drt.offsetMax = new Vector2(-8f, 0f);
        descTxt.enableWordWrapping = true;
        descTxt.overflowMode = TextOverflowModes.Ellipsis;

        // Selo de status a direita (DESBLOQUEADA / BLOQUEADA + progresso X/Y)
        GameObject badge = new GameObject("StatusBadge");
        badge.transform.SetParent(card.transform, false);
        RectTransform brt = badge.AddComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.78f, 0.24f);
        brt.anchorMax = new Vector2(0.98f, 0.76f);
        brt.offsetMin = Vector2.zero;
        brt.offsetMax = Vector2.zero;
        Image badgeBg = badge.AddComponent<Image>();
        badgeBg.color = unlocked
            ? new Color(0.16f, 0.68f, 0.42f, 1f)
            : new Color(0.20f, 0.24f, 0.36f, 1f);

        string statusLabel = unlocked
            ? "DESBLOQUEADA"
            : (req > 1 ? string.Format("BLOQUEADA\n{0}/{1}", count, req) : "BLOQUEADA");

        TextMeshProUGUI stTxt = CreateTMP(
            badge.transform,
            "StatusText",
            statusLabel,
            14f,
            TextAlignmentOptions.Center,
            unlocked ? Color.white : new Color(0.75f, 0.80f, 0.90f, 1f),
            FontStyles.Bold
        );
        StretchFill(stTxt.rectTransform);
    }

    private TextMeshProUGUI CreateTMP(Transform parent, string name, string text, float size, TextAlignmentOptions align, Color color, FontStyles style)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        if (_uiFont != null) tmp.font = _uiFont;
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
