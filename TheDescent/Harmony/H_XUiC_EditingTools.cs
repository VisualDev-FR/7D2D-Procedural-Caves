using HarmonyLib;


[HarmonyPatch(typeof(XUiC_EditingTools), "Init")]
public class H_XUiC_EditingTools_Init
{
    private static XUiC_EditingTools controller;

    public static void Postfix(XUiC_EditingTools __instance)
    {
        controller = __instance;

        var btnCaveEditor = controller.GetChildById("btnCaveEditor") as XUiC_SimpleButton;

        btnCaveEditor.OnPressed += BtnCaveEditor_OnPressed;
    }

    public static void BtnCaveEditor_OnPressed(XUiController _sender, int _mouseButton)
    {
        _sender.xui.FindWindowGroupByName("caveEditor").GetChildByType<XUiC_CaveGenerationWindowGroup>().LastWindowID = XUiC_EditingTools.ID;
        _sender.xui.playerUI.windowManager.Open("caveEditor", _bModal: true);
    }
}