using UnityEngine;

public abstract class Purchasable : Interactable
{
    [SerializeField]
    protected bool _isFree;

    [SerializeField]
    protected int _customCost;

    protected string _hoverString;

    protected bool _customCanBuy = true;

    protected Currency _customCurrency;

    private string _finalHoverString;

    protected override void OnDestroy()
    {
        base.OnDestroy();
        PlayerUI.HideLookAtText(_finalHoverString);
        PlayerUI.HideInspectInfo(this);
    }

    public override void Hover()
    {
        string hoverString = _hoverString;
        if ((bool)Player.LocalPlayer.Holding.HeldItem && !InteractableWhenHoldingItem)
        {
            hoverString = hoverString + "\n(" + LocalizationManager.UnequipItemLocalized.GetLocalizedString() + ")";
        }

        if (!_isHovering)
        {
            OnStartHover();
            PlayerUI.SetLookAtText(hoverString, base.TextTarget);
        }
        else if (hoverString != _finalHoverString)
        {
            PlayerUI.UpdateLookAtText(hoverString);
        }

        _finalHoverString = hoverString;
        PlayerUI.SetLookAtColor((MoneyManager.CanAfford(_customCost, _customCurrency) && _customCanBuy) ? Color.white : GameInfo.RedColor);
        base.Hover();
    }

    protected virtual void OnStartHover()
    {
    }

    public override void UnHover()
    {
        PlayerUI.HideLookAtText(_finalHoverString);
        PlayerUI.HideInspectInfo(this);
        base.UnHover();
    }

    protected void CantBuyEffects()
    {
        MonoBehaviour.print("Cant afford idiot");
        AudioManager.PlayGlobalClip("Error", variation: true, 0.3f);
    }
}
