#nullable enable

using System.Collections.Generic;
using Godot;
using AshwoodCounty3DPrototype.Items;
using AshwoodCounty3DPrototype.Objectives;
using AshwoodCounty3DPrototype.Save;

namespace AshwoodCounty3DPrototype.UI;

public partial class GameplayNotificationDisplay : Label
{
	public static readonly StringName GroupName = new("gameplay_notifications");

	[Export] public NodePath InventoryPath { get; set; } = new("../../Player/Inventory");
	[Export] public NodePath ObjectivePath { get; set; } = new("../../AntibioticsObjective");
	[Export] public NodePath SaveManagerPath { get; set; } = new("../../SaveGameManager");
	[Export] public float FadeDuration { get; set; } = 0.2f;
	[Export] public float MessageDuration { get; set; } = 1.8f;
	[Export] public float DuplicateCooldown { get; set; } = 2.0f;

	private enum PresentationState
	{
		Hidden,
		FadingIn,
		Holding,
		FadingOut,
	}

	private readonly Queue<string> _messages = new();
	private readonly Dictionary<string, ulong> _recentMessages = new();
	private PresentationState _state;
	private string _currentMessage = string.Empty;
	private float _remaining;
	private PlayerInventory _inventory = null!;
	private AntibioticsObjective _objective = null!;
	private SaveGameManager _saveManager = null!;

	public int PendingCount => _messages.Count + (string.IsNullOrEmpty(_currentMessage) ? 0 : 1);
	public string CurrentMessage => _currentMessage;

	public override void _Ready()
	{
		AddToGroup(GroupName);
		_inventory = GetNode<PlayerInventory>(InventoryPath);
		_objective = GetNode<AntibioticsObjective>(ObjectivePath);
		_saveManager = GetNode<SaveGameManager>(SaveManagerPath);
		_inventory.ItemUsed += OnItemUsed;
		_objective.StateChanged += OnObjectiveStateChanged;
		_saveManager.StatusMessageRequested += QueueNotification;
		Visible = false;
		Modulate = new Color(Modulate, 0.0f);
	}

	public override void _ExitTree()
	{
		if (IsInstanceValid(_inventory))
		{
			_inventory.ItemUsed -= OnItemUsed;
		}
		if (IsInstanceValid(_objective))
		{
			_objective.StateChanged -= OnObjectiveStateChanged;
		}
		if (IsInstanceValid(_saveManager))
		{
			_saveManager.StatusMessageRequested -= QueueNotification;
		}
	}

	public override void _Process(double delta)
	{
		float deltaTime = (float)delta;
		if (_state == PresentationState.Hidden)
		{
			ShowNext();
			return;
		}

		float fadeDuration = Mathf.Max(FadeDuration, 0.01f);
		switch (_state)
		{
			case PresentationState.FadingIn:
				SetAlpha(Modulate.A + (deltaTime / fadeDuration));
				if (Modulate.A >= 1.0f)
				{
					_state = PresentationState.Holding;
					_remaining = Mathf.Max(MessageDuration, 0.0f);
				}
				break;
			case PresentationState.Holding:
				_remaining = Mathf.Max(_remaining - deltaTime, 0.0f);
				if (_remaining <= 0.0f)
				{
					_state = PresentationState.FadingOut;
				}
				break;
			case PresentationState.FadingOut:
				SetAlpha(Modulate.A - (deltaTime / fadeDuration));
				if (Modulate.A <= 0.0f)
				{
					_currentMessage = string.Empty;
					_state = PresentationState.Hidden;
					ShowNext();
				}
				break;
		}
	}

	public void QueueNotification(string message)
	{
		string trimmedMessage = message.Trim();
		ulong currentTime = Time.GetTicksMsec();
		if (string.IsNullOrEmpty(trimmedMessage) || IsDuplicate(trimmedMessage, currentTime))
		{
			return;
		}

		_recentMessages[trimmedMessage] = currentTime;
		_messages.Enqueue(trimmedMessage);
		if (_state == PresentationState.Hidden)
		{
			ShowNext();
		}
	}

	public bool ContainsMessage(string text)
	{
		if (_currentMessage.Contains(text, System.StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		foreach (string message in _messages)
		{
			if (message.Contains(text, System.StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsDuplicate(string message, ulong currentTime)
	{
		if (_recentMessages.TryGetValue(message, out ulong previousTime) &&
			currentTime - previousTime < (ulong)(Mathf.Max(DuplicateCooldown, 0.0f) * 1000.0f))
		{
			return true;
		}
		if (_currentMessage == message)
		{
			return true;
		}
		foreach (string queuedMessage in _messages)
		{
			if (queuedMessage == message)
			{
				return true;
			}
		}
		return false;
	}

	private void ShowNext()
	{
		if (_messages.Count == 0)
		{
			Visible = false;
			return;
		}

		_currentMessage = _messages.Dequeue();
		Text = _currentMessage;
		Visible = true;
		SetAlpha(0.0f);
		_state = PresentationState.FadingIn;
	}

	private void SetAlpha(float alpha)
	{
		Modulate = new Color(Modulate, Mathf.Clamp(alpha, 0.0f, 1.0f));
	}

	private void OnItemUsed(string message)
	{
		QueueNotification($"Item used: {message}");
	}

	private void OnObjectiveStateChanged(int state)
	{
		AntibioticsObjectiveState objectiveState = (AntibioticsObjectiveState)state;
		QueueNotification(objectiveState == AntibioticsObjectiveState.Completed
			? "Objective completed: Antibiotics delivered"
			: $"Objective updated: {_objective.DisplayText}");
	}
}
