using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;

public class PSXLitShaderGUI : ShaderGUI
{
    private ShaderGUI litShaderGUI;

    public override void OnGUI(MaterialEditor materialEditorIn, MaterialProperty[] properties)
    {
        if (litShaderGUI == null)
        {
            var litShaderType = Type.GetType("UnityEditor.Rendering.Universal.ShaderGUI.LitShader,Unity.RenderPipelines.Universal.Editor");
            if (litShaderType != null)
            {
                litShaderGUI = (ShaderGUI)Activator.CreateInstance(litShaderType);
            }
        }

        var useCameraClippingProp = FindProperty("_UseCameraClipping", properties, false);
        var useAffineProp = FindProperty("_UseAffine", properties, false);
        var useVertexJitterProp = FindProperty("_UseVertexJitter", properties, false);
        var useColorPrecisionProp = FindProperty("_UseColorPrecision", properties, false);
        var usePixelationProp = FindProperty("_UsePixelation", properties, false);
        var vertexResolutionProp = FindProperty("_VertexResolution", properties, false);
        var colorPrecisionProp = FindProperty("_ColorPrecision", properties, false);
        var affineThresholdProp = FindProperty("_AffineThreshold", properties, false);
        var textureResolutionProp = FindProperty("_TextureResolution", properties, false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("PSX Effects", EditorStyles.boldLabel);
        
        if (useCameraClippingProp != null)
            materialEditorIn.ShaderProperty(useCameraClippingProp, useCameraClippingProp.displayName);
        
        if (useAffineProp != null)
        {
            materialEditorIn.ShaderProperty(useAffineProp, useAffineProp.displayName);
            if (useAffineProp.floatValue > 0.5f && affineThresholdProp != null)
            {
                EditorGUI.indentLevel++;
                materialEditorIn.ShaderProperty(affineThresholdProp, affineThresholdProp.displayName);
                EditorGUI.indentLevel--;
            }
        }
        
        if (useVertexJitterProp != null)
        {
            materialEditorIn.ShaderProperty(useVertexJitterProp, useVertexJitterProp.displayName);
            if (useVertexJitterProp.floatValue > 0.5f && vertexResolutionProp != null)
            {
                EditorGUI.indentLevel++;
                materialEditorIn.ShaderProperty(vertexResolutionProp, vertexResolutionProp.displayName);
                EditorGUI.indentLevel--;
            }
        }
        
        if (useColorPrecisionProp != null)
        {
            materialEditorIn.ShaderProperty(useColorPrecisionProp, useColorPrecisionProp.displayName);
            if (useColorPrecisionProp.floatValue > 0.5f && colorPrecisionProp != null)
            {
                EditorGUI.indentLevel++;
                materialEditorIn.ShaderProperty(colorPrecisionProp, colorPrecisionProp.displayName);
                EditorGUI.indentLevel--;
            }
        }
        
        if (usePixelationProp != null)
        {
            materialEditorIn.ShaderProperty(usePixelationProp, usePixelationProp.displayName);
            if (usePixelationProp.floatValue > 0.5f && textureResolutionProp != null)
            {
                EditorGUI.indentLevel++;
                materialEditorIn.ShaderProperty(textureResolutionProp, textureResolutionProp.displayName);
                EditorGUI.indentLevel--;
            }
        }

        EditorGUILayout.Space();

        if (litShaderGUI != null)
        {
            litShaderGUI.OnGUI(materialEditorIn, properties);
        }
        else
        {
            base.OnGUI(materialEditorIn, properties);
        }
    }
}

