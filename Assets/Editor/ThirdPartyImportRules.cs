using UnityEditor;
using UnityEngine;

namespace Roguelike.EditorTools
{
    /// <summary>
    /// 第三方素材导入规则（0x72 16×16 地牢包 + Kenney RPG 音效）。
    ///
    /// 贴图：
    ///   - PPU = 16，Point 过滤，无压缩、无 Mipmap
    ///   - FullRect（Tiled 绘制模式需要）
    ///   - 全部按单图导入；动画条已在工程外裁成独立帧
    /// 音频：
    ///   - 短音效 DecompressOnLoad，Vorbis
    ///
    /// 规则只在 ThirdParty / 指定音频目录下生效，不影响项目自有资源。
    /// </summary>
    public sealed class ThirdPartyImportRules : AssetPostprocessor
    {
        private const string TilesetFolder = "Assets/Art/ThirdParty/0x72_DungeonTilesetII/";
        private const string AudioFolder = "Assets/Resources/Audio/KenneyRPG/";
        private const string CustomAudioFolder = "Assets/Resources/Audio/Custom/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(TilesetFolder)) return;

            var importer = (TextureImporter)assetImporter;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
        }

        private void OnPreprocessAudio()
        {
            bool isCustom = assetPath.StartsWith(CustomAudioFolder);
            if (!assetPath.StartsWith(AudioFolder) && !isCustom) return;

            var importer = (AudioImporter)assetImporter;

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            // 自制短音效保留 PCM，避免 Vorbis 在极短音上产生毛刺
            settings.compressionFormat = isCustom ? AudioCompressionFormat.PCM : AudioCompressionFormat.Vorbis;
            settings.quality = 0.7f;
            settings.preloadAudioData = true;

            importer.defaultSampleSettings = settings;
            importer.loadInBackground = false;
            importer.forceToMono = false;
        }
    }
}
