using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 씬 빌더에서 사용할 단순 도형 스프라이트를 PNG 에셋으로 생성하는 에디터 헬퍼.
/// 별도 아트 리소스 없이 동그란 버튼을 만들기 위해 사용한다.
/// </summary>
public static class EditorSpriteGenerator
{
    /// <summary>
    /// 지정 경로에 가장자리가 부드러운 흰색 원 스프라이트를 보장한다.
    /// 이미 있으면 그대로 불러오고, 없으면 PNG 를 생성해 스프라이트로 임포트한다.
    /// </summary>
    /// <param name="assetPath">"Assets/..." 형태의 PNG 경로</param>
    /// <param name="size">텍스처 한 변의 픽셀 수</param>
    public static Sprite EnsureCircleSprite(string assetPath, int size = 256)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (existing != null)
            return existing;

        WriteCirclePng(assetPath, size);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

        // 텍스처를 UI 스프라이트로 임포트하도록 설정
        var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    /// <summary>
    /// 안티에일리어싱된 흰색 원을 그린 PNG 파일을 디스크에 쓴다.
    /// </summary>
    static void WriteCirclePng(string assetPath, int size)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var center = (size - 1) * 0.5f;
        var radius = size * 0.5f - 1f;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                // 중심에서의 거리로 가장자리 1px 구간을 부드럽게 처리
                var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                var alpha = Mathf.Clamp01(radius - distance + 0.5f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        var directory = Path.GetDirectoryName(assetPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        File.WriteAllBytes(assetPath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
    }
}
