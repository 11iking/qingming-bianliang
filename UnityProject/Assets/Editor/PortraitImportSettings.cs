using UnityEditor;
using UnityEngine;
public sealed class PortraitImportSettings : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith("Assets/Resources/Portraits/"))return;
        var importer=(TextureImporter)assetImporter;
        importer.textureType=TextureImporterType.Default;
        importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;
        importer.mipmapEnabled=false;importer.sRGBTexture=true;
        importer.maxTextureSize=1024;importer.textureCompression=TextureImporterCompression.CompressedHQ;
        importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
    }
}
