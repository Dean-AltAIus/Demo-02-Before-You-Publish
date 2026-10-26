using System;
using Altaius.Demo02.Domain;
using Altaius.Demo02.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Altaius.Demo02.UI
{
    [DefaultExecutionOrder(-1000),DisallowMultipleComponent]
    public sealed class EnglishCatalogueBinding:MonoBehaviour
    {
        [Serializable] public sealed class Label
        {public string Key;public Text Text;public TextMesh WorldText;public CampusStation Station;}
        public NativeEnglishStringTable Catalogue;
        public Label[] StaticLabels=Array.Empty<Label>();
        Func<string,string> installed;
        void Awake(){Install();RefreshOwnedLabels();}
        public void Install()
        {if(!Catalogue)throw new InvalidOperationException("The saved English catalogue reference is required.");installed=Catalogue.Get;EnglishCopy.Resolver=installed;}
        void OnDestroy(){if(EnglishCopy.Resolver==installed)EnglishCopy.Resolver=null;}
        [ContextMenu("Refresh bound English labels")]
        public void RefreshOwnedLabels()
        {
            if(!Catalogue)return;Catalogue.Invalidate();
            foreach(var label in StaticLabels)
            {var text=Catalogue.Get(label.Key);if(label.Text){label.Text.supportRichText=false;label.Text.text=text;}if(label.WorldText){label.WorldText.richText=false;label.WorldText.text=text;}if(label.Station)label.Station.DisplayName=text;}
        }
    }
}
