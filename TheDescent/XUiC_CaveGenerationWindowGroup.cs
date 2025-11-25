public class XUiC_CaveGenerationWindowGroup : XUiController
{
    public string LastWindowID { get; set; }

    public override void OnClose()
    {
        base.xui.playerUI.windowManager.Close(windowGroup.ID);
        base.xui.playerUI.windowManager.Open(LastWindowID, _bModal: true);
    }
}