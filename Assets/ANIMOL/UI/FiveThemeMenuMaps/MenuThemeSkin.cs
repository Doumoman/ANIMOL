using ANIMOL.PortraitArtV1;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.FiveThemeMenu
{
    /// <summary>Restyle existing runtime controls; never replace their Button/events/state.</summary>
    public static class MenuThemeSkin
    {
        public static void Apply(PortraitEntryController entry,MenuThemeCatalog catalog)
        {
            // Preserve Button, hit size and handler; keep the y=1488 walkway
            // unobstructed even with a 96px bottom safe-area inset.
            var play=(RectTransform)entry.StartScreen.Play.transform;
            play.anchoredPosition=new Vector2(play.anchoredPosition.x,72);
            var mode=entry.ModeScreen;if(mode==null) return;
            Swap(mode.Back,catalog.back);Swap(mode.Ad,catalog.ad);Swap(mode.Shop,catalog.shop);Swap(mode.Settings,catalog.settings);
            var coin=mode.transform.Find("CurrencyCapsule/CoinIcon").GetComponent<Image>();coin.sprite=catalog.coin;coin.rectTransform.sizeDelta=new Vector2(48,48);
            Card(mode.Campaign,catalog.campaign,24);Card(mode.Competition,catalog.competition,376);Card(mode.Cooperation,catalog.cooperation,728);
            var heading=mode.transform.Find("SectionHeading");if(heading!=null) heading.gameObject.SetActive(false);
            // Keep operational status readable over all five palettes without dimming art/UI.
            foreach(var label in new[]{mode.CurrencyState,mode.CompetitionState,mode.CooperationState,mode.Ad.transform.Find("Unavailable").GetComponent<TMP_Text>()}) {
                var existing=label.transform.parent.Find(label.name+"Readability");
                var plate=existing!=null ? existing.GetComponent<Image>() : new GameObject(label.name+"Readability",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).GetComponent<Image>();
                plate.transform.SetParent(label.transform.parent,false);plate.transform.SetSiblingIndex(label.transform.GetSiblingIndex());
                var p=plate.rectTransform;var l=label.rectTransform;
                p.anchorMin=l.anchorMin;p.anchorMax=l.anchorMax;p.pivot=l.pivot;p.anchoredPosition=l.anchoredPosition;p.sizeDelta=l.sizeDelta;
                plate.color=new Color32(30,20,40,210);plate.raycastTarget=false;
            }
        }
        private static void Swap(Button button,Sprite sprite) => ((Image)button.targetGraphic).sprite=sprite;
        private static void Card(Button button,Sprite sprite,int x)
        {
            Swap(button,sprite);var r=(RectTransform)button.transform;
            r.anchorMin=r.anchorMax=r.pivot=Vector2.zero;r.anchoredPosition=new Vector2(x,72);r.sizeDelta=new Vector2(328,176);
            var title=button.transform.Find("Label").GetComponent<TMP_Text>();title.fontSize=32;title.alignment=TextAlignmentOptions.Midline;
            var t=title.rectTransform;t.anchorMin=t.anchorMax=new Vector2(0,1);t.pivot=new Vector2(0,1);t.anchoredPosition=new Vector2(132,-40);t.sizeDelta=new Vector2(166,88);
            var state=button.transform.Find("Availability").GetComponent<TMP_Text>();
            var s=state.rectTransform;s.anchorMin=s.anchorMax=s.pivot=new Vector2(.5f,0);s.anchoredPosition=new Vector2(0,-24);s.sizeDelta=new Vector2(328,40);
            state.fontSize=32;state.alignment=TextAlignmentOptions.Midline;state.color=new Color32(242,225,195,255);
            // Preserve the live unavailable-service label. Only redundant campaign copy is hidden.
            if(button.name=="CampaignCard") state.gameObject.SetActive(false);
        }
    }
}
