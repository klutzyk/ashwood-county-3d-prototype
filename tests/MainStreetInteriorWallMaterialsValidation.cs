#nullable enable

using System;
using System.Collections.Generic;
using Godot;

namespace AshwoodCounty3DPrototype.Tests;

public partial class MainStreetInteriorWallMaterialsValidation : Node
{
	private static readonly IReadOnlyDictionary<string, string> WallMaterials =
		new Dictionary<string, string>
		{
			[
				"res://scenes/world/ashwood/interiors/glens_bakery_interior.tscn"
			] = "res://assets/materials/ashwood_bakery_painted_plaster.tres",
			[
				"res://assets/environment/buildings/Pharmacy/interior.tscn"
			] = "res://assets/materials/greenleaf_pharmacy_wall.tres",
			[
				"res://assets/environment/buildings/WillowOutfitters/interior.tscn"
			] = "res://assets/materials/greenleaf_pharmacy_wall.tres",
			[
				"res://assets/environment/buildings/Diner/interior.tscn"
			] = "res://assets/materials/silver_spoon_wall.tres",
			[
				"res://assets/environment/buildings/AshwoodGrocery/interior.tscn"
			] = "res://assets/materials/ashwood_grocery_wall.tres",
			[
				"res://assets/environment/buildings/MillerHardware/interior.tscn"
			] = "res://assets/materials/miller_hardware_wall.tres",
			[
				"res://assets/environment/buildings/AshwoodPoliceStation/ashwood_police_station.tscn"
			] = "res://assets/materials/ashwood_police_wall_plaster.tres",
		};

	public override void _Ready()
	{
		try
		{
			foreach ((string scenePath, string materialPath) in WallMaterials)
			{
				ValidateSceneUsesMaterial(scenePath, materialPath);
				ValidatePbrMaterial(materialPath);
			}

			GD.Print("MAIN_STREET_INTERIOR_WALL_MATERIALS_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError(
				$"MAIN_STREET_INTERIOR_WALL_MATERIALS_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private static void ValidateSceneUsesMaterial(
		string scenePath,
		string materialPath)
	{
		Require(ResourceLoader.Exists(scenePath),
			$"enterable interior scene exists: {scenePath}");
		string sceneSource = FileAccess.GetFileAsString(scenePath);
		Require(sceneSource.Contains(materialPath, StringComparison.Ordinal),
			$"{scenePath.GetFile()} applies its authored wall material");
	}

	private static void ValidatePbrMaterial(string materialPath)
	{
		StandardMaterial3D material =
			GD.Load<StandardMaterial3D>(materialPath);
		Require(material.AlbedoTexture is not null,
			$"{materialPath.GetFile()} has an albedo texture");
		Require(material.NormalEnabled &&
			material.NormalTexture is not null,
			$"{materialPath.GetFile()} has normal-mapped surface detail");
		Require(material.RoughnessTexture is not null,
			$"{materialPath.GetFile()} has authored roughness");
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
