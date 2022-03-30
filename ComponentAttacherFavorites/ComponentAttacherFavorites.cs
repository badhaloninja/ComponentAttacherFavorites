using BaseX;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;
using NeosModLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace ComponentAttacherFavorites
{
    public class ComponentAttacherFavorites : NeosMod
    {
        public override string Name => "ComponentAttacherFavorites";
        public override string Author => "badhaloninja";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/badhaloninja/ComponentAttacherFavorites";

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<Dictionary<Type, string>> FavoriteComponents = new ModConfigurationKey<Dictionary<Type, string>>("favoriteComponents", "Favorited Components", () => new Dictionary<Type, string>() { });

        private static Dictionary<Type, string> Favorites;

        internal static ModConfiguration config;
        public override void OnEngineInit()
        {
            config = GetConfiguration();
            Favorites = config.GetValue(FavoriteComponents);

            Harmony harmony = new Harmony("me.badhaloninja.ComponentAttacherFavorites");
            harmony.PatchAll();
        }


        // This is pretty sloppily thrown together
        [HarmonyPatch(typeof(ComponentAttacher), "BuildUI")]
        class ComponentAttacher_BuildUI_Patch
        {
            public static bool Prefix(ComponentAttacher __instance, string path, bool genericType, SyncRef<Slot> ____uiRoot, ref SyncRef<TextField> ____customGenericType, ref SyncType ____genericType)
            {
                if (genericType) return true;
                WorkerInitializer.ComponentLibrary.GetSubcategory("/Favorites"); // Generate favorites category

                Favorites = config.GetValue(FavoriteComponents);

                var onOpenCategoryPressed = (ButtonEventHandler<string>)AccessTools.Method(__instance.GetType(), "OnOpenCategoryPressed").CreateDelegate(typeof(ButtonEventHandler<string>), __instance);
                var onCancelPressed = (ButtonEventHandler)AccessTools.Method(__instance.GetType(), "OnCancelPressed").CreateDelegate(typeof(ButtonEventHandler), __instance);


                ____uiRoot.Target.DestroyChildren();
                ____customGenericType.Target = null;
                ____genericType.Value = null;
                UIBuilder uibuilder = new UIBuilder(____uiRoot.Target);
                uibuilder.Style.MinHeight = 32f;
                LocaleString localeString;
                color color;


                CategoryNode<Type> categoryNode;
                if (string.IsNullOrEmpty(path) || path == "/")
                {
                    categoryNode = WorkerInitializer.ComponentLibrary;
                }
                else
                {
                    categoryNode = WorkerInitializer.ComponentLibrary.GetSubcategory(path);
                    if (categoryNode == null)
                    {
                        categoryNode = WorkerInitializer.ComponentLibrary;
                        path = "";
                    }
                }
                if (categoryNode != WorkerInitializer.ComponentLibrary)
                {
                    localeString = "< (back)";
                    color = new color(0.8f, 0.8f, 0.8f, 1f);
                    uibuilder.Button(localeString, color, onOpenCategoryPressed, categoryNode.Parent.GetPath(), 0.35f);
                }

                foreach (CategoryNode<Type> categoryNode2 in categoryNode.Subcategories)
                {
                    localeString = categoryNode2.Name + " >";
                    color = new color(1f, 1f, 0.8f, 1f);
                    var btn = uibuilder.Button(localeString, color, onOpenCategoryPressed, path + "/" + categoryNode2.Name, 0.35f);
                    btn.Label.ParseRichText.Value = false;

                    if (path + "/" + categoryNode2.Name == "/Favorites")
                    {
                        btn.Slot.OrderOffset = -1;
                    }
                }



                if (path != "/Favorites")
                {
                    foreach (Type type in categoryNode.Elements)
                    {
                        componentButton(__instance, type, uibuilder, path);
                    }
                }
                else
                {
                    foreach (var fav in Favorites)
                    {
                        componentButton(__instance, fav.Key, uibuilder, fav.Value);
                    }
                }

                localeString = "Cancel";
                color = new color(1f, 0.8f, 0.8f, 1f);
                uibuilder.Button(localeString, color, onCancelPressed, 0.35f);


                return false;
            }

            static void componentButton(ComponentAttacher instance, Type type, UIBuilder uibuilder, string path)
            {
                var openGenericTypesPressed = (ButtonEventHandler<string>)AccessTools.Method(instance.GetType(), "OpenGenericTypesPressed").CreateDelegate(typeof(ButtonEventHandler<string>), instance);
                var onAddComponentPressed = (ButtonEventHandler<string>)AccessTools.Method(instance.GetType(), "OnAddComponentPressed").CreateDelegate(typeof(ButtonEventHandler<string>), instance);

                LocaleString localeString = type.GetNiceName("<", ">");
                color color;

                var horizontal = uibuilder.HorizontalLayout(4);


                uibuilder.Style.MinWidth = 32;
                uibuilder.Style.FlexibleWidth = 100;
                uibuilder.Style.MinHeight = -1;
                if (type.IsGenericTypeDefinition)
                {
                    color = new color(0.8f, 1f, 0.8f, 1f);
                    uibuilder.Button(localeString, color, openGenericTypesPressed, Path.Combine(path, type.FullName), 0.35f).Label.ParseRichText.Value = false;
                }
                else
                {
                    color = new color(0.8f, 0.8f, 1f, 1f);
                    uibuilder.Button(localeString, color, onAddComponentPressed, type.FullName, 0.35f).Label.ParseRichText.Value = false;
                }



                uibuilder.Style.FlexibleWidth = -1;
                uibuilder.Style.MinWidth = 32f;
                uibuilder.Style.PreferredWidth = 32f;
                color = Favorites.ContainsKey(type) ? new color(1f, 1f, 0.8f) : color.White.MulRGB(0.8f);

                var favorite = uibuilder.Button(NeosAssets.Common.Icons.Star, color);

                favorite.LocalPressed += (btn, btnEvent) =>
                {
                    if (Favorites.ContainsKey(type))
                    {
                        Favorites.Remove(type);
                        favorite.SetColors(color.White.MulRGB(0.8f));
                    }
                    else
                    {
                        Favorites.Add(type, path);
                        favorite.SetColors(new color(1f, 1f, 0.8f));
                    }
                    // Save favorite
                    config.Set(FavoriteComponents, Favorites);
                    config.Save();
                };


                uibuilder.NestOut();

                uibuilder.Style.FlexibleWidth = -1;
                uibuilder.Style.MinWidth = -1;
                uibuilder.Style.PreferredWidth = -1;
                uibuilder.Style.MinHeight = 32f;
            }
        }
    }
}