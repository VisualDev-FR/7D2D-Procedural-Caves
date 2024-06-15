public class XUiC_CavesConfig : XUiController
{
    public static string ID = "caveConfigWindowGroup";

    public override void Init()
    {
        base.Init();

        XUiC_CavesConfig configWindow = GetChildByType<XUiC_CavesConfig>();

        XUiC_TextInput seed = configWindow.GetChildById("seed").GetChildById("input") as XUiC_TextInput;
        XUiC_TextInput Threeshold = configWindow.GetChildById("Threshold").GetChildById("input") as XUiC_TextInput;
        XUiC_TextInput LacunarityY = configWindow.GetChildById("LacunarityY").GetChildById("input") as XUiC_TextInput;
        XUiC_TextInput GainY = configWindow.GetChildById("GainY").GetChildById("input") as XUiC_TextInput;
        XUiC_TextInput FrequencyY = configWindow.GetChildById("FrequencyY").GetChildById("input") as XUiC_TextInput;

        CaveConfig.GetFastNoiseY();
        CaveConfig.GetFastNoiseZX();

        seed.Text = CaveConfig.seed.ToString();
        Threeshold.Text = CaveConfig.NoiseThreeshold.ToString();
        LacunarityY.Text = CaveConfig.noiseY.lacunarity.ToString();
        GainY.Text = CaveConfig.noiseY.gain.ToString();
        FrequencyY.Text = CaveConfig.noiseY.frequency.ToString();
    }

    public static void Open()
    {
        GameManager.Instance.SetConsoleWindowVisible(_b: false);
        LocalPlayerUI.GetUIForPrimaryPlayer().windowManager.Open(ID, _bModal: true);
    }
}
