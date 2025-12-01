// MToon10DepthFOVInspector.cs
// Based on MToonInspector.cs from UniVRM (https://github.com/vrm-c/UniVRM)
//
// Original License:
// MIT License
// Copyright (c) 2020 VRM Consortium
// Copyright (c) 2018 Masataka SUMI for MToon

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using VRM10.MToon10;
using VRM10.MToon10.Editor;

namespace DepthAdaptiveFOV.MToon10.Editor
{
    public sealed class MToon10DepthFOVInspector : ShaderGUI
    {
        private static readonly string[] DepthFOVPropertyNames = new[]
        {
            "_DepthFOV_FarFOV",
            "_DepthFOV_FarDistance"
        };

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            var mtoonProps = MToon10Properties.UnityShaderLabNames
                .ToDictionary(x => x.Key, x => FindProperty(x.Value, properties, false));
            var materials = materialEditor.targets.Select(x => x as Material).ToArray();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.HelpBox("MToon10 with Depth Adaptive FOV", MessageType.Info);

            // Depth Adaptive FOV Section
            DrawDepthFOVSection(materialEditor, properties);

            // Editor Settings
            using (new LabelScope("Editor Settings"))
            {
                PopupEnum<MToon10EditorEditMode>("Edit Mode", mtoonProps[MToon10Prop.EditorEditMode], materialEditor);
            }
            var editMode = (MToon10EditorEditMode)(int)mtoonProps[MToon10Prop.EditorEditMode].floatValue;
            var isAdvancedEditMode = editMode == MToon10EditorEditMode.Advanced;

            // Rendering
            using (new LabelScope("Rendering"))
            {
                PopupEnum<MToon10AlphaMode>("Alpha Mode", mtoonProps[MToon10Prop.AlphaMode], materialEditor);
                var alphaMode = (MToon10AlphaMode)(int)mtoonProps[MToon10Prop.AlphaMode].floatValue;

                if (isAdvancedEditMode && alphaMode == MToon10AlphaMode.Transparent)
                {
                    PopupEnum<MToon10TransparentWithZWriteMode>(
                        "Transparent With ZWrite Mode",
                        mtoonProps[MToon10Prop.TransparentWithZWrite],
                        materialEditor
                    );
                }

                if (alphaMode == MToon10AlphaMode.Cutout)
                {
                    materialEditor.ShaderProperty(mtoonProps[MToon10Prop.AlphaCutoff], "Cutoff");
                }

                PopupEnum<MToon10DoubleSidedMode>("Double Sided", mtoonProps[MToon10Prop.DoubleSided], materialEditor);

                if (isAdvancedEditMode)
                {
                    materialEditor.ShaderProperty(mtoonProps[MToon10Prop.RenderQueueOffsetNumber], "RenderQueue Offset");
                }
            }

            // Lighting
            using (new LabelScope("Lighting"))
            {
                materialEditor.TexturePropertySingleLine(
                    new GUIContent("Lit Color, Alpha", "Lit (RGB), Alpha (A)"),
                    mtoonProps[MToon10Prop.BaseColorTexture],
                    mtoonProps[MToon10Prop.BaseColorFactor]
                );
                materialEditor.TexturePropertySingleLine(
                    new GUIContent("Shade Color", "Shade (RGB)"),
                    mtoonProps[MToon10Prop.ShadeColorTexture],
                    mtoonProps[MToon10Prop.ShadeColorFactor]
                );
                if (isAdvancedEditMode)
                {
                    materialEditor.TexturePropertySingleLine(
                        new GUIContent("Normal Map", "Normal Map (RGB)"),
                        mtoonProps[MToon10Prop.NormalTexture],
                        mtoonProps[MToon10Prop.NormalTextureScale]
                    );
                }
                EditorGUILayout.Space();

                if (isAdvancedEditMode)
                {
                    materialEditor.ShaderProperty(mtoonProps[MToon10Prop.ShadingToonyFactor], "Shading Toony");
                    materialEditor.ShaderProperty(mtoonProps[MToon10Prop.ShadingShiftFactor], "Shading Shift");
                    materialEditor.TexturePropertySingleLine(
                        new GUIContent("Additive Shading Shift", "Shading Shift (R)"),
                        mtoonProps[MToon10Prop.ShadingShiftTexture],
                        mtoonProps[MToon10Prop.ShadingShiftTextureScale]
                    );
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("Presets");
                    if (GUILayout.Button("Default"))
                    {
                        mtoonProps[MToon10Prop.ShadingToonyFactor].floatValue = 0.95f;
                        mtoonProps[MToon10Prop.ShadingShiftFactor].floatValue = -0.05f;
                        mtoonProps[MToon10Prop.ShadingShiftTexture].textureValue = null;
                    }
                    if (GUILayout.Button("Lambert"))
                    {
                        mtoonProps[MToon10Prop.ShadingToonyFactor].floatValue = 0.5f;
                        mtoonProps[MToon10Prop.ShadingShiftFactor].floatValue = -0.5f;
                        mtoonProps[MToon10Prop.ShadingShiftTexture].textureValue = null;
                    }
                    if (GUILayout.Button("Cartoon"))
                    {
                        mtoonProps[MToon10Prop.ShadingToonyFactor].floatValue = 1.0f;
                        mtoonProps[MToon10Prop.ShadingShiftFactor].floatValue = 0.0f;
                        mtoonProps[MToon10Prop.ShadingShiftTexture].textureValue = null;
                    }
                    EditorGUILayout.EndHorizontal();

                    GUILayout.BeginVertical(GUI.skin.box);
                    materialEditor.ShaderProperty(mtoonProps[MToon10Prop.ShadingToonyFactor], "Shading Toony");
                    materialEditor.ShaderProperty(mtoonProps[MToon10Prop.ShadingShiftFactor], "Shading Shift");
                    GUILayout.EndVertical();
                }

                if (mtoonProps[MToon10Prop.ShadingShiftTexture].textureValue == null)
                {
                    var toony = mtoonProps[MToon10Prop.ShadingToonyFactor].floatValue;
                    var shift = mtoonProps[MToon10Prop.ShadingShiftFactor].floatValue;
                    if (toony - shift < 1.0f - 0.001f)
                    {
                        EditorGUILayout.HelpBox("The lit area includes non-lit area.", MessageType.Warning);
                    }
                }
            }

            // Global Illumination
            if (isAdvancedEditMode)
            {
                using (new LabelScope("Global Illumination"))
                {
                    materialEditor.ShaderProperty(mtoonProps[MToon10Prop.GiEqualizationFactor], "GI Equalization");
                }
            }

            // Emission
            using (new LabelScope("Emission"))
            {
                materialEditor.TexturePropertySingleLine(
                    new GUIContent("Emission", "Emission (RGB)"),
                    mtoonProps[MToon10Prop.EmissiveTexture],
                    mtoonProps[MToon10Prop.EmissiveFactor]
                );
            }

            // Rim Lighting
            using (new LabelScope("Rim Lighting"))
            {
                if (isAdvancedEditMode)
                {
                    materialEditor.TexturePropertySingleLine(
                        new GUIContent("Mask", "Rim Lighting Mask (RGB)"),
                        mtoonProps[MToon10Prop.RimMultiplyTexture]
                    );
                    materialEditor.ShaderProperty(
                        mtoonProps[MToon10Prop.RimLightingMixFactor],
                        new GUIContent("LightingMix")
                    );
                    EditorGUILayout.Space();
                }

                using (new LabelScope("Matcap"))
                {
                    materialEditor.TexturePropertySingleLine(
                        new GUIContent("Matcap", "Matcap (RGB)"),
                        mtoonProps[MToon10Prop.MatcapTexture],
                        mtoonProps[MToon10Prop.MatcapColorFactor]
                    );
                    EditorGUILayout.Space();
                }

                using (new LabelScope("Parametric Rim"))
                {
                    materialEditor.ShaderProperty(
                        mtoonProps[MToon10Prop.ParametricRimColorFactor],
                        new GUIContent("Color")
                    );
                    materialEditor.ShaderProperty(
                        mtoonProps[MToon10Prop.ParametricRimFresnelPowerFactor],
                        new GUIContent("Fresnel Power")
                    );
                    materialEditor.ShaderProperty(
                        mtoonProps[MToon10Prop.ParametricRimLiftFactor],
                        new GUIContent("Lift")
                    );
                }
            }

            // Outline
            using (new LabelScope("Outline"))
            {
                PopupEnum<MToon10OutlineMode>("Outline Mode", mtoonProps[MToon10Prop.OutlineWidthMode], materialEditor);
                var hasOutline = (MToon10OutlineMode)(int)mtoonProps[MToon10Prop.OutlineWidthMode].floatValue != MToon10OutlineMode.None;

                if (hasOutline)
                {
                    materialEditor.TexturePropertySingleLine(
                        new GUIContent("Outline Width", "Outline Width (G) [meter]"),
                        mtoonProps[MToon10Prop.OutlineWidthMultiplyTexture],
                        mtoonProps[MToon10Prop.OutlineWidthFactor]
                    );
                    materialEditor.ShaderProperty(
                        mtoonProps[MToon10Prop.OutlineColorFactor],
                        new GUIContent("Outline Color")
                    );
                    if (isAdvancedEditMode)
                    {
                        materialEditor.ShaderProperty(
                            mtoonProps[MToon10Prop.OutlineLightingMixFactor],
                            new GUIContent("Outline LightingMix")
                        );
                    }
                }
            }

            // UV Animation
            if (isAdvancedEditMode)
            {
                using (new LabelScope("UV Animation"))
                {
                    materialEditor.TexturePropertySingleLine(
                        new GUIContent("Mask", "Mask (B)"),
                        mtoonProps[MToon10Prop.UvAnimationMaskTexture]
                    );
                    materialEditor.ShaderProperty(
                        mtoonProps[MToon10Prop.UvAnimationScrollXSpeedFactor],
                        new GUIContent("Translate X")
                    );
                    materialEditor.ShaderProperty(
                        mtoonProps[MToon10Prop.UvAnimationScrollYSpeedFactor],
                        new GUIContent("Translate Y")
                    );
                    materialEditor.ShaderProperty(
                        mtoonProps[MToon10Prop.UvAnimationRotationSpeedFactor],
                        new GUIContent("Rotation")
                    );
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                Validate(materials);
            }

            // Debug
            if (isAdvancedEditMode && materials.Length == 1)
            {
                var mat = materials[0];
                using (new LabelScope("Debug"))
                {
                    EditorGUILayout.LabelField("RenderQueue", mat.renderQueue.ToString());
                    EditorGUILayout.LabelField("Cull", ((CullMode)mtoonProps[MToon10Prop.UnityCullMode].floatValue).ToString());
                    EditorGUILayout.LabelField("SrcBlend", ((BlendMode)mtoonProps[MToon10Prop.UnitySrcBlend].floatValue).ToString());
                    EditorGUILayout.LabelField("DstBlend", ((BlendMode)mtoonProps[MToon10Prop.UnityDstBlend].floatValue).ToString());
                    EditorGUILayout.LabelField("ZWrite", ((UnityZWriteMode)mtoonProps[MToon10Prop.UnityZWrite].floatValue).ToString());
                    EditorGUILayout.LabelField("AlphaToMask", ((UnityAlphaToMaskMode)mtoonProps[MToon10Prop.UnityAlphaToMask].floatValue).ToString());
                    EditorGUILayout.LabelField("Enabled Keywords", string.Join("\n", mat.shaderKeywords), EditorStyles.textArea);
                }
            }
        }

        private void DrawDepthFOVSection(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            var farFOV = FindProperty("_DepthFOV_FarFOV", properties);
            var farDistance = FindProperty("_DepthFOV_FarDistance", properties);

            using (new LabelScope("Depth Adaptive FOV"))
            {
                materialEditor.ShaderProperty(farFOV, new GUIContent(
                    "Far FOV",
                    "Target FOV at far distance (degrees). Objects at infinity will approach this FOV."
                ));
                materialEditor.ShaderProperty(farDistance, new GUIContent(
                    "Far Distance",
                    "Distance (meters) where FOV is halfway between camera FOV and Far FOV."
                ));

                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(
                    "Near objects use camera's native FOV.\n" +
                    "Far objects approach the specified Far FOV.\n" +
                    "Formula: t = depth / (depth + farDistance)",
                    MessageType.None
                );
            }
        }

        private static void Validate(Material[] materials)
        {
            foreach (var material in materials)
            {
                new MToonValidator(material).Validate();
            }
        }

        private static bool PopupEnum<T>(string name, MaterialProperty property, MaterialEditor editor) where T : struct
        {
            if (property == null) return false;

            EditorGUI.showMixedValue = property.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            var ret = EditorGUILayout.Popup(name, (int)property.floatValue, Enum.GetNames(typeof(T)));
            var changed = EditorGUI.EndChangeCheck();
            if (changed)
            {
                editor.RegisterPropertyChangeUndo($"Change {name}");
                property.floatValue = ret;
            }

            EditorGUI.showMixedValue = false;
            return changed;
        }

        private readonly struct LabelScope : IDisposable
        {
            public LabelScope(string label)
            {
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(GUI.skin.box);
            }

            public void Dispose()
            {
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }
    }

    internal enum UnityZWriteMode
    {
        Off = 0,
        On = 1,
    }

    internal enum UnityAlphaToMaskMode
    {
        Off = 0,
        On = 1,
    }
}
