using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace DCGO.UI
{
    public class DCGOThemeRuntimeApplier : MonoBehaviour
    {
        public static DCGOThemeRuntimeApplier Instance { get; private set; }

        // Color Palette Constants - DCGO Visual Identity (design.md)
        public static readonly Color ColorCyberCyan          = new Color(0.000f, 0.824f, 1.000f, 1.000f); // #00D2FF
        public static readonly Color ColorNeonOrange         = new Color(1.000f, 0.420f, 0.000f, 1.000f); // #FF6B00
        public static readonly Color ColorCrimsonRed         = new Color(1.000f, 0.165f, 0.294f, 1.000f); // #FF2A4B
        public static readonly Color ColorDeepMatrixCharcoal = new Color(0.039f, 0.055f, 0.090f, 1.000f); // #0A0E17
        public static readonly Color ColorCarbonFiberGrey    = new Color(0.165f, 0.180f, 0.239f, 0.920f); // #2A2E3D
        public static readonly Color ColorButtonCyan         = new Color(0.000f, 0.700f, 0.900f, 1.000f);

        private static Material _cyberpunkBgMaterial;
        private static Material _holographicGlassCyanMaterial;
        private static Material _holographicGlassOrangeMaterial;

        private static readonly ColorBlock ThemedButtonBlock = new ColorBlock
        {
            normalColor = Color.white,
            highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1.00f),
            pressedColor = new Color(0.60f, 0.60f, 0.60f, 1.00f),
            selectedColor = new Color(1.15f, 1.15f, 1.15f, 1.00f),
            disabledColor = new Color(0.50f, 0.50f, 0.50f, 0.50f),
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitializeRuntimeTheme()
        {
            EnsureInstance();
            ApplyThemeToActiveScene();
        }

        private static void EnsureInstance()
        {
            if (Instance != null) return;

            GameObject applierGO = new GameObject("DCGOThemeRuntimeApplier");
            Instance = applierGO.AddComponent<DCGOThemeRuntimeApplier>();
            DontDestroyOnLoad(applierGO);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyThemeToActiveScene();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeShaderMaterials();
        }

        private static void InitializeShaderMaterials()
        {
            if (_cyberpunkBgMaterial == null)
            {
                Shader bgShader = Shader.Find("UI/DCGO/CyberpunkBackground");
                if (bgShader != null)
                {
                    _cyberpunkBgMaterial = new Material(bgShader) { name = "CyberpunkBg_Runtime" };
                    _cyberpunkBgMaterial.SetColor("_DarkBgColor", ColorDeepMatrixCharcoal);
                    _cyberpunkBgMaterial.SetColor("_CyanColor", ColorCyberCyan);
                    _cyberpunkBgMaterial.SetColor("_OrangeColor", ColorNeonOrange);
                    _cyberpunkBgMaterial.SetColor("_CrimsonColor", ColorCrimsonRed);
                    _cyberpunkBgMaterial.SetColor("_CarbonColor", ColorCarbonFiberGrey);
                }
            }

            if (_holographicGlassCyanMaterial == null)
            {
                Shader glassShader = Shader.Find("UI/DCGO/HolographicGlassShield");
                if (glassShader != null)
                {
                    _holographicGlassCyanMaterial = new Material(glassShader) { name = "GlassShield_Cyan_Runtime" };
                    _holographicGlassCyanMaterial.SetColor("_Color", new Color(ColorCyberCyan.r, ColorCyberCyan.g, ColorCyberCyan.b, 0.35f));
                    _holographicGlassCyanMaterial.SetColor("_BorderColor", ColorCyberCyan);
                    _holographicGlassCyanMaterial.SetColor("_CarbonBgColor", ColorCarbonFiberGrey);
                }
            }

            if (_holographicGlassOrangeMaterial == null)
            {
                Shader glassShader = Shader.Find("UI/DCGO/HolographicGlassShield");
                if (glassShader != null)
                {
                    _holographicGlassOrangeMaterial = new Material(glassShader) { name = "GlassShield_Orange_Runtime" };
                    _holographicGlassOrangeMaterial.SetColor("_Color", new Color(ColorNeonOrange.r, ColorNeonOrange.g, ColorNeonOrange.b, 0.35f));
                    _holographicGlassOrangeMaterial.SetColor("_BorderColor", ColorNeonOrange);
                    _holographicGlassOrangeMaterial.SetColor("_CarbonBgColor", ColorCarbonFiberGrey);
                }
            }
        }

        public static void ApplyThemeToActiveScene()
        {
            if (SceneManager.GetActiveScene().name == "ReplayScene")
            {
                return;
            }

            InitializeShaderMaterials();

            Image[] images = System.Array.ConvertAll(UnityEngine.Object.FindObjectsOfType(typeof(Image), true), x => (Image)x);
            foreach (Image img in images)
            {
                ApplyThemeToImage(img);
            }

            Selectable[] selectables = System.Array.ConvertAll(UnityEngine.Object.FindObjectsOfType(typeof(Selectable), true), x => (Selectable)x);
            foreach (Selectable sel in selectables)
            {
                if (sel.transition == Selectable.Transition.ColorTint)
                {
                    sel.colors = ThemedButtonBlock;
                }
            }
        }

        public static void ApplyThemeToImage(Image image)
        {
            if (image == null) return;

            string nameLower = image.gameObject.name.ToLowerInvariant();

            // Exclude Card Art, Details, Logos, Replays
            if (nameLower.Contains("logo") || nameLower.Contains("background_home") || nameLower.Contains("bg_home") || nameLower.Contains("replay"))
            {
                return;
            }

            for (Transform t = image.transform; t != null; t = t.parent)
            {
                string pName = t.name.ToLowerInvariant();
                if (pName.Contains("cardimage") || pName.Contains("detailcard") || pName.Contains("cardart"))
                {
                    return;
                }
            }

            bool hasSprite = image.sprite != null;

            // 1. Popup Windows / Mulligan Panels / Dialog Boxes -> Glass Shield Shader
            bool isWindowOrPopup = nameLower.Contains("window") || nameLower.Contains("panel") ||
                                   nameLower.Contains("dialog") || nameLower.Contains("mulligan") ||
                                   nameLower.Contains("shield") || nameLower.Contains("popup");

            if (isWindowOrPopup)
            {
                bool isOrange = nameLower.Contains("opponent") || nameLower.Contains("right");
                Material glassMat = isOrange ? _holographicGlassOrangeMaterial : _holographicGlassCyanMaterial;
                if (glassMat != null)
                {
                    image.material = glassMat;
                    image.color = Color.white;
                    return;
                }
            }

            // 2. Full Backgrounds / Canvas Masks -> Cyberpunk Background Shader
            RectTransform rt = image.rectTransform;
            bool isFullBackground = nameLower.Contains("background") || nameLower.Contains("bg") ||
                                    (rt.parent != null && rt.parent.GetComponent<Canvas>() != null && nameLower.Contains("mask"));

            if (isFullBackground && _cyberpunkBgMaterial != null)
            {
                image.material = _cyberpunkBgMaterial;
                image.color = Color.white;
                return;
            }

            // 3. Process un-sprited / white elements
            if (!hasSprite)
            {
                Color c = image.color;
                bool isWhiteOrDefault = c.a >= 0.5f && c.r >= 0.85f && c.g >= 0.85f && c.b >= 0.85f;
                bool hasTextChild = false;

                for (int i = 0; i < image.transform.childCount; i++)
                {
                    Transform child = image.transform.GetChild(i);
                    if (child.GetComponent<Text>() != null || child.GetComponent<TextMeshProUGUI>() != null)
                    {
                        hasTextChild = true;
                        break;
                    }
                }

                if (isWhiteOrDefault || hasTextChild)
                {
                    GameObject go = image.gameObject;

                    if (go.GetComponent<Button>() != null)
                    {
                        image.color = ColorButtonCyan;
                        if (_holographicGlassCyanMaterial != null)
                        {
                            image.material = _holographicGlassCyanMaterial;
                        }
                    }
                    else if (go.GetComponent<Toggle>() != null || go.GetComponentInParent<Toggle>() != null)
                    {
                        bool isOrange = nameLower.Contains("opponent") || nameLower.Contains("right");
                        image.color = isOrange ? ColorNeonOrange : ColorCyberCyan;
                    }
                    else
                    {
                        image.color = ColorCarbonFiberGrey;
                    }
                }
            }
        }
    }
}
