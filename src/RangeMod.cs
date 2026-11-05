using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using HutongGames.PlayMaker.Actions;
using Modding;
using Modding.Menu;
using Modding.Menu.Config;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RangeMod
{
    public class RangeMod : Mod, IGlobalSettings<Settings>, ICustomMenuMod
    {
        public static RangeMod Instance;

        public Settings Settings { get; private set; } = new Settings();

        public static float NailMul = 1f;
        public static float GreatSlashMul = 1f;
        public static float DashSlashMul = 1f;
        public static float CycloneMul = 1f;
        public static float FireballMul = 1f;
        public static float QuakeMul = 1f;
        public static float ScreamMul = 1f;

        public static float HeroMul = 1f;

        public static bool MovementSync = false;

        public override string GetVersion() => "3.11.0";

        public RangeMod() : base("Range Mod")
        {
            Instance = this;
        }

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Log("Initializing Range Mod");
            Apply();
            var harmony = new Harmony("foreyy.rangemod");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            GameObject drv = new GameObject("RM-ScaleDriver");
            UnityEngine.Object.DontDestroyOnLoad(drv);
            drv.AddComponent<ScaleDriver>();

            ModHooks.DashVectorHook += v => MovementSync
                ? new Vector2(v.x * HeroMul, v.y * HeroMul)
                : v;

            ModHooks.ObjectPoolSpawnHook += PoolSpawnScale;

            Log("Range Mod patched");
        }

        private GameObject PoolSpawnScale(GameObject obj)
        {
            try
            {
                if (obj == null) return obj;
                string n = obj.name;
                Transform t = obj.transform;

                if (_poolDiagCount < 80 && _seenPoolSpawns.Add(n))
                {
                    _poolDiagCount++;
                    bool near = Patches.NearHero(t);
                    LogWarn($"[RM-DIAG] pool-spawn '{n}' nearHero={near}");
                }

                int kind;
                Func<float> live = Patches.SpellMul(n, "", true, out kind);
                if (live == null) return obj;

                if (!Patches.IsUnderHero(t) && !Patches.NearHero(t))
                    return obj;

                Patches.TrackSpell(t, live, kind);
            }
            catch (Exception e)
            {
                LogWarn($"PoolSpawnScale failed: {e}");
            }
            return obj;
        }

        private static readonly Dictionary<string, Vector3> _poolBase = new Dictionary<string, Vector3>();
        private static readonly HashSet<string> _seenPoolSpawns = new HashSet<string>();
        private static int _poolDiagCount;

        private static float Clamp(float v) => Mathf.Clamp(v, 0.1f, 3f);

        internal void SaveNow()
        {
            try { SaveGlobalSettings(); }
            catch (Exception e) { LogWarn("Save settings failed: " + e); }
        }

        internal void Apply()
        {
            NailMul       = Settings.NailEnabled       ? Clamp(Settings.NailScale)       : 1f;
            GreatSlashMul = Settings.GreatSlashEnabled ? Clamp(Settings.GreatSlashScale) : 1f;
            DashSlashMul  = Settings.DashSlashEnabled  ? Clamp(Settings.DashSlashScale)  : 1f;
            CycloneMul    = Settings.CycloneEnabled    ? Clamp(Settings.CycloneScale)    : 1f;
            FireballMul   = Settings.FireballEnabled   ? Clamp(Settings.FireballScale)   : 1f;
            QuakeMul      = Settings.QuakeEnabled      ? Clamp(Settings.QuakeScale)      : 1f;
            ScreamMul     = Settings.ScreamEnabled     ? Clamp(Settings.ScreamScale)     : 1f;
            HeroMul       = Settings.HeroSizeEnabled   ? Clamp(Settings.HeroScale)       : 1f;
            MovementSync  = Settings.MovementSyncEnabled && Settings.HeroSizeEnabled;
        }

        public void OnLoadGlobal(Settings s)
        {
            Settings = s ?? new Settings();
            Apply();
        }

        public Settings OnSaveGlobal() => Settings;

        public bool ToggleButtonInsideMenu => false;

        private static readonly string[] OnOff = { "OFF", "ON" };

        public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates)
        {
            InstanceMenu.returnScreen = modListMenu;
            RangeMod.Instance.Log("[RM-DIAG] GetMenuScreen called, building slider rows");

            MenuButton backButton;
            MenuBuilder builder = MenuUtils.CreateMenuBuilderWithBackButton("Range Mod", modListMenu, out backButton);

            builder.AddContent(
                new NullContentLayout(),
                c =>
                {
                    c.AddScrollPaneContent(
                        new ScrollbarConfig
                        {
                            CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(modListMenu),
                            Navigation = new Navigation
                            {
                                mode = Navigation.Mode.Explicit,
                                selectOnUp = backButton,
                                selectOnDown = backButton
                            },
                            Position = new AnchoredPosition
                            {
                                ChildAnchor = new Vector2(0f, 1f),
                                ParentAnchor = new Vector2(1f, 1f),
                                Offset = new Vector2(-310f, 0f)
                            }
                        },
                        new RelLength(9 * 105f),
                        RegularGridLayout.CreateVerticalLayout(105f),
                        inner =>
                        {
                            AddSlider(inner, "\u0420\u0430\u0437\u043c\u0435\u0440 \u0433\u0435\u0440\u043e\u044f",
                                () => Settings.HeroSizeEnabled ? Settings.HeroScale : 0f,
                                v =>
                                {
                                    Settings.HeroSizeEnabled = v > 0f;
                                    if (v > 0f) Settings.HeroScale = v;
                                });
                            AddSlider(inner, "\u0413\u0432\u043e\u0437\u0434\u044c",
                                () => Settings.NailEnabled ? Settings.NailScale : 0f,
                                v =>
                                {
                                    Settings.NailEnabled = v > 0f;
                                    if (v > 0f) Settings.NailScale = v;
                                });
                            AddSlider(inner, "\u0412\u0435\u043b\u0438\u043a\u0438\u0439 \u0443\u0434\u0430\u0440",
                                () => Settings.GreatSlashEnabled ? Settings.GreatSlashScale : 0f,
                                v =>
                                {
                                    Settings.GreatSlashEnabled = v > 0f;
                                    if (v > 0f) Settings.GreatSlashScale = v;
                                });
                            AddSlider(inner, "\u0420\u0430\u0441\u0441\u0435\u043a\u0430\u044e\u0449\u0438\u0439 \u0443\u0434\u0430\u0440",
                                () => Settings.DashSlashEnabled ? Settings.DashSlashScale : 0f,
                                v =>
                                {
                                    Settings.DashSlashEnabled = v > 0f;
                                    if (v > 0f) Settings.DashSlashScale = v;
                                });
                            AddSlider(inner, "\u0423\u0440\u0430\u0433\u0430\u043d\u043d\u044b\u0439 \u0443\u0434\u0430\u0440",
                                () => Settings.CycloneEnabled ? Settings.CycloneScale : 0f,
                                v =>
                                {
                                    Settings.CycloneEnabled = v > 0f;
                                    if (v > 0f) Settings.CycloneScale = v;
                                });
                            AddSlider(inner, "\u041c\u0441\u0442\u0438\u0442\u0435\u043b\u044c\u043d\u044b\u0439 \u0434\u0443\u0445",
                                () => Settings.FireballEnabled ? Settings.FireballScale : 0f,
                                v =>
                                {
                                    Settings.FireballEnabled = v > 0f;
                                    if (v > 0f) Settings.FireballScale = v;
                                });
                            AddSlider(inner, "\u041e\u043f\u0443\u0441\u0442\u043e\u0448\u0430\u044e\u0449\u0435\u0435 \u043f\u0438\u043a\u0435",
                                () => Settings.QuakeEnabled ? Settings.QuakeScale : 0f,
                                v =>
                                {
                                    Settings.QuakeEnabled = v > 0f;
                                    if (v > 0f) Settings.QuakeScale = v;
                                });
                            AddSlider(inner, "\u0412\u043e\u044e\u0449\u0438\u0435 \u0434\u0443\u0445\u0438",
                                () => Settings.ScreamEnabled ? Settings.ScreamScale : 0f,
                                v =>
                                {
                                    Settings.ScreamEnabled = v > 0f;
                                    if (v > 0f) Settings.ScreamScale = v;
                                });

                            inner.AddHorizontalOption("\u0421\u0438\u043d\u0445\u0440. \u043c\u0443\u0432\u043c\u0435\u043d\u0442\u0430", new HorizontalOptionConfig
                            {
                                Label = "\u0421\u0438\u043d\u0445\u0440. \u043c\u0443\u0432\u043c\u0435\u043d\u0442\u0430",
                                Options = OnOff,
                                Style = HorizontalOptionStyle.VanillaStyle,
                                ApplySetting = (_, i) =>
                                {
                                    Settings.MovementSyncEnabled = i == 1;
                                    Instance.Apply();
                                },
                                RefreshSetting = (sm, _) => sm.optionList.SetOptionTo(Settings.MovementSyncEnabled ? 1 : 0),
                                CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(InstanceMenu.returnScreen)
                            }, out var sync);
                            sync.menuSetting.RefreshValueFromGameSettings();
                        }
                    );
                }
            );

            return builder.Build();
        }

        private static void AddSlider(ContentArea c, string label, Func<float> get, Action<float> set)
        {
            GameObject go = new GameObject(label);
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.transform.SetParent(c.ContentObject.transform, false);
            go.AddComponent<CanvasRenderer>();
            RectTransform rt = go.AddComponent<RectTransform>();
            new RelVector2(new Vector2(1000f, 60f)).GetBaseTransformData().Apply(rt);
            c.Layout.ModifyNext(rt);

            Image bg = go.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.001f);

            Text labelText = MakeRowText(go.transform, "Label", label, MenuResources.TrajanBold, 38,
                TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(330f, 60f));

            GameObject barGo = new GameObject("Track");
            UnityEngine.Object.DontDestroyOnLoad(barGo);
            barGo.transform.SetParent(go.transform, false);
            barGo.AddComponent<CanvasRenderer>();
            RectTransform barRt = barGo.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0.5f, 0.5f);
            barRt.anchorMax = new Vector2(0.5f, 0.5f);
            barRt.pivot = new Vector2(0.5f, 0.5f);
            barRt.anchoredPosition = new Vector2(90f, 0f);
            barRt.sizeDelta = new Vector2(420f, 26f);
            Image barImg = barGo.AddComponent<Image>();
            barImg.color = new Color(0f, 0f, 0f, 0.65f);

            GameObject fillGo = new GameObject("Fill");
            UnityEngine.Object.DontDestroyOnLoad(fillGo);
            fillGo.transform.SetParent(barGo.transform, false);
            fillGo.AddComponent<CanvasRenderer>();
            RectTransform fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.anchoredPosition = new Vector2(0f, 0f);
            fillRt.sizeDelta = new Vector2(0f, 0f);
            Image fillImg = fillGo.AddComponent<Image>();
            fillImg.color = new Color(1f, 1f, 1f, 0.85f);

            GameObject handleGo = new GameObject("Handle");
            UnityEngine.Object.DontDestroyOnLoad(handleGo);
            handleGo.transform.SetParent(barGo.transform, false);
            handleGo.AddComponent<CanvasRenderer>();
            RectTransform handleRt = handleGo.AddComponent<RectTransform>();
            handleRt.anchorMin = new Vector2(0f, 0.5f);
            handleRt.anchorMax = new Vector2(0f, 0.5f);
            handleRt.pivot = new Vector2(0.5f, 0.5f);
            handleRt.anchoredPosition = new Vector2(0f, 0f);
            handleRt.sizeDelta = new Vector2(16f, 40f);
            Image handleImg = handleGo.AddComponent<Image>();
            handleImg.color = new Color(1f, 1f, 1f, 1f);
            handleRt.sizeDelta = new Vector2(26f, 52f);

            Text valueText = MakeRowText(go.transform, "Value", "1.0x", MenuResources.TrajanRegular, 38,
                TextAnchor.MiddleRight, new Vector2(1f, 0.5f), new Vector2(-30f, 0f), new Vector2(170f, 60f));

            Animator curL = MakeCursor(go.transform, "CursorLeft", new Vector2(0f, 0.5f), new Vector2(-70f, 0f), new Vector3(0.4f, 0.4f, 0.4f));
            Animator curR = MakeCursor(go.transform, "CursorRight", new Vector2(1f, 0.5f), new Vector2(70f, 0f), new Vector3(-0.4f, 0.4f, 0.4f));

            SliderRow row = go.AddComponent<SliderRow>();
            row.leftCursor = curL;
            row.rightCursor = curR;
            row.get = get;
            row.set = set;
            row.track = barRt;
            row.fill = fillRt;
            row.handle = handleRt;
            row.valueText = valueText;
            row.cancelAction = GlobalEnums.CancelAction.GoToExtrasMenu;
            row.customCancelAction = _ => UIManager.instance.UIGoToDynamicMenu(InstanceMenu.returnScreen);
            c.NavGraph.AddNavigationNode(row);
            row.InitFrom(get());

            RangeMod.Instance.Log("[RM-DIAG] slider row added: " + label);
        }

        private static Text MakeRowText(Transform parent, string name, string content, Font font, int size,
            TextAnchor anchor, Vector2 anchorPos, Vector2 pos, Vector2 dims)
        {
            GameObject go = new GameObject(name);
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.transform.SetParent(parent, false);
            go.AddComponent<CanvasRenderer>();
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorPos;
            rt.anchorMax = anchorPos;
            rt.pivot = new Vector2(anchorPos.x, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = dims;
            Text text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.resizeTextMaxSize = size;
            text.alignment = anchor;
            text.text = content;
            text.supportRichText = true;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            go.AddComponent<FixVerticalAlign>();
            return text;
        }

        private static Animator MakeCursor(Transform parent, string name, Vector2 anchorPos, Vector2 pos, Vector3 scale)
        {
            GameObject go = new GameObject(name);
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.transform.SetParent(parent, false);
            go.AddComponent<CanvasRenderer>();
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(164f, 119f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchorMin = anchorPos;
            rt.anchorMax = anchorPos;
            rt.anchoredPosition = pos;
            rt.localScale = scale;
            Animator animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = MenuResources.MenuCursorAnimator;
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.applyRootMotion = false;
            go.AddComponent<Image>();
            return animator;
        }

        internal static class InstanceMenu
        {
            public static MenuScreen returnScreen;
        }
    }


    internal class ScaleDriver : MonoBehaviour
    {
        private void OnEnable()
        {
            Camera.onPreRender += Enforce;
        }

        private void OnDisable()
        {
            Camera.onPreRender -= Enforce;
        }

        private static void Enforce(Camera _)
        {
            try { Patches.EnforceTrackedSpells(); }
            catch (Exception e) { RangeMod.Instance.LogWarn("Enforce failed: " + e); }
        }

        private void LateUpdate()
        {
            Enforce(null);
        }
    }

    internal class SliderRow : MenuSelectable, IMoveHandler, IPointerDownHandler, IPointerUpHandler, IDragHandler, IScrollHandler
    {
        public Func<float> get;
        public Action<float> set;
        public RectTransform track;
        public RectTransform fill;
        public RectTransform handle;
        public Text valueText;

        public const int MaxIndex = 30;
        private const float BarWidth = 420f;
        private const float HandleHalf = 13f;

        private int index;
        private bool dragging;

        public void InitFrom(float v)
        {
            SetIndex(ToIndex(v), false);
        }

        private static int ToIndex(float v)
        {
            if (v <= 0f) return 0;
            return Mathf.Clamp(Mathf.RoundToInt(v * 10f), 1, MaxIndex);
        }

        private void SetIndex(int i, bool apply)
        {
            index = Mathf.Clamp(i, 0, MaxIndex);
            float frac = index / (float)MaxIndex;

            float x = HandleHalf + frac * (BarWidth - 2f * HandleHalf);
            if (fill != null)
            {
                Vector2 am = fill.anchorMax;
                am.x = frac;
                fill.anchorMax = am;
            }
            if (handle != null)
            {
                Vector2 p = handle.anchoredPosition;
                p.x = x;
                handle.anchoredPosition = p;
            }
            if (valueText != null)
                valueText.text = index == 0 ? "OFF" : (index * 0.1f).ToString("0.0") + "x";

            if (apply)
            {
                try
                {
                    set?.Invoke(index == 0 ? 0f : index * 0.1f);
                    RangeMod.Instance.Apply();
                    if (!dragging) RangeMod.Instance.SaveNow();
                    RangeMod.Instance.LogWarn($"[RM-DIAG] slider -> {(index == 0 ? "OFF" : (index * 0.1f).ToString("0.0") + "x")}");
                }
                catch (Exception e)
                {
                    RangeMod.Instance.LogWarn($"Slider apply failed: {e}");
                }
                if (uiAudioPlayer != null) uiAudioPlayer.PlaySlider();
            }
        }

        public new void OnMove(AxisEventData move)
        {
            if (base.interactable)
            {
                switch (move.moveDir)
                {
                    case MoveDirection.Left:
                        SetIndex(index - 1, true);
                        return;
                    case MoveDirection.Right:
                        SetIndex(index + 1, true);
                        return;
                }
            }
            base.OnMove(move);
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (!base.interactable) return;
            SetIndex(index + (eventData.scrollDelta.y > 0f ? 1 : -1), true);
        }

        public new void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            if (!base.interactable) return;
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(gameObject);
            if (OnTrack(eventData))
            {
                dragging = true;
                SetFromPointer(eventData);
            }
        }

        public new void OnPointerUp(PointerEventData eventData)
        {
            if (dragging) RangeMod.Instance.SaveNow();
            dragging = false;
            base.OnPointerUp(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!base.interactable || !dragging) return;
            SetFromPointer(eventData);
        }

        private bool OnTrack(PointerEventData eventData)
        {
            if (track == null) return false;
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    track, eventData.position, eventData.pressEventCamera, out local))
                return false;
            return local.x >= -BarWidth * 0.5f - 20f && local.x <= BarWidth * 0.5f + 20f &&
                   local.y >= -30f && local.y <= 30f;
        }

        private void SetFromPointer(PointerEventData eventData)
        {
            if (track == null) return;
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    track, eventData.position, eventData.pressEventCamera, out local))
                return;
            float frac = Mathf.Clamp01((local.x + BarWidth * 0.5f) / BarWidth);
            SetIndex(Mathf.RoundToInt(frac * MaxIndex), true);
        }
    }

    public class Settings
    {

        public bool NailEnabled { get; set; } = true;
        public float NailScale { get; set; } = 1f;

        public bool HeroSizeEnabled { get; set; } = false;
        public float HeroScale { get; set; } = 1f;

        public bool MovementSyncEnabled { get; set; } = false;

        public bool GreatSlashEnabled { get; set; } = true;
        public float GreatSlashScale { get; set; } = 1f;

        public bool DashSlashEnabled { get; set; } = true;
        public float DashSlashScale { get; set; } = 1f;

        public bool CycloneEnabled { get; set; } = true;
        public float CycloneScale { get; set; } = 1f;

        public bool FireballEnabled { get; set; } = true;
        public float FireballScale { get; set; } = 1f;

        public bool QuakeEnabled { get; set; } = true;
        public float QuakeScale { get; set; } = 1f;

        public bool ScreamEnabled { get; set; } = true;
        public float ScreamScale { get; set; } = 1f;
    }

    [HarmonyPatch]
    internal static class Patches
    {
        internal static void Scale(Transform t, float m)
        {
            if (t == null || m == 1f) return;
            Vector3 s = t.localScale;
            s.x *= m;
            s.y *= m;
            t.localScale = s;
        }

        internal static float SlashMul(float featureMul)
        {
            float hero = RangeMod.HeroMul;
            if (hero > 0.001f && Math.Abs(hero - 1f) > 0.0001f)
                return featureMul / hero;
            return featureMul;
        }

        private static Transform _heroRoot;

        internal static bool NearHero(Transform t)
        {
            var hero = HeroController.instance;
            if (hero == null || t == null) return false;
            return Vector2.Distance(t.position, hero.transform.position) < 25f;
        }

        private struct SpellTrack
        {
            public Transform t;
            public Vector3 baseScale;
            public Func<float> mul;
            public Vector3 lastSet;
            public int kind;
            public bool warned;
            public bool appliedLogged;
            public float loggedM;
            public Vector3 basePos;
            public Vector3 lastSetPos;
            public Vector3 chain;
            public bool chainSet;
            public bool underHero;
            public float lastSeen;
        }

        private static readonly List<SpellTrack> _tracked = new List<SpellTrack>();

        internal const int KindNone = 0;
        internal const int KindFire = 1;
        internal const int KindQuake = 2;
        internal const int KindScream = 3;

        private static int StrongKind(string n)
        {
            if (n.Contains("Fireball") || n.Contains("Fire Ball") || n.Contains("Vengeful") || n.Contains("Shade Soul"))
                return KindFire;
            if (n.StartsWith("Scr") || n.StartsWith("S1 ") || n.Contains("Roar") || n.Contains("Scream")
                || n.Contains("Shriek") || n.Contains("Wraith") || n.Contains("Abyss"))
                return KindScream;
            if (n.StartsWith("Q ") || n.StartsWith("Q1") || n.StartsWith("Q2") || n.Contains("Quake")
                || n.Contains("Pillar") || n.Contains("Dive") || n.Contains("Slam")
                || n.Contains("Shadow Burst") || n.Contains("Shadow Ring"))
                return KindQuake;
            return -1;
        }

        private static bool GenericEffect(string n)
        {
            return n.Contains("Splash") || n.Contains("Dripper") || n.Contains("Spatter") || n.Contains("Streak")
                || n.Contains("Puff") || n.Contains("Trail") || n.Contains("Wave") || n.Contains("Burst")
                || n.Contains("Ring") || n.Contains("Shock") || n.Contains("Flash") || n.Contains("Blast")
                || n.Contains("Glow") || n.Contains("Smoke") || n.Contains("Dust") || n.Contains("Impact")
                || n.Contains("Hit");
        }

        private static bool AbilityName(string n)
        {
            return n.Contains("SD ") || n.Contains("Sharp") || n.Contains("Crystal") || n.Contains("Dash")
                || n.Contains("Recharge") || n.Contains("Wing") || n.Contains("Cloak") || n.Contains("Charm")
                || n.Contains("Thorn") || n.Contains("Soul") || n.Contains("Geo") || n.Contains("Wall")
                || n.Contains("Map") || n.Contains("Inv") || n.Contains("Focus") || n.Contains("Blobs")
                || n.Contains("Blurp") || n.Contains("Grub");
        }

        internal static Func<float> MulForKind(int kind)
        {
            if (kind == KindFire) return () => RangeMod.FireballMul;
            if (kind == KindQuake) return () => RangeMod.QuakeMul;
            if (kind == KindScream) return () => RangeMod.ScreamMul;
            return () => 1f;
        }

        internal static int ActiveSpellKind;
        private static float _activeSpellTime;

        internal static Func<float> SpellMul(string n, string fsm, bool spawn, out int kind)
        {
            string nm = (n ?? "") + " " + (fsm ?? "");
            int k = StrongKind(nm);
            if (k >= 0) { kind = k; return MulForKind(k); }
            if (spawn && GenericEffect(nm) && !AbilityName(nm) && Time.time - _activeSpellTime < 2.5f && ActiveSpellKind != KindNone)
            {
                kind = ActiveSpellKind;
                return MulForKind(kind);
            }
            kind = KindNone;
            return null;
        }

        internal static bool IsNailRelated(Transform t)
        {
            for (Transform p = t; p != null; p = p.parent)
            {
                string n = p.name;
                if (n.Contains("Slash") || n.Contains("Nail") || n.Contains("Attack") || n.Contains("Great")
                    || n.Contains("Cyclone") || n.Contains("Dash Slash") || n.Contains("Wall Slash"))
                    return true;
            }
            return false;
        }

        private static bool TrackedAncestor(Transform t, int kind)
        {
            for (Transform p = t.parent; p != null; p = p.parent)
            {
                for (int j = 0; j < _tracked.Count; j++)
                {
                    if (_tracked[j].t != p) continue;
                    if (kind == KindNone || _tracked[j].kind == kind) return true;
                }
            }
            return false;
        }

        internal static void TrackSpell(Transform t, Func<float> mul, int kind)
        {
            if (t == null || mul == null) return;
            if (kind == KindNone && IsNailRelated(t)) return;

            for (int i = 0; i < _tracked.Count; i++)
            {
                if (_tracked[i].t == t)
                {
                    SpellTrack e = _tracked[i];
                    if (kind != KindNone) { e.kind = kind; e.mul = mul; }
                    _tracked[i] = e;
                    return;
                }
            }
            if (TrackedAncestor(t, kind)) return;

            if (_tracked.Count > 500)
            {
                for (int i = _tracked.Count - 1; i >= 0 && _tracked.Count > 400; i--)
                    if (_tracked[i].t == null) _tracked.RemoveAt(i);
                if (_tracked.Count > 500) return;
            }

            _tracked.Add(new SpellTrack
            {
                t = t,
                baseScale = t.localScale,
                mul = mul,
                lastSet = t.localScale,
                kind = kind,
                basePos = t.localPosition,
                lastSetPos = t.localPosition
            });
            if (_trackDiag < 80)
            {
                _trackDiag++;
                RangeMod.Instance.LogWarn($"[RM-DIAG] track '{t.name}' kind={kind}");
            }
        }

        private static int _trackDiag;

        private static readonly string[] BodyIgnore =
        {
            "Slash", "Nail", "Attack", "Great", "Cyclone", "Audio", "Control", "Rigid",
            "Inventory", "Charm", "Focus", "Map", "Body", "Sprite", "Shadow",
            "UI", "Wing", "Cloak", "Horn", "Face", "Eye", "Hair", "Mesh", "Model", "Rig", "Knight", "Hero"
        };

        private static void CollectHeroEffects(Transform root, List<Transform> list)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform c = root.GetChild(i);
                string n = c.name;
                bool skip = false;
                for (int k = 0; k < BodyIgnore.Length; k++)
                    if (n.Contains(BodyIgnore[k])) { skip = true; break; }
                if (skip) continue;
                list.Add(c);
                CollectHeroEffects(c, list);
            }
        }

        private static Transform _lastHeroRoot;
        private static Transform _spellsRoot;
        private static Transform[] _spellsChildren;
        private static readonly List<Transform> _scanBuf = new List<Transform>(128);
        private static bool _heroUpdateLogged;
        private static Vector3 _heroBase = Vector3.one;
        private static Vector3 _heroLastSet = Vector3.one;
        private static PlayMakerFSM _spellFsm;
        private static BoxCollider2D[] _diveCols;
        private static Vector2[] _diveBases;
        private static bool _diveScaled;

        internal static void UpdateHeroSpells(HeroController hero)
        {
            if (_lastHeroRoot != hero.transform)
            {
                _lastHeroRoot = hero.transform;
                _spellFsm = null;
                _diveCols = null;
                _diveBases = null;
                _diveScaled = false;
            }

            if (_spellFsm == null)
            {
                PlayMakerFSM[] fsms = hero.GetComponentsInChildren<PlayMakerFSM>(true);
                for (int i = 0; i < fsms.Length; i++)
                    if (fsms[i].FsmName == "Spell Control") { _spellFsm = fsms[i]; break; }
                if (_spellFsm != null) RangeMod.Instance.LogWarn("[RM-DIAG] Spell Control FSM found");
            }

            string st = (_spellFsm != null && _spellFsm.Fsm != null) ? (_spellFsm.Fsm.ActiveStateName ?? "") : "";
            int castKind = (st.Contains("Fireball") || st.Contains("Fire Ball") || st.Contains("Shade Soul") || st.Contains("Vengeful")) ? KindFire
                : (st.StartsWith("Q") || st.Contains("Quake") || st.Contains("Dive")) ? KindQuake
                : (st.Contains("Scream") || st.Contains("Roar") || st.Contains("Shriek") || st.Contains("Abyss") || st.Contains("Wraith")) ? KindScream
                : KindNone;
            if (castKind != KindNone)
            {
                if (ActiveSpellKind != castKind)
                    RangeMod.Instance.LogWarn($"[RM-DIAG] cast state='{st}' kind={castKind}");
                ActiveSpellKind = castKind;
                _activeSpellTime = Time.time;
            }

            if (_spellsChildren == null || _spellsRoot == null || !_spellsRoot || _spellsRoot.parent != hero.transform)
            {
                _spellsRoot = null;
                _spellsChildren = null;
                for (int si = 0; si < hero.transform.childCount; si++)
                {
                    Transform c0 = hero.transform.GetChild(si);
                    if (c0.name == "Spells") { _spellsRoot = c0; break; }
                }
                if (_spellsRoot != null)
                {
                    _spellsChildren = new Transform[_spellsRoot.childCount];
                    for (int si = 0; si < _spellsRoot.childCount; si++)
                        _spellsChildren[si] = _spellsRoot.GetChild(si);
                }
            }
            if (_spellsChildren != null)
            {
                for (int si = 0; si < _spellsChildren.Length; si++)
                {
                    Transform c0 = _spellsChildren[si];
                    if (c0 == null) continue;
                    int k0 = c0.name.StartsWith("Q") ? KindQuake
                        : (c0.name.StartsWith("Scr") || c0.name.StartsWith("S1")) ? KindScream
                        : KindFire;
                    TrackSpell(c0, MulForKind(k0), k0);
                }
            }

            if (Time.frameCount % 4 == 0)
            {
                _scanBuf.Clear();
                CollectHeroEffects(hero.transform, _scanBuf);
                for (int i = 0; i < _scanBuf.Count; i++)
                {
                    int kind;
                    Func<float> mv = SpellMul(_scanBuf[i].name, "", false, out kind);
                    if (mv != null) TrackSpell(_scanBuf[i], mv, kind);
                }
            }

            bool diving = castKind == KindQuake;
            if (diving)
            {
                if (_diveCols == null)
                {
                    BoxCollider2D[] all = hero.GetComponents<BoxCollider2D>();
                    List<BoxCollider2D> trg = new List<BoxCollider2D>();
                    for (int i = 0; i < all.Length; i++)
                        if (all[i] != null && all[i].isTrigger) trg.Add(all[i]);
                    _diveCols = trg.ToArray();
                    _diveBases = new Vector2[_diveCols.Length];
                    for (int i = 0; i < _diveCols.Length; i++) _diveBases[i] = _diveCols[i].size;
                }
                if (!_diveScaled) _diveScaled = true;
                float k2 = RangeMod.QuakeMul / Mathf.Max(0.001f, RangeMod.HeroMul);
                for (int i = 0; i < _diveCols.Length; i++)
                {
                    if (_diveCols[i] == null) continue;
                    Vector2 target2 = new Vector2(_diveBases[i].x * k2, _diveBases[i].y * k2);
                    if ((_diveCols[i].size - target2).sqrMagnitude > 1e-8f) _diveCols[i].size = target2;
                }
            }
            else if (_diveScaled && _diveCols != null)
            {
                for (int i = 0; i < _diveCols.Length; i++)
                    if (_diveCols[i] != null) _diveCols[i].size = _diveBases[i];
                _diveScaled = false;
            }
        }

        private static bool TryGetHeroChain(Transform t, out Vector3 chain)
        {
            chain = Vector3.zero;
            var hero = HeroController.instance;
            if (hero == null || t == null) return false;
            Transform h = hero.transform;
            Transform p = t.parent;
            while (p != null && p != h)
            {
                chain += p.localPosition;
                p = p.parent;
            }
            return p == h;
        }

        internal static void EnforceTrackedSpells()
        {
            for (int i = _tracked.Count - 1; i >= 0; i--)
            {
                SpellTrack tr = _tracked[i];
                if (tr.t == null) { _tracked.RemoveAt(i); continue; }
                if (TrackedAncestor(tr.t, tr.kind)) continue;

                float m = tr.mul != null ? tr.mul() : 1f;
                Vector3 cur = tr.t.localScale;
                if (Math.Abs(cur.x - tr.lastSet.x) > 0.0001f || Math.Abs(cur.y - tr.lastSet.y) > 0.0001f)
                    tr.baseScale = cur;

                Vector3 lossy = tr.t.lossyScale;
                Vector3 target = cur;
                if (Math.Abs(cur.x) > 1e-5f && Math.Abs(lossy.x) > 1e-5f)
                    target.x = tr.baseScale.x * m * cur.x / lossy.x;
                if (Math.Abs(cur.y) > 1e-5f && Math.Abs(lossy.y) > 1e-5f)
                    target.y = tr.baseScale.y * m * cur.y / lossy.y;

                if (Math.Abs(target.x - cur.x) > 0.0001f || Math.Abs(target.y - cur.y) > 0.0001f)
                    tr.t.localScale = target;
                tr.lastSet = target;

                if (!tr.chainSet)
                {
                    tr.chainSet = true;
                    tr.underHero = TryGetHeroChain(tr.t, out Vector3 ch);
                    tr.chain = ch;
                    tr.lastSeen = Time.time;
                }
                if (tr.underHero)
                {
                    Vector3 lp = tr.t.localPosition;
                    if (Math.Abs(lp.x - tr.lastSetPos.x) > 0.0001f || Math.Abs(lp.y - tr.lastSetPos.y) > 0.0001f)
                        tr.basePos = lp;
                    float hm = tr.t.parent != null ? Mathf.Abs(tr.t.parent.lossyScale.y) : 1f;
                    if (hm < 1e-4f) hm = 1f;
                    Vector3 tp;
                    tp.x = (tr.chain.x + tr.basePos.x) * m / hm - tr.chain.x;
                    tp.y = (tr.chain.y + tr.basePos.y) * m / hm - tr.chain.y;
                    tp.z = (tr.chain.z + tr.basePos.z) * m / hm - tr.chain.z;
                    if (Math.Abs(tp.x - lp.x) > 0.0001f || Math.Abs(tp.y - lp.y) > 0.0001f)
                        tr.t.localPosition = tp;
                    tr.lastSetPos = tp;
                }
                _tracked[i] = tr;

                if ((!tr.appliedLogged || Math.Abs(tr.loggedM - m) > 0.01f) && tr.t.gameObject.activeInHierarchy)
                {
                    tr.appliedLogged = true;
                    tr.loggedM = m;
                    _tracked[i] = tr;
                    Vector3 lw = tr.t.lossyScale;
                    RangeMod.Instance.LogWarn($"[RM-DIAG] apply '{tr.t.name}' kind={tr.kind} m={m:F2} base={tr.baseScale.x:F2} local={target.x:F2} world={lw.x:F2}");
                }

                if (!tr.warned)
                {
                    Vector3 w = tr.t.lossyScale;
                    float want = tr.baseScale.x * m;
                    if (w.x > 0.01f && Mathf.Abs(w.x - want) / Mathf.Max(want, 0.01f) > 0.25f)
                    {
                        tr.warned = true;
                        _tracked[i] = tr;
                        RangeMod.Instance.LogWarn($"[RM-DIAG] MISMATCH '{tr.t.name}' kind={tr.kind} world.x={w.x:F2} want={want:F2} m={m:F2}");
                    }
                }
            }
        }

        internal static bool IsUnderHero(Transform t)
        {
            var hero = HeroController.instance;
            if (hero == null) return false;
            _heroRoot = hero.transform;
            if (t == _heroRoot) return true;
            for (Transform cur = t.parent; cur != null; cur = cur.parent)
                if (cur == _heroRoot)
                    return true;
            return false;
        }

        private static Transform[] _artTransforms;
        private static readonly Dictionary<Transform, Vector3> _artBase = new Dictionary<Transform, Vector3>();
        private static bool _artLogged;

        private static readonly string[] ArtSlashNames =
        {
            "Great Slash", "Dash Slash", "Cyclone Slash"
        };

        private static void UpdateArtSlashes(HeroController hero)
        {
            if (_artTransforms == null)
            {
                var found = new List<Transform>();
                foreach (string nm in ArtSlashNames)
                {
                    Transform t = FindDeep(hero.transform, nm);
                    if (t != null)
                    {
                        found.Add(t);
                        _artBase[t] = t.localScale;
                    }
                }
                _artTransforms = found.ToArray();
                if (!_artLogged && _artTransforms.Length > 0)
                {
                    _artLogged = true;
                    RangeMod.Instance.LogWarn(
                        "[RM-DIAG] art slashes found: " +
                        string.Join(", ", _artTransforms.Select(a => a.name)));
                }
                return;
            }

            float mul = RangeMod.HeroMul;
            foreach (Transform t in _artTransforms)
            {
                if (t == null || !_artBase.ContainsKey(t)) continue;

                string n = t.name;
                float m =
                    n.StartsWith("Great Slash") ? RangeMod.GreatSlashMul :
                    n.StartsWith("Dash Slash") ? RangeMod.DashSlashMul :
                    RangeMod.CycloneMul;

                float local = SlashMul(m);                Vector3 target = new Vector3(_artBase[t].x * local, _artBase[t].y * local, _artBase[t].z);
                Vector3 cur = t.localScale;
                if (Math.Abs(cur.x - target.x) > 0.0001f || Math.Abs(cur.y - target.y) > 0.0001f)
                    t.localScale = target;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HeroController), "Update")]
        public static void Hero_Update(HeroController __instance)
        {
            try
            {
                if (!_heroUpdateLogged)
                {
                    _heroUpdateLogged = true;
                    RangeMod.Instance.LogWarn("[RM-DIAG] Hero_Update postfix alive, HeroMul=" + RangeMod.HeroMul);
                }
                UpdateArtSlashes(__instance);
                UpdateHeroSpells(__instance);
                float m = RangeMod.HeroMul;
                Transform t = __instance.transform;

                Vector3 hcur = t.localScale;
                if (Mathf.Abs(hcur.x - _heroLastSet.x) > 0.0001f || Mathf.Abs(hcur.y - _heroLastSet.y) > 0.0001f)
                    _heroBase = hcur;
                Vector3 htarget = new Vector3(_heroBase.x * m, _heroBase.y * m, _heroBase.z);
                if (Mathf.Abs(hcur.x - htarget.x) > 0.0001f || Mathf.Abs(hcur.y - htarget.y) > 0.0001f)
                    t.localScale = htarget;
                _heroLastSet = t.localScale;
            }
            catch (Exception e)
            {
                RangeMod.Instance.LogWarn("Hero scale failed: " + e);
            }
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(HeroController), "Move")]
        public static void Hero_Move(HeroController __instance)
        {
            if (!RangeMod.MovementSync || RangeMod.HeroMul == 1f) return;
            var rb = __instance.GetComponent<Rigidbody2D>();
            if (rb == null) return;
            Vector2 v = rb.velocity;
            v.x *= RangeMod.HeroMul;
            rb.velocity = v;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HeroController), "Jump")]
        public static void Hero_Jump(HeroController __instance)
        {
            if (!RangeMod.MovementSync || RangeMod.HeroMul == 1f) return;
            var rb = __instance.GetComponent<Rigidbody2D>();
            if (rb == null) return;
            Vector2 v = rb.velocity;
            v.y *= RangeMod.HeroMul;
            rb.velocity = v;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HeroController), "DoubleJump")]
        public static void Hero_DoubleJump(HeroController __instance)
        {
            if (!RangeMod.MovementSync || RangeMod.HeroMul == 1f) return;
            var rb = __instance.GetComponent<Rigidbody2D>();
            if (rb == null) return;
            Vector2 v = rb.velocity;
            v.y *= RangeMod.HeroMul;
            rb.velocity = v;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HeroController), "Update")]
        public static void Hero_FallCap(HeroController __instance)
        {
            if (!RangeMod.MovementSync) return;
            float mul = RangeMod.HeroMul;
            if (mul == 1f) return;
            var rb = __instance.GetComponent<Rigidbody2D>();
            if (rb == null) return;
            float cap = __instance.MAX_FALL_VELOCITY * mul;
            Vector2 v = rb.velocity;
            if (v.y < -cap)
                rb.velocity = new Vector2(v.x, -cap);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(NailSlash), nameof(NailSlash.StartSlash))]
        public static void NailSlash_StartSlash(NailSlash __instance)
        {
            string objName = __instance.transform.name;
            bool underHero = IsUnderHero(__instance.transform);

            if (_slashDiagCount < 40 && _seenSlashes.Add(objName))
            {
                _slashDiagCount++;
                RangeMod.Instance.LogWarn($"[RM-DIAG] slash obj='{objName}' underHero={underHero}");
            }

            if (!underHero) return;

            Transform t = __instance.transform;

            float m;
            if (objName.StartsWith("Great Slash"))
                m = RangeMod.GreatSlashMul;
            else if (objName.StartsWith("Dash Slash"))
                m = RangeMod.DashSlashMul;
            else if (objName.StartsWith("Cyclone Slash"))
                m = RangeMod.CycloneMul;
            else
                m = RangeMod.NailMul;

            Scale(t, SlashMul(m));
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(NailSlash), "Awake")]
        public static void NailSlash_Awake(NailSlash __instance)
        {
            string n = __instance.transform.name;
            if (_awakeDiagCount < 40 && _seenAwake.Add(n))
            {
                _awakeDiagCount++;
                bool under = IsUnderHero(__instance.transform);
                RangeMod.Instance.LogWarn($"[RM-DIAG] NailSlash awake obj='{n}' underHero={under}");
            }
        }

        private static readonly HashSet<string> _seenAwake = new HashSet<string>();
        private static int _awakeDiagCount;

        private static readonly HashSet<string> _seenSlashes = new HashSet<string>();
        private static int _slashDiagCount;

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform r = FindDeep(root.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HeroController), "Start")]
        public static void Hero_Start(HeroController __instance)
        {
            try
            {
                float hero = RangeMod.HeroMul;
                float scream = RangeMod.ScreamMul;
                float quake = RangeMod.QuakeMul;

                {
                    string[] screamBoxes =
                    {
                        "S1 F 1", "S1 F 2", "S1 F 3",
                        "S1 Flame1", "S1 Flame2", "S1 Flame3"
                    };
                    foreach (string nm in screamBoxes)
                    {
                        Transform t = FindDeep(__instance.transform, nm);
                        if (t != null)
                            TrackSpell(t, () => RangeMod.ScreamMul, KindScream);
                    }
                }

            }
            catch (Exception e)
            {
                RangeMod.Instance.LogWarn($"Scream child scale failed: {e}");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SpawnObjectFromGlobalPool), nameof(SpawnObjectFromGlobalPool.OnEnter))]
        public static void SpawnObject_OnEnter(SpawnObjectFromGlobalPool __instance)
        {
            try
            {
                string fsm = __instance.Fsm != null ? __instance.Fsm.Name : "";
                GameObject ownerGo = __instance.Fsm != null && __instance.Fsm.Owner != null
                    ? __instance.Fsm.Owner.gameObject
                    : null;
                bool ownerIsHero = ownerGo != null && ownerGo.GetComponent<HeroController>() != null;

                if (ownerIsHero && _seenSpawnCount < 40 && _seenSpawns.Add(fsm + " | " + objNameSafe(__instance)))
                {
                    _seenSpawnCount++;
                    RangeMod.Instance.LogWarn($"[RM-DIAG] spawn fsm='{fsm}' obj='{objNameSafe(__instance)}' heroOwner={ownerIsHero}");
                }

            }
            catch (Exception e)
            {
                RangeMod.Instance.LogWarn($"SpawnObject diag failed: {e}");
            }
        }

        private static readonly HashSet<string> _seenSpawns = new HashSet<string>();
        private static int _seenSpawnCount;

        private static string objNameSafe(SpawnObjectFromGlobalPool a)
        {
            var go = a.gameObject.Value;
            return go != null ? go.name : "<null>";
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HutongGames.PlayMaker.Actions.CreateObject), nameof(HutongGames.PlayMaker.Actions.CreateObject.OnEnter))]
        public static void CreateObject_OnEnter(HutongGames.PlayMaker.Actions.CreateObject __instance)
        {
            try
            {
                string fsm = __instance.Fsm != null ? __instance.Fsm.Name : "";
                GameObject ownerGo = __instance.Fsm != null && __instance.Fsm.Owner != null
                    ? __instance.Fsm.Owner.gameObject
                    : null;
                bool ownerIsHero = ownerGo != null && ownerGo.GetComponent<HeroController>() != null;
                if (!ownerIsHero)
                {
                    GameObject obj0 = __instance.storeObject.Value;
                    if (obj0 == null) return;
                    int kind0;
                    Func<float> live0 = SpellMul(obj0.name, fsm, true, out kind0);
                    if (live0 == null) return;
                    if (!NearHero(obj0.transform)) return;
                    TrackSpell(obj0.transform, live0, kind0);
                    return;
                }

                GameObject obj = __instance.storeObject.Value;
                string srcName = __instance.gameObject.Value != null ? __instance.gameObject.Value.name : "<null>";
                string spawnedName = obj != null ? obj.name : "<null>";

                if (_seenSpawnCount < 40 && _seenSpawns.Add("CREATE " + fsm + " | " + srcName))
                {
                    _seenSpawnCount++;
                    RangeMod.Instance.LogWarn($"[RM-DIAG] create fsm='{fsm}' src='{srcName}' spawned='{spawnedName}'");
                }

                if (obj == null) return;
                string n = obj.name;

                int kind;
                Func<float> live = SpellMul(n, fsm, true, out kind);
                if (live == null) return;

                Transform top = obj.transform;

                if (!IsUnderHero(top) && !NearHero(top)) return;

                TrackSpell(top, live, kind);
            }
            catch (Exception e)
            {
                RangeMod.Instance.LogWarn($"CreateObject scale failed: {e}");
            }
        }

        private static readonly Dictionary<string, Vector3> _createBase = new Dictionary<string, Vector3>();
    }
}
