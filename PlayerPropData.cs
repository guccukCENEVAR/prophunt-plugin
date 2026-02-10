using CounterStrikeSharp.API.Core;

namespace PropHunt;

public class PlayerPropData
{
    public CDynamicProp? PropEntity { get; set; }
    public string ModelPath { get; set; } = string.Empty;
    public PropSize Size { get; set; } = PropSize.Medium;
    public bool IsFrozen { get; set; } = false;
    public int SwapsLeft { get; set; }
    public int DecoysLeft { get; set; }
    public int WhistlesLeft { get; set; }
    public float LastWhistleTime { get; set; } = 0f;
    public int TauntsLeft { get; set; }
    public float LastTauntTime { get; set; } = 0f;
    public bool IsThirdPerson { get; set; } = false;
    public CDynamicProp? CameraProp { get; set; }
    public List<CDynamicProp> DecoyProps { get; set; } = new();

    // ── Button press tracking (one-press detection) ─────
    // Fields (not properties) so they can be passed by ref
    public bool _btnTauntDown;
    public bool _btnSwapDown;
    public bool _btnFreezeDown;
    public bool _btnWhistleDown;
    public bool _btnDecoyDown;

    public PlayerPropData(int swapLimit, int decoyLimit, int whistleLimit, int tauntLimit)
    {
        SwapsLeft = swapLimit;
        DecoysLeft = decoyLimit;
        WhistlesLeft = whistleLimit;
        TauntsLeft = tauntLimit;
    }
}
