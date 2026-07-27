#nullable enable

using Godot;

namespace AshwoodCounty3DPrototype.Tools;

[GlobalClass, Tool]
public partial class BuildingInteriorInstance : Node3D
{
	public const string VisualRootName = "GeneratedInteriorVisuals";
	public const string CollisionRootName = "GeneratedInteriorCollision";

	private BuildingInteriorLayout? _layoutResource;
	private bool _rebuildInEditor = true;
	private bool _generateVisuals = true;
	private bool _generateCollision = true;
	private bool _rebuildQueued;
	private readonly Callable _layoutChangedCallable;

	public BuildingInteriorInstance()
	{
		_layoutChangedCallable = Callable.From(QueueEditorRebuild);
	}

	[Export]
	public BuildingInteriorLayout? LayoutResource
	{
		get => _layoutResource;
		set
		{
			if (_layoutResource == value)
			{
				return;
			}

			DisconnectLayoutChanged();
			_layoutResource = value;
			ConnectLayoutChanged();
			QueueEditorRebuild();
		}
	}

	[Export]
	public bool RebuildInEditor
	{
		get => _rebuildInEditor;
		set
		{
			_rebuildInEditor = value;
			QueueEditorRebuild();
		}
	}

	[Export]
	public bool GenerateVisuals
	{
		get => _generateVisuals;
		set
		{
			_generateVisuals = value;
			QueueEditorRebuild();
		}
	}

	[Export]
	public bool GenerateCollision
	{
		get => _generateCollision;
		set
		{
			_generateCollision = value;
			QueueEditorRebuild();
		}
	}

	[ExportToolButton("Rebuild Now", Icon = "Reload")]
	public Callable RebuildNowButton => Callable.From(RebuildNow);

	[ExportToolButton("Clear Generated", Icon = "Clear")]
	public Callable ClearGeneratedButton => Callable.From(ClearGenerated);

	public override void _Ready()
	{
		ConnectLayoutChanged();
		if (Engine.IsEditorHint())
		{
			if (_rebuildInEditor)
			{
				CallDeferred(MethodName.RebuildNow);
			}
			return;
		}

		RebuildNow();
	}

	public override void _ExitTree()
	{
		DisconnectLayoutChanged();
	}

	public void RebuildNow()
	{
		_rebuildQueued = false;
		Node3D visualRoot = GetOrCreateRoot(VisualRootName);
		Node3D collisionRoot = GetOrCreateRoot(CollisionRootName);
		BuildingInteriorBuilder.ClearGeneratedChildren(visualRoot);
		BuildingInteriorBuilder.ClearGeneratedChildren(collisionRoot);

		if (_layoutResource is null)
		{
			GD.PushWarning(
				$"{Name} cannot rebuild without a BuildingInteriorLayout.");
			return;
		}

		string layoutPath = _layoutResource.ResourcePath;
		GD.Print(
			$"Building interior runtime generation begins from " +
			$"{(string.IsNullOrWhiteSpace(layoutPath) ? "<embedded resource>" : layoutPath)}.");

		foreach (BuildingInteriorElement element in _layoutResource.Elements)
		{
			if (element is null || !element.Enabled)
			{
				continue;
			}

			try
			{
				if (_generateVisuals)
				{
					visualRoot.AddChild(
						BuildingInteriorBuilder.CreateVisual(element));
				}

				if (_generateCollision)
				{
					collisionRoot.AddChild(
						BuildingInteriorBuilder.CreateCollision(element));
				}
			}
			catch (System.Exception exception)
			{
				GD.PushError(
					$"Failed to build interior element '{element.Name}' " +
					$"from {layoutPath}: {exception.Message}");
			}
		}
	}

	public void ClearGenerated()
	{
		_rebuildQueued = false;
		BuildingInteriorBuilder.ClearGeneratedChildren(
			GetOrCreateRoot(VisualRootName));
		BuildingInteriorBuilder.ClearGeneratedChildren(
			GetOrCreateRoot(CollisionRootName));
	}

	private void QueueEditorRebuild()
	{
		if (!Engine.IsEditorHint() ||
			!_rebuildInEditor ||
			!IsInsideTree() ||
			_rebuildQueued)
		{
			return;
		}

		_rebuildQueued = true;
		CallDeferred(MethodName.RebuildNow);
	}

	private void ConnectLayoutChanged()
	{
		if (_layoutResource is not null &&
			!_layoutResource.IsConnected(
				Resource.SignalName.Changed,
				_layoutChangedCallable))
		{
			_layoutResource.Connect(
				Resource.SignalName.Changed,
				_layoutChangedCallable);
		}
	}

	private void DisconnectLayoutChanged()
	{
		if (_layoutResource is not null &&
			_layoutResource.IsConnected(
				Resource.SignalName.Changed,
				_layoutChangedCallable))
		{
			_layoutResource.Disconnect(
				Resource.SignalName.Changed,
				_layoutChangedCallable);
		}
	}

	private Node3D GetOrCreateRoot(string rootName)
	{
		Node3D? root = GetNodeOrNull<Node3D>(rootName);
		if (root is not null)
		{
			root.Transform = Transform3D.Identity;
			return root;
		}

		root = new Node3D
		{
			Name = rootName,
			Transform = Transform3D.Identity,
		};
		AddChild(root);
		return root;
	}
}
