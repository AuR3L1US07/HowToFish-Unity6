using HeathenEngineering.SteamworksIntegration;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

[RequireComponent(typeof(TMP_InputField))]
public class UIInputField : MonoBehaviour, ISubmitHandler, IEventSystemHandler
{
	private TMP_InputField _inputField;

	private Callback<GamepadTextInputDismissed_t> _textInputDismissed;

	private bool _keyboardOpen;

	private void Awake()
	{
		_inputField = GetComponent<TMP_InputField>();
		// Exported input fields retained a different global font than their visible label.
		// TMP uses this global font when editing, so keep it paired with the label atlas.
		if (LocalPlayMode.Enabled && _inputField.textComponent != null)
			_inputField.fontAsset = _inputField.textComponent.font;
		if (LocalPlayMode.Enabled && gameObject.name == "ServerNameInputField")
		{
			_inputField.contentType = TMP_InputField.ContentType.Standard;
			_inputField.onValidateInput += ValidateSaveNameCharacter;
			_inputField.textComponent.color = Color.white;
		}
	}

	private static char ValidateSaveNameCharacter(string text, int index, char added)
	{
		return char.IsControl(added) || System.Array.IndexOf(System.IO.Path.GetInvalidFileNameChars(), added) >= 0 ? '\0' : added;
	}

	private void OnDestroy()
	{
		_textInputDismissed?.Dispose();
	}

	public void OnSubmit(BaseEventData eventData)
	{
		if (!_keyboardOpen && _inputField.interactable && !_inputField.readOnly && WasSubmittedByController() && SteamSettings.Initialized)
		{
			if (_textInputDismissed == null)
			{
				_textInputDismissed = Callback<GamepadTextInputDismissed_t>.Create(OnTextInputDismissed);
			}
			TMP_InputField.ContentType contentType = _inputField.contentType;
			EGamepadTextInputMode eInputMode = ((contentType == TMP_InputField.ContentType.Password || contentType == TMP_InputField.ContentType.Pin) ? EGamepadTextInputMode.k_EGamepadTextInputModePassword : EGamepadTextInputMode.k_EGamepadTextInputModeNormal);
			EGamepadTextInputLineMode eLineInputMode = ((_inputField.lineType != TMP_InputField.LineType.SingleLine) ? EGamepadTextInputLineMode.k_EGamepadTextInputLineModeMultipleLines : EGamepadTextInputLineMode.k_EGamepadTextInputLineModeSingleLine);
			uint unCharMax = ((_inputField.characterLimit > 0) ? ((uint)_inputField.characterLimit) : 256u);
			_keyboardOpen = SteamUtils.ShowGamepadTextInput(eInputMode, eLineInputMode, LocalizationManager.EnterTextLocalized.GetLocalizedString(), unCharMax, _inputField.text);
		}
	}

	private static bool WasSubmittedByController()
	{
		if (!(EventSystem.current?.currentInputModule is InputSystemUIInputModule inputSystemUIInputModule))
		{
			return false;
		}
		return inputSystemUIInputModule.submit?.action?.activeControl?.device is Gamepad;
	}

	private void OnTextInputDismissed(GamepadTextInputDismissed_t result)
	{
		_keyboardOpen = false;
		if (result.m_bSubmitted)
		{
			uint enteredGamepadTextLength = SteamUtils.GetEnteredGamepadTextLength();
			if (SteamUtils.GetEnteredGamepadTextInput(out var pchText, enteredGamepadTextLength + 1))
			{
				_inputField.text = pchText;
				_inputField.caretPosition = pchText.Length;
				_inputField.onEndEdit.Invoke(pchText);
			}
		}
	}
}
