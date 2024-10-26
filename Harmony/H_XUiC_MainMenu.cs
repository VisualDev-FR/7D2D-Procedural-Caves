using HarmonyLib;

[HarmonyPatch(typeof(XUiC_MainMenu), "OnOpen")]
public static class XUiC_MainMenu_OnOpen
{
    public static void Postfix(XUiC_MainMenu __instance)
    {
        var videoData = VideoManager.GetVideoData("caveMenuBackground01");

        XUiC_VideoPlayer.GetInstance(__instance.xui).PlayVideo(videoData, false);
    }
}