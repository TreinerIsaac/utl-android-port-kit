using TMPro;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Permite personalizar o tamanho das caixas de escolha de Charm, Medalha e Recompensas
/// no Android (NORMAL 100% / GRANDE 116% / EXTRA 130%) salvo em PlayerPrefs.
/// </summary>
public class UTLCharmBoxScaler : MonoBehaviour
{
    private const string PREF_KEY = "UTL_CharmBoxScaleMode";
    private static UTLCharmBoxScaler _instance;
    private static readonly float[] ScaleValues = { 1.0f, 1.16f, 1.30f };
    private static readonly string[] ModeLabels = { "NORMAL", "GRANDE", "EXTRA" };

    private readonly Dictionary<int, Vector3> _baseScales = new Dictionary<int, Vector3>();
    private float _nextScanTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static UTLCharmBoxScaler EnsureInstance()
    {
        if (_instance != null) return _instance;
        GameObject go = new GameObject("UTLCharmBoxScaler");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<UTLCharmBoxScaler>();
        return _instance;
    }

    public static int CurrentMode
    {
        get { return Mathf.Clamp(PlayerPrefs.GetInt(PREF_KEY, 0), 0, ScaleValues.Length - 1); }
    }

    public static float CurrentScale
    {
        get { return ScaleValues[CurrentMode]; }
    }

    public static string CurrentLabel
    {
        get { return ModeLabels[CurrentMode]; }
    }

    public static void CycleMode()
    {
        int next = (CurrentMode + 1) % ScaleValues.Length;
        PlayerPrefs.SetInt(PREF_KEY, next);
        PlayerPrefs.Save();
        if (_instance != null)
        {
            _instance._nextScanTime = 0f;
            _instance.ApplyToActiveBoxes();
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
    }

    private void Update()
    {
        if (Time.unscaledTime < _nextScanTime) return;
        _nextScanTime = Time.unscaledTime + 0.35f;
        ApplyToActiveBoxes();
    }

    private void ApplyScale(Component comp)
    {
        if (comp == null || !comp.gameObject.activeInHierarchy) return;
        Transform t = comp.transform;
        int id = t.GetInstanceID();
        if (!_baseScales.TryGetValue(id, out Vector3 baseScale))
        {
            baseScale = t.localScale;
            if (baseScale.x <= 0.01f || baseScale.y <= 0.01f) return;
            _baseScales[id] = baseScale;
        }
        float mult = CurrentScale;
        Vector3 target = new Vector3(baseScale.x * mult, baseScale.y * mult, baseScale.z);
        if ((t.localScale - target).sqrMagnitude > 0.0001f)
        {
            t.localScale = target;
        }
    }

    private void ApplyToActiveBoxes()
    {
        if (_baseScales.Count > 128) _baseScales.Clear();

        UIRosesRoomQuickEquip[] qe = FindObjectsOfType<UIRosesRoomQuickEquip>();
        for (int i = 0; i < qe.Length; i++) ApplyScale(qe[i]);

        UIRosesRoomQuickEquipChooseBadge[] badges = FindObjectsOfType<UIRosesRoomQuickEquipChooseBadge>();
        for (int i = 0; i < badges.Length; i++) ApplyScale(badges[i]);

        UIRosesRoomPostBattleChooseReward[] rewards = FindObjectsOfType<UIRosesRoomPostBattleChooseReward>();
        for (int i = 0; i < rewards.Length; i++) ApplyScale(rewards[i]);

        UIRosesRoomPerkSelector[] perks = FindObjectsOfType<UIRosesRoomPerkSelector>();
        for (int i = 0; i < perks.Length; i++)
        {
            ApplyScale(perks[i]);
            ForceWhiteTextOnDarkCard(perks[i]);
        }

        // Tambem garante texto branco em tooltips escuros de selecao de habilidade do Quarto da Rose
        TextMeshProUGUI[] allTmps = FindObjectsOfType<TextMeshProUGUI>();
        for (int i = 0; i < allTmps.Length; i++)
        {
            TextMeshProUGUI t = allTmps[i];
            if (t == null || !t.gameObject.activeInHierarchy) continue;
            // So mexe em textos escuros dentro de caixas do Quarto da Rose (habilidades/pedestais/confirmar)
            Color c = t.color;
            if (c.r > 0.35f || c.g > 0.35f || c.b > 0.35f) continue;
            Transform p = t.transform;
            bool inRosesRoomCard = false;
            for (int depth = 0; depth < 6 && p != null; depth++, p = p.parent)
            {
                string pn = p.name;
                if (pn.IndexOf("RosesRoom", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || pn.IndexOf("AbilityChoice", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || pn.IndexOf("PerkSelector", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || pn.IndexOf("RewardTooltip", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || pn.IndexOf("ChoiceTooltip", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || pn.IndexOf("ConfirmButton", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    inRosesRoomCard = true;
                    break;
                }
            }
            if (inRosesRoomCard)
            {
                ForceSingleTmpWhite(t);
            }
        }
    }

    private void ForceWhiteTextOnDarkCard(Component root)
    {
        if (root == null || !root.gameObject.activeInHierarchy) return;
        TextMeshProUGUI[] tmps = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < tmps.Length; i++)
        {
            ForceSingleTmpWhite(tmps[i]);
        }
    }

    private void ForceSingleTmpWhite(TextMeshProUGUI t)
    {
        if (t == null) return;
        t.color = Color.white;
        Material fm = t.fontSharedMaterial;
        if (fm != null && fm.HasProperty("_FaceColor"))
        {
            Color fc = fm.GetColor("_FaceColor");
            if (fc.r < 0.45f && fc.g < 0.45f && fc.b < 0.45f)
            {
                Material inst = t.fontMaterial;
                if (inst != null && inst.HasProperty("_FaceColor"))
                {
                    inst.SetColor("_FaceColor", Color.white);
                }
            }
        }
    }
}
