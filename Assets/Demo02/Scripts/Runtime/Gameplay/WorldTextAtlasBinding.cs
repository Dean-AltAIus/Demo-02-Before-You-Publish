using UnityEngine;

namespace Altaius.Demo02.Gameplay
{
    /// <summary>Keeps a depth-tested world label bound to its dynamic font atlas.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TextMesh), typeof(MeshRenderer))]
    public sealed class WorldTextAtlasBinding : MonoBehaviour
    {
        static readonly int MainTexture = Shader.PropertyToID("_MainTex");
        TextMesh label;
        MeshRenderer target;
        Texture boundAtlas;
        MaterialPropertyBlock properties;

        void OnEnable()
        {
            label = GetComponent<TextMesh>();
            target = GetComponent<MeshRenderer>();
            Font.textureRebuilt += OnFontRebuilt;
            RefreshAtlas();
        }

        void OnDisable()
        {
            Font.textureRebuilt -= OnFontRebuilt;
            boundAtlas = null;
        }

        void OnValidate() => RefreshAtlas();
        void OnWillRenderObject() => RefreshAtlas();

        void OnFontRebuilt(Font font)
        {
            if (!label || label.font != font) return;
            boundAtlas = null;
            RefreshAtlas();
        }

        public void RefreshAtlas()
        {
            if (!label) label = GetComponent<TextMesh>();
            if (!target) target = GetComponent<MeshRenderer>();
            if (!label || !target || !label.font || !label.font.material) return;
            var atlas = label.font.material.mainTexture;
            if (!atlas || atlas == boundAtlas) return;
            if (properties == null) properties = new MaterialPropertyBlock();
            // Preserve other authored or runtime renderer properties and materials.
            target.GetPropertyBlock(properties);
            properties.SetTexture(MainTexture, atlas);
            target.SetPropertyBlock(properties);
            boundAtlas = atlas;
        }
    }
}
