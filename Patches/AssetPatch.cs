using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using CustomAlbums.Data;
using CustomAlbums.Managers;
using Il2CppAssets.Scripts.PeroTools.Commons;
using Il2CppAssets.Scripts.PeroTools.GeneralLocalization;
using Il2CppAssets.Scripts.PeroTools.Managers;
using Il2CppInterop.Runtime;
using Il2CppPeroTools2.Resources;
using MelonLoader.NativeUtils;
using UnityEngine;
using AudioManager = CustomAlbums.Managers.AudioManager;
using Logger = CustomAlbums.Utilities.Logger;
using Object = UnityEngine.Object;

namespace CustomAlbums.Patches;

/// <summary>
///     The meat and potatoes of this whole mod.
///     This class patches the LoadFromName method and adds custom albums and other custom information to the game's memory.
///     It's okay to not understand what's going on here because you don't need to interface with it.
///     However, this provides an example of how to patch generic methods if you ever need to do so.
///     As such, this class has been heavily documented.
/// </summary>
internal class AssetPatch
{
    // this is the object that will handle the injection on the native method
    private static readonly NativeHook<LoadFromNameDelegate> Hook = new();

    private static readonly Dictionary<string, Object> LoadedAssets = new();

    private static readonly string[] AssetSuffixes =
    {
        "_demo",
        "_music",
        "_cover",
        "_map1",
        "_map2",
        "_map3",
        "_map4"
    };

    private static readonly Logger Logger = new(nameof(AssetPatch));

    // FOR DEVELOPERS: unless you are patching a generic method, or some other weird edge case, please DO NOT use native hooks
    internal static unsafe void AttachHook()
    {
        // the string in GetNestedType comes from metadata digging in ResourcesManager
        var type = typeof(ResourcesManager).GetNestedType("MethodInfoStoreGeneric_LoadFromName_Public_T_String_0`1", BindingFlags.NonPublic)
            ?.MakeGenericType(typeof(TextAsset));
        if (type != null)
        {
            // the method is stored as a pointer in the field named Pointer, so we get that field using reflection
            // this will give us the memory address of the original LoadFromName
            var originalLfn = *(IntPtr*)(IntPtr)type.GetField("Pointer", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(type)!;

            // create a pointer for our new method to be called instead
            // this is Cdecl because this is going to be called in an unmanaged context
            delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, IntPtr> detourPtr = &LoadFromNamePatch;

            // set the hook so that LoadFromNamePatch runs instead of the original LoadFromName
            Hook.Detour = (IntPtr)detourPtr;
            Hook.Target = originalLfn;
        }

        // Attach the hook so that it's active and will actually do what it's supposed to
        Hook.Attach();
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static IntPtr LoadFromNamePatch(IntPtr instance, IntPtr ptrAssetName, IntPtr nativeMethodInfo)
    {
        // call the original method to get the asset pointer, convert IntPtr assetName to managed string as well
        var assetPtr = Hook.Trampoline(instance, ptrAssetName, nativeMethodInfo);
        var assetName = IL2CPP.Il2CppStringToManaged(ptrAssetName);

        if (assetName is null or "LocalizationSettings") return assetPtr;

        if (HasNonNullCachedAsset(assetName, out var asset))
        {
            Logger.Msg(AudioManager.SwitchLoad(assetName)
                ? $"Resuming async load of {assetName}"
                : $"Using cache for {assetName}");
            return asset.Pointer;
        }

        var cacheAsset = true;
        var language = SingletonScriptableObject<LocalizationSettings>.instance.GetActiveOption("Language");
        Object newAsset = null;

        // adding album assets
        if (assetName == AlbumManager.JsonName)
            newAsset = CreateInfoJsonAsset(assetName);

        else if (assetName == $"albums_{language}")
            newAsset = CreateAlbumTitleAsset(assetName, assetPtr, language);

        else if (assetName == $"{AlbumManager.JsonName}_{language}")
            newAsset = CreateAlbumInfoAsset(assetName);

        if (assetPtr == IntPtr.Zero && (assetName.StartsWith("fs_") || assetName.StartsWith("pkg_")))
            cacheAsset = LoadCustomAsset(assetName, out newAsset);

        if (newAsset == null) return assetPtr;

        // Add to cache
        if (cacheAsset)
        {
            LoadedAssets.Add(assetName, newAsset);
            Logger.Msg($"Cached {assetName}");
        }
        else
            Logger.Msg($"Loaded {assetName}");

        return newAsset.Pointer;
    }

    /// <summary>
    ///     Checks if the asset is already cached
    ///     If the asset is cached, it will be returned and the method will return true
    ///     If the asset is null, it will be removed from the cache and the method will return false
    /// </summary>
    /// <param name="assetName"></param>
    /// <param name="asset"></param>
    /// <returns>Has non null cached asset</returns>
    private static bool HasNonNullCachedAsset(string assetName, out Object asset)
    {
        if (!LoadedAssets.TryGetValue(assetName, out asset))
            return false;

        if (asset == null)
        {
            Logger.Msg("Removing null asset");
            LoadedAssets.Remove(assetName);
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Creates a object (Text Asset) containing all the album info
    /// </summary>
    /// <param name="assetName"></param>
    /// <returns></returns>
    private static Object CreateInfoJsonAsset(string assetName)
    {
        var albumJson = new JsonArray();
        foreach (var (key, album) in AlbumManager.LoadedAlbums)
        {
            var info = album.Info;
            var infoObject = new JsonObject
            {
                { "uid", $"{AlbumManager.Uid}-{album.Index}" },
                { "name", info.Name },
                { "author", info.Author },
                { "bpm", info.Bpm },
                { "music", $"{key}_music" },
                { "demo", $"{key}_demo" },
                { "cover", $"{key}_cover" },
                { "noteJson", $"{key}_map" },
                { "scene", info.Scene }
            };

            FillInfoJsonObject(info, infoObject);

            albumJson.Add(infoObject);
        }

        Object newAsset = CreateTextAsset(assetName, JsonSerializer.Serialize(albumJson));
        if (!Singleton<ConfigManager>.instance.m_Dictionary.ContainsKey(assetName))
            Singleton<ConfigManager>.instance.Add(assetName, ((TextAsset)newAsset).text);

        return newAsset;
    }

    /// <summary>
    ///     Fills the info object with the info from the album.
    /// </summary>
    /// <param name="info"></param>
    /// <param name="infoObject"></param>
    private static void FillInfoJsonObject(AlbumInfo info, JsonObject infoObject)
    {
        if (!string.IsNullOrEmpty(info.LevelDesigner))
            infoObject.Add("levelDesigner", info.LevelDesigner);
        if (!string.IsNullOrEmpty(info.LevelDesigner1))
            infoObject.Add("levelDesigner1", info.LevelDesigner1);
        if (!string.IsNullOrEmpty(info.LevelDesigner2))
            infoObject.Add("levelDesigner2", info.LevelDesigner2);
        if (!string.IsNullOrEmpty(info.LevelDesigner3))
            infoObject.Add("levelDesigner3", info.LevelDesigner3);
        if (!string.IsNullOrEmpty(info.LevelDesigner4))
            infoObject.Add("levelDesigner4", info.LevelDesigner4);
        if (!string.IsNullOrEmpty(info.Difficulty1))
            infoObject.Add("difficulty1", info.Difficulty1);
        if (!string.IsNullOrEmpty(info.Difficulty2))
            infoObject.Add("difficulty2", info.Difficulty2);
        if (!string.IsNullOrEmpty(info.Difficulty3))
            infoObject.Add("difficulty3", info.Difficulty3);
        if (!string.IsNullOrEmpty(info.Difficulty4))
            infoObject.Add("difficulty4", info.Difficulty4);
    }

    /// <summary>
    ///     Create the object (Text Asset) containing the album title
    /// </summary>
    /// <param name="assetName"></param>
    /// <param name="assetPtr"></param>
    /// <param name="language"></param>
    /// <returns></returns>
    private static Object CreateAlbumTitleAsset(string assetName, IntPtr assetPtr, string language)
    {
        var textAsset = new TextAsset(assetPtr);
        var jsonArray = JsonSerializer.Deserialize<JsonArray>(textAsset.text);
        jsonArray.Add(new
        {
            title = AlbumManager.Languages[language]
        });

        Object newAsset = CreateTextAsset(assetName, JsonSerializer.Serialize(jsonArray));
        if (!Singleton<ConfigManager>.instance.m_Dictionary.ContainsKey(assetName))
            Singleton<ConfigManager>.instance.Add(assetName, ((TextAsset)newAsset).text);

        return newAsset;
    }

    /// <summary>
    ///     Creates the object (Text Asset) containing the chart name and author
    /// </summary>
    /// <param name="assetName"></param>
    /// <returns></returns>
    private static Object CreateAlbumInfoAsset(string assetName)
    {
        var jsonArray = new JsonArray();
        foreach (var albumInfo in AlbumManager.LoadedAlbums.Select(x => x.Value.Info))
        {
            jsonArray.Add(new
            {
                name = albumInfo.Name,
                author = albumInfo.Author
            });
        }

        Object newAsset = CreateTextAsset(assetName, JsonSerializer.Serialize(jsonArray));
        if (!Singleton<ConfigManager>.instance.m_Dictionary.ContainsKey(assetName))
            Singleton<ConfigManager>.instance.Add(assetName, ((TextAsset)newAsset).text);
        return newAsset;
    }

    /// <summary>
    ///     Loads the custom asset
    /// </summary>
    /// <param name="assetName"></param>
    /// <param name="newAsset"></param>
    /// <returns>Cache asset</returns>
    private static bool LoadCustomAsset(string assetName, out Object newAsset)
    {
        newAsset = null;
        var cacheAsset = true;
        var suffix = AssetSuffixes.FirstOrDefault(assetName.EndsWith);

        if (string.IsNullOrEmpty(suffix)) return true;

        var albumKey = assetName[..^suffix.Length];
        AlbumManager.LoadedAlbums.TryGetValue(albumKey, out var album);

        if (suffix.StartsWith("_map"))
        {
            newAsset = album?.Sheets[int.Parse(suffix[..^4])].StageInfo;
            // Do not cache any StageInfo
            cacheAsset = false;
        }
        else
            switch (suffix)
            {
                case "_demo":
                    newAsset = album?.Demo;
                    break;

                case "_music":
                    newAsset = album?.Music;
                    break;

                case "_cover":
                    newAsset = album?.GetCover();
                    break;

                default:
                    Logger.Msg($"Unknown suffix: {suffix}");
                    break;
            }

        return cacheAsset;
    }

    /// <summary>
    ///     Creates a Text Asset with the given name and text
    /// </summary>
    /// <param name="name"></param>
    /// <param name="text"></param>
    /// <returns></returns>
    private static TextAsset CreateTextAsset(string name, string text)
    {
        var newAsset = new TextAsset(text)
        {
            name = name
        };
        return newAsset;
    }

    // the function signature for LoadFromName
    // we need to use Cdecl here since the method we are patching has been compiled to C++
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr LoadFromNameDelegate(IntPtr instance, IntPtr ptrAssetName, IntPtr info);
}