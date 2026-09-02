using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DCGO.UI
{
    public class DCGOThemeManager : MonoBehaviour
    {
        public static DCGOThemeManager Instance { get; private set; }

        // Color Palette Constants - DCGO Visual Identity
        public static readonly Color ColorCyberCyan            = new Color(0.000f, 0.824f, 1.000f, 1.0f); // #00D2FF
        public static readonly Color ColorNeonOrange           = new Color(1.000f, 0.420f, 0.000f, 1.0f); // #FF6B00
        public static readonly Color ColorCrimsonRed           = new Color(1.000f, 0.165f, 0.294f, 1.0f); // #FF2A4B
        public static readonly Color ColorDeepMatrixCharcoal   = new Color(0.039f, 0.055f, 0.090f, 1.0f); // #0A0E17
        public static readonly Color ColorCarbonFiberGrey      = new Color(0.165f, 0.180f, 0.239f, 1.0f); // #2A2E3D

        private Material _bgMaterial;
        private Material _shieldBlueMaterial;
        private Material _shieldOrangeMaterial;
        private Material _glitchLogoMaterial;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeMaterials();
        }

        public void InitializeMaterials()
        {
            Shader bgShader = Shader.Find("UI/DCGO/CyberpunkBackground");
            if (bgShader != null)
            {
                _bgMaterial = new Material(bgShader);
                _bgMaterial.name = "UI_CyberpunkBackground_Runtime";
                _bgMaterial.SetColor("_DarkBgColor", ColorDeepMatrixCharcoal);
                _bgMaterial.SetColor("_CyanColor", ColorCyberCyan);
                _bgMaterial.SetColor("_OrangeColor", ColorNeonOrange);
                _bgMaterial.SetColor("_CrimsonColor", ColorCrimsonRed);
                _bgMaterial.SetColor("_CarbonColor", ColorCarbonFiberGrey);
            }

            Shader shieldShader = Shader.Find("UI/DCGO/HolographicGlassShield");
            if (shieldShader != null)
            {
                // Left Shield (Blue / Cyber Cyan @ 30%)
                _shieldBlueMaterial = new Material(shieldShader);
                _shieldBlueMaterial.name = "UI_GlassShield_Cyan";
                _shieldBlueMaterial.SetColor("_Color", new Color(ColorCyberCyan.r, ColorCyberCyan.g, ColorCyberCyan.b, 0.3f));
                _shieldBlueMaterial.SetColor("_BorderColor", ColorCyberCyan);
                _shieldBlueMaterial.SetColor("_CarbonBgColor", ColorCarbonFiberGrey);

                // Right Shield (Orange / Neon Orange @ 30%)
                _shieldOrangeMaterial = new Material(shieldShader);
                _shieldOrangeMaterial.name = "UI_GlassShield_Orange";
                _shieldOrangeMaterial.SetColor("_Color", new Color(ColorNeonOrange.r, ColorNeonOrange.g, ColorNeonOrange.b, 0.3f));
                _shieldOrangeMaterial.SetColor("_BorderColor", ColorNeonOrange);
                _shieldOrangeMaterial.SetColor("_CarbonBgColor", ColorCarbonFiberGrey);
            }

            Shader logoShader = Shader.Find("UI/DCGO/GlitchLogo");
            if (logoShader != null)
            {
                _glitchLogoMaterial = new Material(logoShader);
                _glitchLogoMaterial.name = "UI_GlitchLogo_Runtime";
                _glitchLogoMaterial.SetColor("_Color", ColorCyberCyan);
                _glitchLogoMaterial.SetColor("_CrimsonGlow", ColorCrimsonRed);
                _glitchLogoMaterial.SetColor("_CarbonPattern", ColorCarbonFiberGrey);
                _glitchLogoMaterial.SetColor("_GlitchColor", ColorNeonOrange);
            }
        }

        public static Material GetBackgroundMaterial()
        {
            if (Instance != null && Instance._bgMaterial != null)
                return Instance._bgMaterial;

            Shader s = Shader.Find("UI/DCGO/CyberpunkBackground");
            return s != null ? new Material(s) : null;
        }

        public static Material GetShieldMaterial(bool isOrangeRight)
        {
            if (Instance != null)
            {
                if (isOrangeRight && Instance._shieldOrangeMaterial != null)
                    return Instance._shieldOrangeMaterial;
                if (!isOrangeRight && Instance._shieldBlueMaterial != null)
                    return Instance._shieldBlueMaterial;
            }

            Shader s = Shader.Find("UI/DCGO/HolographicGlassShield");
            if (s == null) return null;
            Material m = new Material(s);
            Color c = isOrangeRight ? ColorNeonOrange : ColorCyberCyan;
            m.SetColor("_Color", new Color(c.r, c.g, c.b, 0.3f));
            m.SetColor("_BorderColor", c);
            return m;
        }

        public static Material GetGlitchLogoMaterial()
        {
            if (Instance != null && Instance._glitchLogoMaterial != null)
                return Instance._glitchLogoMaterial;

            Shader s = Shader.Find("UI/DCGO/GlitchLogo");
            return s != null ? new Material(s) : null;
        }

        public static void ApplyBackgroundShader(Image targetImage)
        {
            if (targetImage == null) return;
            Material mat = GetBackgroundMaterial();
            if (mat != null)
            {
                targetImage.material = mat;
                targetImage.color = Color.white;
            }
        }

        public static void ApplyHolographicShield(Image targetImage, bool isOrangeRight = false)
        {
            if (targetImage == null) return;
            Material mat = GetShieldMaterial(isOrangeRight);
            if (mat != null)
            {
                targetImage.material = mat;
                targetImage.color = Color.white;
            }
        }

        public static void ApplyGlitchLogo(Image targetImage)
        {
            if (targetImage == null) return;
            Material mat = GetGlitchLogoMaterial();
            if (mat != null)
            {
                targetImage.material = mat;
                targetImage.color = Color.white;
            }
        }
    }
}
