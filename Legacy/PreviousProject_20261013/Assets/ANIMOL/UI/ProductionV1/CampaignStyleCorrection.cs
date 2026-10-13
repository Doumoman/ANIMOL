using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.ProductionV1
{
    /// <summary>SC02 presentation only; never replaces artwork, layout, fonts or data bindings.</summary>
    public static class CampaignStyleCorrection
    {
        [Serializable] private class Document {public int replacedAssetCount;public Colors uiCorrections;}
        [Serializable] private class Colors {public string cardBacking,headerBacking,textPrimary,textSecondary;}
        private static Colors colors;
        private static Colors Configuration
        {
            get
            {
                if(colors!=null)return colors;
                var asset=Resources.Load<TextAsset>("ANIMOLProductionV1/campaign-style-corrections");
                if(asset==null)throw new InvalidOperationException("Missing SC02 presentation correction configuration.");
                var document=JsonUtility.FromJson<Document>(asset.text);
                if(document.replacedAssetCount!=0 || document.uiCorrections==null)throw new InvalidOperationException("SC02 correction must retain existing sprites.");
                return colors=document.uiCorrections;
            }
        }
        private static Color Parse(string value)
        {
            if(!ColorUtility.TryParseHtmlString(value,out var color))throw new InvalidOperationException("Invalid SC02 color: "+value);
            return color;
        }
        public static void Apply(Transform screen)
        {
            if(screen==null)return;
            var config=Configuration;
            var card=Parse(config.cardBacking);var header=Parse(config.headerBacking);
            foreach(var image in screen.GetComponentsInChildren<Image>(true))
            {
                if(image.name=="ThemePaper")image.color=card;
                // Current header/back PNGs already contain #f4f4f4. White modulation
                // preserves that backing and their colored borders without adding an Image.
                else if(image.name=="UI_Common_Panel_Main" || image.name=="PV1_SC02_ThemeSelectBack")
                    image.color=new Color(header.r/(244f/255),header.g/(244f/255),header.b/(244f/255),header.a);
            }
            foreach(var text in screen.GetComponentsInChildren<TextMeshProUGUI>(true))
                if(text.name=="Title" || text.name.StartsWith("ThemeData_",StringComparison.Ordinal) || text.name=="Label")text.color=Parse(config.textPrimary);
                else if(text.name=="Subtitle")text.color=Parse(config.textSecondary);
        }
    }
}
