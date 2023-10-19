using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using Il2CppPeroTools2.Resources;
using MelonLoader.NativeUtils;
using UnityEngine;
using Il2CppAssets.Scripts.PeroTools.Commons;
using Il2CppAssets.Scripts.PeroTools.GeneralLocalization;
using Il2CppInterop.Runtime;
using System.Text.Json.Nodes;
using CustomAlbums.Managers;
using Il2CppAssets.Scripts.PeroTools.Managers;
using Logger = CustomAlbums.Utilities.Logger;

namespace CustomAlbums.Patches
{
    /// <summary>
    /// The meat and potatoes of this whole mod.
    /// This class patches the LoadFromName method and adds custom albums and other custom information to the game's memory.
    /// It's okay to not understand what's going on here because you don't need to interface with it.
    /// However, this provides an example of how to patch generic methods if you ever need to do so.
    /// As such, this class has been heavily documented.
    /// </summary>
    internal class AssetPatch
    {
        // the function signature for LoadFromName
        // we need to use Cdecl here since the method we are patching has been compiled to C++
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr LoadFromNameDelegate(IntPtr instance, IntPtr ptrAssetName, IntPtr info);

        // this is the object that will handle the injection on the native method
        private static readonly NativeHook<LoadFromNameDelegate> Hook = new();
        
        private static readonly Dictionary<string, UnityEngine.Object> LoadedAssets = new();

        private static readonly string[] AssetSuffixes = {
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
            var type = typeof(ResourcesManager).GetNestedType("MethodInfoStoreGeneric_LoadFromName_Public_T_String_0`1", BindingFlags.NonPublic)?.MakeGenericType(typeof(TextAsset));
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

            // there is a cached asset, return it
            if (LoadedAssets.TryGetValue(assetName, out var asset))
            {
                if (asset != null)
                {
                    Logger.Msg(Managers.AudioManager.SwitchLoad(assetName)
                        ? $"Resuming async load of {assetName}"
                        : $"Using cache for {assetName}");
                    return asset.Pointer;
                }
                else
                {
                    Logger.Msg("Removing null asset");
                    LoadedAssets.Remove(assetName);
                }
            }

            var cacheAsset = true;
            var language = SingletonScriptableObject<LocalizationSettings>.instance.GetActiveOption("Language");
            UnityEngine.Object newAsset = null;

            // adding album assets
            if (assetName == AlbumManager.JsonName)
            {
                var albumJson = new JsonArray();
                foreach (var loadedAlbum in AlbumManager.LoadedAlbums)
                {
                    var key = loadedAlbum.Key;
                    var album = loadedAlbum.Value;
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

                    albumJson.Add(infoObject);
                }
                newAsset = CreateTextAsset(assetName, JsonSerializer.Serialize(albumJson));
                if (!Singleton<ConfigManager>.instance.m_Dictionary.ContainsKey(assetName)) Singleton<ConfigManager>.instance.Add(assetName, ((TextAsset)newAsset).text);
            }
            else if (assetName == $"albums_{language}")
            {
                var textAsset = new TextAsset(assetPtr);
                var jsonArray = JsonSerializer.Deserialize<JsonArray>(textAsset.text);
                jsonArray.Add(new
                {
                    title = AlbumManager.Languages[language]
                });
                newAsset = CreateTextAsset(assetName, JsonSerializer.Serialize(jsonArray));
                if (!Singleton<ConfigManager>.instance.m_Dictionary.ContainsKey(assetName)) Singleton<ConfigManager>.instance.Add(assetName, ((TextAsset)newAsset).text);
            }
            else if (assetName == $"{AlbumManager.JsonName}_{language}")
            {
                var jsonArray = new JsonArray();
                foreach (var loadedAlbum in AlbumManager.LoadedAlbums)
                {
                    jsonArray.Add(new
                    {
                        name = loadedAlbum.Value.Info.Name,
                        author = loadedAlbum.Value.Info.Author,
                    });
                }
                newAsset = CreateTextAsset(assetName, JsonSerializer.Serialize(jsonArray));
                if (!Singleton<ConfigManager>.instance.m_Dictionary.ContainsKey(assetName)) Singleton<ConfigManager>.instance.Add(assetName, ((TextAsset)newAsset).text);
            }

            if (assetPtr == IntPtr.Zero)
            {
                // Try load custom asset
                if (assetName.StartsWith("fs_") || assetName.StartsWith("pkg_"))
                {
                    var suffix = AssetSuffixes.FirstOrDefault(s => assetName.EndsWith(s));
                    if (!string.IsNullOrEmpty(suffix))
                    {

                        var albumKey = assetName[..^suffix.Length];
                        AlbumManager.LoadedAlbums.TryGetValue(albumKey, out var album);
                        if (suffix.StartsWith("_map"))
                        {
                            newAsset = album?.Sheets[int.Parse(suffix[..^4])].StageInfo;
                            // Do not cache any StageInfo
                            cacheAsset = false;
                        }
                        else
                        {
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
                        }
                    }
                }
            }

            if (newAsset != null)
            {
                // Add to cache
                if (cacheAsset)
                {
                    LoadedAssets.Add(assetName, newAsset);
                    Logger.Msg($"Cached {assetName}");
                }
                else
                {
                    Logger.Msg($"Loaded {assetName}");
                }
                return newAsset.Pointer;
            }
            return assetPtr;
        }

        private static TextAsset CreateTextAsset(string name, string text)
        {
            var newAsset = new TextAsset(text)
            {
                name = name
            };
            return newAsset;
        }
    }
}
