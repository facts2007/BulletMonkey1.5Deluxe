using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
public static class LootShopChecks
{
    static IEnumerator routine;static double until;static readonly List<string> results=new List<string>();
    public static string Report=>string.Join("\n",results);
    public static void Run(){results.Clear();routine=Checks();until=0;EditorApplication.update+=Step;}
    static void Step(){if(EditorApplication.timeSinceStartup<until)return;try{if(routine.MoveNext()){until=EditorApplication.timeSinceStartup+Convert.ToDouble(routine.Current);return;}}catch(Exception e){results.Add("FAIL "+e);}EditorApplication.update-=Step;System.IO.File.WriteAllText("Temp/CombatChecks/loot-shop-report.txt",Report);}
    static void Check(bool ok,string label){results.Add((ok?"PASS ":"FAIL ")+label);if(!ok)throw new Exception(label);}
    static IEnumerator Checks()
    {
        Application.runInBackground=true;
        var p=UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();var boost=p.GetComponent<SpeedBoostAbility>();var gun=UnityEngine.Object.FindFirstObjectByType<Gun>();var shop=UnityEngine.Object.FindFirstObjectByType<ShopManager>();
        boost.Collect();boost.Collect();Check(boost.storedBananas==2&&!boost.IsActive,"Bananas collect into inventory without activating");
        Time.timeScale=0;Check(!boost.UseStored()&&boost.storedBananas==2,"Paused game cannot spend stored banana");Time.timeScale=1;
        Check(boost.UseStored()&&boost.storedBananas==1&&boost.IsActive&&boost.CurrentMultiplier==2,"Use consumes one banana and doubles speed");
        boost.Tick(3);Check(boost.UseStored()&&boost.storedBananas==0&&boost.RemainingSeconds==boost.duration,"Another use refreshes boost instead of stacking multiplier");
        Check(!boost.UseStored()&&boost.inventoryText.text.Contains("x0"),"Empty inventory does not activate and count updates");
        boost.Tick(100);Check(!boost.IsActive&&boost.CurrentMultiplier==1,"Boost expires normally");
        int hp=p.maxHealth,ammo=gun.maxAmmo;float rate=gun.fireRate;PlayerParts.Instance.currentParts=10000;
        for(int i=0;i<4;i++){shop.UpgradeHealth();shop.UpgradeFireRate();shop.UpgradeMaxAmmo();}
        Check(p.maxHealth==hp+shop.healthIncreaseLevel1+shop.healthIncreaseLevel2+shop.healthIncreaseLevel3+shop.healthIncreaseLevel4,"Health fourth upgrade applies");
        Check(Mathf.Approximately(gun.fireRate,rate+shop.fireRateIncreaseLevel1+shop.fireRateIncreaseLevel2+shop.fireRateIncreaseLevel3+shop.fireRateIncreaseLevel4),"Fire-rate fourth upgrade applies");
        Check(gun.maxAmmo==ammo+shop.maxAmmoIncreaseLevel1+shop.maxAmmoIncreaseLevel2+shop.maxAmmoIncreaseLevel3+shop.maxAmmoIncreaseLevel4,"Ammo fourth upgrade applies");
        int credits=PlayerParts.Instance.currentParts;shop.UpgradeHealth();shop.UpgradeFireRate();shop.UpgradeMaxAmmo();Check(PlayerParts.Instance.currentParts==credits&&shop.healthUpgradeButtonText.text.Contains("4/4"),"All upgrades stop at tier four without extra charges");
        var drops=new List<GameObject>();
        foreach(var path in new[]{"Assets/Assets/SceneStuff/Parts.prefab","Assets/Assets/SceneStuff/Ammo.prefab","Assets/Assets/SceneStuff/SuperBananaPickup.prefab","Assets/Models/Prefabs/Cocey banan Variant.prefab"})drops.Add(LootMotion.Drop(AssetDatabase.LoadAssetAtPath<GameObject>(path),p.transform.position+p.transform.right*4+Vector3.up,Quaternion.identity));
        Check(drops.All(d=>d.GetComponent<LootMotion>().IsFlying&&d.GetComponent<LootMotion>().visual!=null),"All four drop types launch with spinning visuals");
        var parts=drops[0].GetComponent<PartsPickup>();int before=PlayerParts.Instance.currentParts;parts.SendMessage("OnTriggerEnter",p.GetComponent<Collider>());Check(PlayerParts.Instance.currentParts==before,"Flying loot is not collected before its entrance animation");
        yield return 2;
        Check(drops.All(d=>d!=null&&!d.GetComponent<LootMotion>().IsFlying),"Drops land and transition to hover/spin");
        parts.SendMessage("OnTriggerEnter",p.GetComponent<Collider>());parts.SendMessage("OnTriggerEnter",p.GetComponent<Collider>());Check(PlayerParts.Instance.currentParts==before+parts.amount,"Landed scrap credits exactly once");
        drops[3].SendMessage("OnTriggerEnter",p.GetComponent<Collider>());Check(boost.storedBananas==1&&!boost.IsActive,"World Cokey pickup stores a banana instead of activating");
        var audio=GameAudio.Instance;audio.playerCheer=AudioClip.Create("Cheer test",44100,1,44100,false);audio.RegisterPlayerKill();audio.RegisterPlayerKill();Check(audio.cheerPopup.picture.gameObject.activeSelf,"Cheer sound triggers monkey-image popup");
        yield return .3;Check(audio.cheerPopup.picture.localScale.x>0&&Mathf.Abs(audio.cheerPopup.picture.localEulerAngles.z)>.1f,"Popup animates scale and goofy rotation");
        yield return 1.5;Check(!audio.cheerPopup.picture.gameObject.activeSelf,"Popup hides after its animation");
        foreach(var drop in drops)if(drop!=null)UnityEngine.Object.Destroy(drop);
        typeof(ShopManager).GetMethod("OpenShop",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(shop,null);Check(shop.shopPanel.activeSelf&&Time.timeScale==0,"Restyled shop still opens and pauses gameplay");
        results.Add("COMPLETE — shop left open for visual inspection");
    }
}
