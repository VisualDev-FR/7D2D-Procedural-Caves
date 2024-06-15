public class XUiC_CavesConfig : XUiController
{
    public static string ID = "caveConfigWindowGroup";

    public static void Open()
    {
        GameManager.Instance.SetConsoleWindowVisible(_b: false);
        LocalPlayerUI.GetUIForPrimaryPlayer().windowManager.Open(ID, _bModal: true);
    }
}
