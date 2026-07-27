#nullable enable

using System;
using System.Collections.Generic;
using Godot;

namespace AshwoodCounty3DPrototype.Tools;

[Tool]
public partial class BuildingInteriorAuthoring : Node3D
{
	private const string PreviewRootPath = "LayoutPreviews";
	private const string ExteriorRootPath = "ExteriorPreview";
	private const string GeneratedCollisionRootPath = "GeneratedCollision";
	private const string GeneratedSceneCollisionRootPath =
		"GeneratedSceneCollision";
	private const string CollisionProxyMetadata =
		"building_interior_collision_proxy";

	private PackedScene? _exteriorScene;
	private BuildingInteriorLayout? _layoutResource;
	private bool _exteriorVisible = true;
	private bool _ghostExterior;
	private float _ghostExteriorOpacity = 0.3f;
	private bool _collisionPreviewVisible = true;
	private readonly Dictionary<ulong, PreviewMaterialState>
		_exteriorMaterialStates = new();

	[Export] public NodePath CollisionTargetRoot { get; set; } =
		new("ExteriorPreview/ExteriorReference");
	[Export] public BuildingSceneCollisionMode DefaultCollisionMode { get; set; } =
		BuildingSceneCollisionMode.Auto;
	[Export] public bool PreserveExistingCollision { get; set; } = true;
	[Export] public bool IncludeHiddenObjects { get; set; }
	[Export]
	public Godot.Collections.Array<BuildingSceneCollisionOverride>
		SceneCollisionOverrides { get; set; } = new();

	[Export]
	public PackedScene? ExteriorScene
	{
		get => _exteriorScene;
		set
		{
			if (_exteriorScene == value)
			{
				return;
			}

			_exteriorScene = value;
			QueueExteriorRefresh();
		}
	}

	[Export]
	public BuildingInteriorLayout? LayoutResource
	{
		get => _layoutResource;
		set => _layoutResource = value;
	}

	[Export]
	public bool ExteriorVisible
	{
		get => _exteriorVisible;
		set
		{
			_exteriorVisible = value;
			ApplyExteriorAppearance();
		}
	}

	[Export]
	public bool GhostExterior
	{
		get => _ghostExterior;
		set
		{
			_ghostExterior = value;
			ApplyExteriorAppearance();
		}
	}

	[Export(PropertyHint.Range, "0,1,0.05")]
	public float GhostExteriorOpacity
	{
		get => _ghostExteriorOpacity;
		set
		{
			_ghostExteriorOpacity = Mathf.Clamp(value, 0.0f, 1.0f);
			ApplyExteriorAppearance();
		}
	}

	[Export]
	public bool CollisionPreviewVisible
	{
		get => _collisionPreviewVisible;
		set
		{
			_collisionPreviewVisible = value;
			ApplyCollisionPreviewVisibility();
		}
	}

	[ExportToolButton("Reload Layout", Icon = "Reload")]
	public Callable ReloadLayoutButton => Callable.From(ReloadLayout);

	[ExportToolButton("Save Layout", Icon = "Save")]
	public Callable SaveLayoutButton => Callable.From(SaveLayout);

	[ExportToolButton("Add Wall", Icon = "Add")]
	public Callable AddWallButton =>
		Callable.From(() => AddElement(BuildingInteriorElementType.Wall));

	[ExportToolButton("Add Floor", Icon = "Add")]
	public Callable AddFloorButton =>
		Callable.From(() => AddElement(BuildingInteriorElementType.Floor));

	[ExportToolButton("Add Ceiling", Icon = "Add")]
	public Callable AddCeilingButton =>
		Callable.From(() => AddElement(BuildingInteriorElementType.Ceiling));

	[ExportToolButton("Add Counter", Icon = "Add")]
	public Callable AddCounterButton =>
		Callable.From(() => AddElement(BuildingInteriorElementType.Counter));

	[ExportToolButton("Add Fixture", Icon = "Add")]
	public Callable AddFixtureButton =>
		Callable.From(() => AddElement(BuildingInteriorElementType.Fixture));

	[ExportToolButton("Generate Collision", Icon = "CollisionShape3D")]
	public Callable GenerateCollisionButton => Callable.From(GenerateCollision);

	[ExportToolButton("Clear Generated Collision", Icon = "Clear")]
	public Callable ClearGeneratedCollisionButton =>
		Callable.From(ClearGeneratedCollision);

	[ExportToolButton("Generate Scene Collision", Icon = "CollisionShape3D")]
	public Callable GenerateSceneCollisionButton =>
		Callable.From(GenerateSceneCollision);

	[ExportToolButton("Clear Scene Collision", Icon = "Clear")]
	public Callable ClearSceneCollisionButton =>
		Callable.From(ClearSceneCollision);

	[ExportToolButton("Toggle Exterior Visibility", Icon = "GuiVisibilityVisible")]
	public Callable ToggleExteriorVisibilityButton =>
		Callable.From(ToggleExteriorVisibility);

	[ExportToolButton("Toggle Collision Preview", Icon = "GuiVisibilityVisible")]
	public Callable ToggleCollisionPreviewButton =>
		Callable.From(ToggleCollisionPreview);

	public override void _Ready()
	{
		if (!Engine.IsEditorHint())
		{
			SetProcess(false);
			return;
		}

		// Layout loading is intentionally manual. Opening the tool never writes to,
		// reloads from, or otherwise mutates the assigned layout resource.
		CallDeferred(MethodName.RefreshExterior);
	}

	private void ReloadLayout()
	{
		if (!EnsureEditorContext() || _layoutResource is null)
		{
			GD.PushWarning("Assign a BuildingInteriorLayout before reloading.");
			return;
		}

		Node3D previewRoot = GetRequiredRoot(PreviewRootPath);
		BuildingInteriorBuilder.ClearGeneratedChildren(previewRoot);

		foreach (BuildingInteriorElement element in _layoutResource.Elements)
		{
			if (element is null)
			{
				continue;
			}

			CreatePreview(element);
		}

		GD.Print($"Reloaded {_layoutResource.Elements.Count} interior layout elements.");
	}

	private void SaveLayout()
	{
		if (!EnsureEditorContext() || _layoutResource is null)
		{
			GD.PushWarning("Assign a BuildingInteriorLayout before saving.");
			return;
		}

		if (string.IsNullOrWhiteSpace(_layoutResource.ResourcePath))
		{
			GD.PushError(
				"LayoutResource must be saved as a .tres resource before Save Layout.");
			return;
		}

		Node3D previewRoot = GetRequiredRoot(PreviewRootPath);
		Godot.Collections.Array<BuildingInteriorElement> savedElements = new();
		foreach (Node child in previewRoot.GetChildren())
		{
			if (child is not BuildingInteriorPreviewElement preview)
			{
				continue;
			}

			savedElements.Add(
				BuildingInteriorBuilder.CreateElementFromPreview(preview));
		}

		_layoutResource.Elements = savedElements;
		_layoutResource.EmitChanged();
		GD.Print(
			$"Saving building interior layout to {_layoutResource.ResourcePath}.");
		Error result = ResourceSaver.Save(
			_layoutResource,
			_layoutResource.ResourcePath);
		if (result != Error.Ok)
		{
			GD.PushError(
				$"Failed to save {_layoutResource.ResourcePath}: {result}");
			return;
		}

		VerifySavedLayout(_layoutResource.ResourcePath, savedElements.Count);
		ReloadLayout();
	}

	private void AddElement(BuildingInteriorElementType type)
	{
		if (!EnsureEditorContext())
		{
			return;
		}

		Node3D previewRoot = GetRequiredRoot(PreviewRootPath);
		int typeCount = 0;
		foreach (Node child in previewRoot.GetChildren())
		{
			if (child is BuildingInteriorPreviewElement preview &&
				preview.ElementType == type)
			{
				typeCount++;
			}
		}

		(Vector3 position, Vector3 size) = type switch
		{
			BuildingInteriorElementType.Wall =>
				(new Vector3(0, 1.25f, 0), new Vector3(3, 2.5f, 0.15f)),
			BuildingInteriorElementType.Floor =>
				(Vector3.Zero, new Vector3(4, 0.1f, 4)),
			BuildingInteriorElementType.Ceiling =>
				(new Vector3(0, 2.8f, 0), new Vector3(4, 0.1f, 4)),
			BuildingInteriorElementType.Counter =>
				(new Vector3(0, 0.5f, 0), new Vector3(1.5f, 1, 0.7f)),
			_ => (new Vector3(0, 0.5f, 0), Vector3.One),
		};
		CreatePreview(new BuildingInteriorElement
		{
			Name = new StringName($"{type} {typeCount + 1}"),
			Type = type,
			Position = position,
			Size = size,
		});
	}

	private void GenerateCollision()
	{
		if (!EnsureEditorContext() || _layoutResource is null)
		{
			GD.PushWarning(
				"Assign a saved BuildingInteriorLayout before generating collision.");
			return;
		}

		string layoutPath = _layoutResource.ResourcePath;
		if (string.IsNullOrWhiteSpace(layoutPath) ||
			!layoutPath.EndsWith(".tres", StringComparison.OrdinalIgnoreCase) ||
			!ResourceLoader.Exists(layoutPath))
		{
			GD.PushError(
				"Generate Collision requires a saved standalone .tres " +
				"BuildingInteriorLayout. Press Save Layout first.");
			return;
		}

		Resource? savedResource = ResourceLoader.Load(
			layoutPath,
			cacheMode: ResourceLoader.CacheMode.Ignore);
		if (savedResource is not BuildingInteriorLayout savedLayout)
		{
			GD.PushError(
				$"Could not reload {layoutPath} as a saved BuildingInteriorLayout.");
			return;
		}

		Node3D collisionRoot = GetRequiredRoot(GeneratedCollisionRootPath);
		BuildingInteriorBuilder.ClearGeneratedChildren(collisionRoot);

		int generatedCount = 0;
		foreach (BuildingInteriorElement element in savedLayout.Elements)
		{
			if (element is null ||
				!element.Enabled ||
				!element.GenerateCollision)
			{
				continue;
			}

			try
			{
				StaticBody3D body =
					BuildingInteriorBuilder.CreateCollision(element);
				CollisionShape3D collisionShape =
					body.GetNode<CollisionShape3D>("CollisionShape3D");
				collisionRoot.AddChild(body);
				SetEditorOwner(body);
				SetEditorOwner(collisionShape);

				MeshInstance3D proxy = BuildingInteriorBuilder.CreateVisual(
					element,
					CreateCollisionProxyMaterial());
				proxy.Name = new StringName($"{element.Name} Collision Preview");
				proxy.CastShadow =
					GeometryInstance3D.ShadowCastingSetting.Off;
				proxy.Visible = _collisionPreviewVisible;
				proxy.SetMeta(CollisionProxyMetadata, true);
				collisionRoot.AddChild(proxy);
				SetEditorOwner(proxy);
				generatedCount++;
			}
			catch (Exception exception)
			{
				GD.PushError(
					$"Failed to generate collision for '{element.Name}': " +
					exception.Message);
				return;
			}
		}

		GD.Print(
			$"Generated {generatedCount} layout-element collision bodies from " +
			$"saved layout {layoutPath}. This action does not inspect arbitrary " +
			"scene meshes.");
	}

	private void ClearGeneratedCollision()
	{
		if (!EnsureEditorContext())
		{
			return;
		}

		Node3D collisionRoot = GetRequiredRoot(GeneratedCollisionRootPath);
		int removedCount = collisionRoot.GetChildCount();
		BuildingInteriorBuilder.ClearGeneratedChildren(collisionRoot);
		GD.Print($"Cleared {removedCount} tool-generated collision bodies.");
	}

	private void CreatePreview(BuildingInteriorElement element)
	{
		BuildingInteriorPreviewElement preview =
			BuildingInteriorBuilder.CreatePreview(element);

		Node3D previewRoot = GetRequiredRoot(PreviewRootPath);
		previewRoot.AddChild(preview);
		SetEditorOwner(preview);
	}

	private static StandardMaterial3D CreatePreviewMaterial(Color color)
	{
		return new StandardMaterial3D
		{
			AlbedoColor = color,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
		};
	}

	private static StandardMaterial3D CreateCollisionProxyMaterial()
	{
		return CreatePreviewMaterial(new Color(0.2f, 1.0f, 0.35f, 0.3f));
	}

	private void GenerateSceneCollision()
	{
		if (!EnsureEditorContext())
		{
			return;
		}

		Node3D? target = GetNodeOrNull<Node3D>(CollisionTargetRoot);
		if (target is null)
		{
			GD.PushError(
				$"Collision Target Root '{CollisionTargetRoot}' was not found.");
			return;
		}

		Node3D generatedRoot =
			GetRequiredRoot(GeneratedSceneCollisionRootPath);
		BuildingInteriorBuilder.ClearGeneratedChildren(generatedRoot);
		int count = 0;
		foreach (Node node in target.FindChildren(
			"*", "MeshInstance3D", true, false))
		{
			if (node is not MeshInstance3D mesh ||
				mesh.Mesh is null ||
				(!IncludeHiddenObjects && !mesh.IsVisibleInTree()))
			{
				continue;
			}

			NodePath relativePath = target.GetPathTo(mesh);
			BuildingSceneCollisionMode selectedMode =
				GetSceneCollisionMode(relativePath);
			if (selectedMode == BuildingSceneCollisionMode.None)
			{
				continue;
			}
			if (PreserveExistingCollision &&
				BuildingSceneCollisionBuilder.HasExistingCollision(
					mesh,
					target))
			{
				continue;
			}

			try
			{
				BuildingSceneCollisionBuildResult result =
					BuildingSceneCollisionBuilder.Build(
						mesh,
						generatedRoot,
						selectedMode);
				generatedRoot.AddChild(result.Body);
				SetEditorOwnerRecursive(result.Body);
				result.Preview.MaterialOverride =
					CreateSceneCollisionPreviewMaterial(result.Mode);
				result.Preview.Visible = _collisionPreviewVisible;
				result.Preview.SetMeta(CollisionProxyMetadata, true);
				generatedRoot.AddChild(result.Preview);
				SetEditorOwner(result.Preview);
				count++;
			}
			catch (Exception exception)
			{
				GD.PushError(
					$"Scene collision failed for '{mesh.Name}' " +
					$"(mode {selectedMode}): {exception.Message}");
			}
		}

		GD.Print(
			$"Generated scene collision for {count} mesh objects beneath " +
			$"{CollisionTargetRoot}.");
	}

	private void ClearSceneCollision()
	{
		if (!EnsureEditorContext())
		{
			return;
		}
		Node3D root = GetRequiredRoot(GeneratedSceneCollisionRootPath);
		int count = root.GetChildCount();
		BuildingInteriorBuilder.ClearGeneratedChildren(root);
		GD.Print($"Cleared {count} tool-generated scene collision nodes.");
	}

	private BuildingSceneCollisionMode GetSceneCollisionMode(NodePath path)
	{
		foreach (BuildingSceneCollisionOverride collisionOverride in
			SceneCollisionOverrides)
		{
			if (collisionOverride is not null &&
				collisionOverride.ObjectPath == path)
			{
				return collisionOverride.Mode;
			}
		}
		return DefaultCollisionMode;
	}

	private static StandardMaterial3D CreateSceneCollisionPreviewMaterial(
		BuildingSceneCollisionMode mode)
	{
		Color color = mode switch
		{
			BuildingSceneCollisionMode.Box =>
				new Color(0.2f, 0.9f, 1.0f, 0.28f),
			BuildingSceneCollisionMode.Convex =>
				new Color(1.0f, 0.65f, 0.12f, 0.28f),
			_ => new Color(0.9f, 0.2f, 1.0f, 0.24f),
		};
		return CreatePreviewMaterial(color);
	}

	private void QueueExteriorRefresh()
	{
		if (Engine.IsEditorHint() && IsInsideTree())
		{
			CallDeferred(MethodName.RefreshExterior);
		}
	}

	private void RefreshExterior()
	{
		if (!EnsureEditorContext())
		{
			return;
		}

		Node3D exteriorRoot = GetRequiredRoot(ExteriorRootPath);
		RestoreExteriorPreviewMaterials();
		BuildingInteriorBuilder.ClearGeneratedChildren(exteriorRoot);
		if (_exteriorScene is null)
		{
			return;
		}

		Node exterior = _exteriorScene.Instantiate();
		exterior.Name = "ExteriorReference";
		DisableNestedInteriorGeneration(exterior);
		exteriorRoot.AddChild(exterior);
		ApplyExteriorAppearance();
	}

	private void ToggleExteriorVisibility()
	{
		ExteriorVisible = !ExteriorVisible;
		NotifyPropertyListChanged();
	}

	private void ToggleCollisionPreview()
	{
		CollisionPreviewVisible = !CollisionPreviewVisible;
		NotifyPropertyListChanged();
	}

	private void ApplyExteriorAppearance()
	{
		if (!Engine.IsEditorHint() || !IsInsideTree())
		{
			return;
		}

		Node3D? exteriorRoot = GetNodeOrNull<Node3D>(ExteriorRootPath);
		if (exteriorRoot is null)
		{
			return;
		}

		exteriorRoot.Visible = _exteriorVisible;
		if (!_ghostExterior)
		{
			RestoreExteriorPreviewMaterials();
			return;
		}

		foreach (Node node in exteriorRoot.FindChildren(
			"*", "MeshInstance3D", true, false))
		{
			if (node is MeshInstance3D meshInstance)
			{
				ApplyGhostMaterials(meshInstance);
			}
		}
	}

	private void ApplyGhostMaterials(MeshInstance3D meshInstance)
	{
		if (meshInstance.Mesh is null)
		{
			return;
		}

		ulong instanceId = meshInstance.GetInstanceId();
		if (!_exteriorMaterialStates.TryGetValue(
			instanceId,
			out PreviewMaterialState? state))
		{
			Material?[] originalOverrides =
				new Material?[meshInstance.Mesh.GetSurfaceCount()];
			for (int surface = 0;
				surface < originalOverrides.Length;
				surface++)
			{
				originalOverrides[surface] =
					meshInstance.GetSurfaceOverrideMaterial(surface);
			}

			state = new PreviewMaterialState(meshInstance, originalOverrides);
			_exteriorMaterialStates.Add(instanceId, state);
		}

		for (int surface = 0;
			surface < state.OriginalOverrides.Length;
			surface++)
		{
			Material? source =
				state.OriginalOverrides[surface] ??
				meshInstance.Mesh.SurfaceGetMaterial(surface);
			BaseMaterial3D ghostMaterial =
				CreateGhostMaterial(source, _ghostExteriorOpacity);
			meshInstance.SetSurfaceOverrideMaterial(surface, ghostMaterial);
		}
	}

	private static BaseMaterial3D CreateGhostMaterial(
		Material? source,
		float opacity)
	{
		BaseMaterial3D material = source is BaseMaterial3D baseMaterial
			? (BaseMaterial3D)baseMaterial.Duplicate(true)
			: new StandardMaterial3D
			{
				AlbedoColor = Colors.White,
				Roughness = 0.8f,
			};
		Color color = material.AlbedoColor;
		color.A = opacity;
		material.AlbedoColor = color;
		material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
		return material;
	}

	private void RestoreExteriorPreviewMaterials()
	{
		foreach (PreviewMaterialState state in _exteriorMaterialStates.Values)
		{
			if (!GodotObject.IsInstanceValid(state.MeshInstance) ||
				state.MeshInstance.Mesh is null)
			{
				continue;
			}

			for (int surface = 0;
				surface < state.OriginalOverrides.Length;
				surface++)
			{
				state.MeshInstance.SetSurfaceOverrideMaterial(
					surface,
					state.OriginalOverrides[surface]);
			}
		}
		_exteriorMaterialStates.Clear();
	}

	private void ApplyCollisionPreviewVisibility()
	{
		if (!Engine.IsEditorHint() || !IsInsideTree())
		{
			return;
		}

		foreach (string rootPath in new[]
			{ GeneratedCollisionRootPath, GeneratedSceneCollisionRootPath })
		{
			Node3D? collisionRoot = GetNodeOrNull<Node3D>(rootPath);
			if (collisionRoot is null)
			{
				continue;
			}
			foreach (Node child in collisionRoot.FindChildren(
				"*", "MeshInstance3D", true, false))
			{
				if (child is MeshInstance3D proxy &&
					proxy.HasMeta(CollisionProxyMetadata))
				{
					proxy.Visible = _collisionPreviewVisible;
				}
			}
		}
	}

	private static void DisableNestedInteriorGeneration(Node exterior)
	{
		if (exterior is BuildingInteriorInstance rootInstance)
		{
			rootInstance.RebuildInEditor = false;
			rootInstance.GenerateVisuals = false;
			rootInstance.GenerateCollision = false;
		}

		foreach (Node node in exterior.FindChildren("*", "", true, false))
		{
			if (node is BuildingInteriorInstance instance)
			{
				instance.RebuildInEditor = false;
				instance.GenerateVisuals = false;
				instance.GenerateCollision = false;
			}
		}
	}

	private void VerifySavedLayout(string resourcePath, int expectedCount)
	{
		Resource? reloadedResource = ResourceLoader.Load(
			resourcePath,
			cacheMode: ResourceLoader.CacheMode.Ignore);
		if (reloadedResource is BuildingInteriorLayout reloadedLayout &&
			reloadedLayout.Elements.Count == expectedCount)
		{
			GD.Print(
				$"Saved and reloaded {expectedCount} layout elements from {resourcePath}.");
			return;
		}

		GD.PushError($"Save/reload check failed for {resourcePath}.");
	}

	private Node3D GetRequiredRoot(string path)
	{
		Node3D? root = GetNodeOrNull<Node3D>(path);
		if (root is not null)
		{
			return root;
		}

		throw new InvalidOperationException(
			$"Building interior authoring scene is missing {path}.");
	}

	private void SetEditorOwner(Node node)
	{
		Node? editedSceneRoot = GetTree().EditedSceneRoot;
		if (editedSceneRoot is not null)
		{
			node.Owner = editedSceneRoot;
		}
	}

	private void SetEditorOwnerRecursive(Node node)
	{
		SetEditorOwner(node);
		foreach (Node child in node.GetChildren())
		{
			SetEditorOwnerRecursive(child);
		}
	}

	private static bool EnsureEditorContext()
	{
		return Engine.IsEditorHint();
	}

	private sealed record PreviewMaterialState(
		MeshInstance3D MeshInstance,
		Material?[] OriginalOverrides);
}
