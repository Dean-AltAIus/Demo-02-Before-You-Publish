using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Altaius.Demo02.Gameplay;
using Altaius.Demo02.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace Altaius.Demo02.Authoring
{
    /// <summary>Explicit additive text binding. Does not regenerate UI, sources or layout.</summary>
    public static class EnglishCatalogueAuthoring
    {
        [Serializable] sealed class InputEntries{public NativeEnglishStringTable.Entry[] Entries;}
        public static object ApplyMissing(string entriesPath)
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying||scene.isDirty||scene.path!="Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity")throw new InvalidOperationException("Clean saved Demo02 campus outside Play required.");
            const string path="Assets/Demo02/strings/STR_EN.asset";
            if(!AssetDatabase.IsValidFolder("Assets/Demo02/strings"))AssetDatabase.CreateFolder("Assets/Demo02","strings");
            var table=AssetDatabase.LoadAssetAtPath<NativeEnglishStringTable>(path);
            if(!table){table=ScriptableObject.CreateInstance<NativeEnglishStringTable>();AssetDatabase.CreateAsset(table,path);}
            var entries=table.Entries.ToList();var incoming=JsonUtility.FromJson<InputEntries>(File.ReadAllText(entriesPath));
            foreach(var item in incoming.Entries)if(!entries.Any(e=>e.Key==item.Key))entries.Add(item);
            foreach(var type in new[]{typeof(Altaius.Demo02.Domain.PublicationStatus),typeof(Altaius.Demo02.Domain.ReviewStatus),typeof(Altaius.Demo02.Domain.ActorRole),typeof(Altaius.Demo02.Domain.HoldDefectBasis),typeof(Altaius.Demo02.Integration.ReplyState)})
            foreach(Enum state in Enum.GetValues(type))Add(entries,"state."+type.Name+"."+state,Regex.Replace(state.ToString(),"([a-z])([A-Z])","$1 $2"));
            var roots=scene.GetRootGameObjects();var view=roots.SelectMany(g=>g.GetComponentsInChildren<WorkbenchView>(true)).Single();
            var ui=roots.SelectMany(g=>g.GetComponentsInChildren<PresentationView>(true)).Single();
            var binding=view.GetComponent<EnglishCatalogueBinding>();if(!binding)binding=Undo.AddComponent<EnglishCatalogueBinding>(view.gameObject);
            binding.Catalogue=table;view.English=table;
            var labels=binding.StaticLabels.ToList();
            Action<Text,string> text=(target,key)=>{if(!target||labels.Any(l=>l.Text==target))return;Add(entries,key,target.text);labels.Add(new EnglishCatalogueBinding.Label{Key=key,Text=target});};
            text(view.HudTitle,"saved.hud.title");text(view.LeenaIdentity,"saved.leena.identity");
            foreach(var button in new[]{view.NotebookButton,view.HistoryButton,view.RoutesButton,view.PauseButton})if(button)text(button.GetComponentInChildren<Text>(true),"saved.hud."+Slug(button.name));
            text(view.Close.GetComponentInChildren<Text>(true),"saved.workspace.close");
            text(ui.TalkLauncher.GetComponentInChildren<Text>(true),"saved.talk.launcher");
            foreach(var surface in ui.Surfaces)
            {
                text(surface.Close.GetComponentInChildren<Text>(true),"saved.surface.close");
                if(surface.Send)text(surface.Send.GetComponentInChildren<Text>(true),"saved."+surface.SurfaceId+".send");
                if(surface.CancelRequest&&surface.SurfaceId=="coach")text(surface.CancelRequest.GetComponentInChildren<Text>(true),"saved.coach.clear");
                foreach(var button in surface.TabButtons)text(button.GetComponentInChildren<Text>(true),"saved."+surface.SurfaceId+".tab."+Slug(button.name));
                foreach(var setting in surface.SettingRows??Array.Empty<NativeSettingRow>())
                {text(setting.Label,"saved.setting."+setting.SettingId);foreach(var toggle in setting.Control.GetComponentsInChildren<Toggle>(true))text(toggle.GetComponentInChildren<Text>(true),"saved.setting.toggle");}
            }
            foreach(var station in view.Gameplay.Stations)
            {string key="saved.station."+station.StationId;Add(entries,key,station.DisplayName);if(!labels.Any(l=>l.Station==station))labels.Add(new EnglishCatalogueBinding.Label{Key=key,Station=station});}
            foreach(var world in roots.SelectMany(g=>g.GetComponentsInChildren<TextMesh>(true)).Where(t=>t.name.EndsWith(" sign",StringComparison.Ordinal)))
            {string key="saved.world."+Slug(world.name);Add(entries,key,world.text);if(!labels.Any(l=>l.WorldText==world))labels.Add(new EnglishCatalogueBinding.Label{Key=key,WorldText=world});}
            table.Entries=entries.OrderBy(e=>e.Key,StringComparer.Ordinal).ToArray();table.Invalidate();binding.StaticLabels=labels.ToArray();
            EditorUtility.SetDirty(table);EditorUtility.SetDirty(view);EditorUtility.SetDirty(binding);PrefabUtility.RecordPrefabInstancePropertyModifications(view);PrefabUtility.RecordPrefabInstancePropertyModifications(binding);
            EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
            return new{path,entries=table.Entries.Length,savedLabels=labels.Count,roots=scene.rootCount};
        }
        static void Add(List<NativeEnglishStringTable.Entry> entries,string key,string value)
        {if(!entries.Any(e=>e.Key==key))entries.Add(new NativeEnglishStringTable.Entry{Key=key,English=value});}
        static string Slug(string value)=>Regex.Replace(value.ToLowerInvariant(),"[^a-z0-9]+",".").Trim('.');
    }
}
