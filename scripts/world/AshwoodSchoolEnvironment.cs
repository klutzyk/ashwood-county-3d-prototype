#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace AshwoodCounty3DPrototype.World;

/// <summary>
/// Builds the deterministic, artist-authored Ashwood County High School shell
/// and prop layout. Architectural boxes are always covered by project PBR
/// materials; recognizable furniture and set dressing comes from credited
/// external models.
/// </summary>
public partial class AshwoodSchoolEnvironment : Node3D
{
	private const float GroundFloorTop = 0.12f;
	private const float UpperFloorTop = 3.48f;
	private const float StoreyHeight = 3.36f;
	private const float WallHeight = 3.22f;

	private const string BrickMaterialPath =
		"res://assets/materials/ashwood_police_exterior_brick.tres";
	private const string WallMaterialPath =
		"res://assets/materials/ashwood_police_wall_plaster.tres";
	private const string FloorMaterialPath =
		"res://assets/materials/ashwood_school_floor_linoleum.tres";
	private const string ConcreteMaterialPath =
		"res://assets/materials/ashwood_police_basement_concrete.tres";
	private const string CeilingMaterialPath =
		"res://assets/materials/ashwood_school_ceiling.tres";
	private const string TileMaterialPath =
		"res://assets/materials/ashwood_police_bathroom_tile.tres";
	private const string GymFloorMaterialPath =
		"res://assets/materials/ashwood_school_gym_floor.tres";
	private const string GreenMetalMaterialPath =
		"res://assets/materials/ashwood_grocery_green_metal.tres";
	private const string DarkWoodMaterialPath =
		"res://assets/materials/ashwood_police_dark_wood.tres";
	private const string GlassMaterialPath =
		"res://assets/materials/ashwood_police_glass.tres";

	private const string UserPropRoot =
		"res://assets/environment/props/user_supplied/";
	private const string PharmacyPropRoot =
		"res://assets/third_party/interiors/pharmacy/poly_haven/";
	private const string BakeryPropRoot =
		"res://assets/third_party/interiors/bakery/poly_haven/";
	private const float InteriorPropVisibilityRange = 18.0f;
	private const float ExteriorPropVisibilityRange = 90.0f;
	private const float PropVisibilityFadeMargin = 3.0f;

	private readonly Dictionary<string, PackedScene> _sceneCache =
		new(StringComparer.Ordinal);

	private Material _brick = null!;
	private Material _wall = null!;
	private Material _floor = null!;
	private Material _concrete = null!;
	private Material _ceiling = null!;
	private Material _tile = null!;
	private Material _gymFloor = null!;
	private Material _greenMetal = null!;
	private Material _darkWood = null!;
	private Material _glass = null!;

	public override void _Ready()
	{
		if (GetNodeOrNull<Node3D>("AuthoredSchool") is not null)
		{
			return;
		}

		LoadMaterials();
		BuildSchool();
	}

	private void LoadMaterials()
	{
		_brick = LoadRequired<Material>(BrickMaterialPath);
		_wall = LoadRequired<Material>(WallMaterialPath);
		_floor = LoadRequired<Material>(FloorMaterialPath);
		_concrete = LoadRequired<Material>(ConcreteMaterialPath);
		_ceiling = LoadRequired<Material>(CeilingMaterialPath);
		_tile = LoadRequired<Material>(TileMaterialPath);
		_gymFloor = LoadRequired<Material>(GymFloorMaterialPath);
		_greenMetal = LoadRequired<Material>(GreenMetalMaterialPath);
		_darkWood = LoadRequired<Material>(DarkWoodMaterialPath);
		_glass = LoadRequired<Material>(GlassMaterialPath);
	}

	private void BuildSchool()
	{
		Node3D authored = AddNode<Node3D>(this, "AuthoredSchool");
		Node3D architecture = AddNode<Node3D>(authored, "Architecture");
		Node3D exterior = AddNode<Node3D>(authored, "ExteriorIdentity");
		Node3D interior = AddNode<Node3D>(authored, "InteriorDressing");
		Node3D lights = AddNode<Node3D>(authored, "Lighting");

		BuildFloorPlates(architecture);
		BuildExteriorWalls(architecture);
		BuildGroundFloorRooms(architecture);
		BuildUpperFloorRooms(architecture);
		BuildStairwell(architecture);
		BuildExteriorIdentity(exterior);
		BuildInteriorDressing(interior);
		BuildLighting(lights);
		BuildGymAnnex(architecture, interior, lights);

		authored.SetMeta("building", "Ashwood County High School");
		authored.SetMeta("storeys", 2);
		authored.SetMeta("layout", "hand_authored");
		authored.SetMeta(
			"stair_collision",
			"continuous ramp for forward-only traversal");
	}

	private void BuildFloorPlates(Node3D parent)
	{
		Node3D floors = AddNode<Node3D>(parent, "Floors");

		AddBox(
			floors,
			"GroundFloor",
			new Vector3(0.0f, 0.02f, 0.0f),
			new Vector3(27.6f, 0.2f, 25.6f),
			_floor);

		// The upper floor is split around the stair void, so the imported stair
		// is physically usable rather than hidden under a full collision slab.
		AddBox(
			floors,
			"UpperFloorWest",
			new Vector3(-5.6f, 3.38f, 0.0f),
			new Vector3(16.4f, 0.2f, 25.6f),
			_floor);
		AddBox(
			floors,
			"UpperFloorEast",
			new Vector3(10.8f, 3.38f, 0.0f),
			new Vector3(6.0f, 0.2f, 25.6f),
			_floor);
		AddBox(
			floors,
			"UpperFloorStairRear",
			new Vector3(5.2f, 3.38f, -6.2f),
			new Vector3(5.2f, 0.2f, 13.2f),
			_floor);
		AddBox(
			floors,
			"UpperFloorStairFront",
			new Vector3(5.2f, 3.38f, 8.2f),
			new Vector3(5.2f, 0.2f, 9.2f),
			_floor);

		AddBox(
			floors,
			"GroundCeiling",
			new Vector3(0.0f, 3.25f, 0.0f),
			new Vector3(27.3f, 0.08f, 25.3f),
			_ceiling,
			collision: false);
		AddBox(
			floors,
			"UpperCeiling",
			new Vector3(0.0f, 6.71f, 0.0f),
			new Vector3(27.3f, 0.1f, 25.3f),
			_ceiling,
			collision: false);
		AddBox(
			floors,
			"FlatRoof",
			new Vector3(0.0f, 6.86f, 0.0f),
			new Vector3(28.2f, 0.22f, 26.2f),
			_concrete);

		AddBox(
			floors,
			"GymWoodInset",
			new Vector3(7.95f, 0.131f, -8.0f),
			new Vector3(11.2f, 0.022f, 8.2f),
			_gymFloor,
			collision: false);
		AddBox(
			floors,
			"RestroomTileNorth",
			new Vector3(10.55f, 0.133f, 10.6f),
			new Vector3(5.7f, 0.026f, 3.7f),
			_tile,
			collision: false);
		AddBox(
			floors,
			"RestroomTileSouth",
			new Vector3(10.55f, 0.133f, 6.5f),
			new Vector3(5.7f, 0.026f, 3.7f),
			_tile,
			collision: false);
	}

	private void BuildExteriorWalls(Node3D parent)
	{
		Node3D shell = AddNode<Node3D>(parent, "ExteriorShell");

		for (int storey = 0; storey < 2; storey++)
		{
			float baseY = storey == 0 ? GroundFloorTop : UpperFloorTop;
			string prefix = storey == 0 ? "Ground" : "Upper";

			Opening[] frontOpenings = storey == 0
				? new[]
				{
					new Opening(-9.15f, 2.0f, 0.48f, 2.28f),
					new Opening(0.0f, 4.3f, 0.0f, 2.34f),
					new Opening(9.15f, 2.0f, 0.48f, 2.28f),
				}
				: new[]
				{
					new Opening(-9.1f, 2.0f, 0.48f, 2.28f),
					new Opening(-3.1f, 2.0f, 0.48f, 2.28f),
					new Opening(3.1f, 2.0f, 0.48f, 2.28f),
					new Opening(9.1f, 2.0f, 0.48f, 2.28f),
				};

			AddWallXWithOpenings(
				shell,
				$"{prefix}FrontBrick",
				baseY,
				12.82f,
				-13.8f,
				13.8f,
				0.36f,
				_brick,
				frontOpenings);
			AddWallXWithOpenings(
				shell,
				$"{prefix}FrontPlaster",
				baseY,
				12.61f,
				-13.62f,
				13.62f,
				0.045f,
				_wall,
				frontOpenings,
				collision: false);

			Opening[] backOpenings =
			{
				new Opening(-8.9f, 2.0f, 0.48f, 2.28f),
				new Opening(-3.0f, 2.0f, 0.48f, 2.28f),
				new Opening(3.0f, 2.0f, 0.48f, 2.28f),
				new Opening(8.9f, 2.0f, 0.48f, 2.28f),
			};
			AddWallXWithOpenings(
				shell,
				$"{prefix}RearBrick",
				baseY,
				-12.82f,
				-13.8f,
				13.8f,
				0.36f,
				_brick,
				backOpenings);
			AddWallXWithOpenings(
				shell,
				$"{prefix}RearPlaster",
				baseY,
				-12.61f,
				-13.62f,
				13.62f,
				0.045f,
				_wall,
				backOpenings,
				collision: false);

			Opening[] sideOpenings =
			{
				new Opening(-8.4f, 2.0f, 0.48f, 2.28f),
				new Opening(-2.2f, 2.0f, 0.48f, 2.28f),
				new Opening(4.2f, 2.0f, 0.48f, 2.28f),
				new Opening(9.3f, 2.0f, 0.48f, 2.28f),
			};
			Opening[] eastSideOpenings = storey == 0
				? new[]
				{
					new Opening(-8.4f, 2.2f, 0.0f, 2.36f),
					new Opening(-2.2f, 2.0f, 0.48f, 2.28f),
					new Opening(4.2f, 2.0f, 0.48f, 2.28f),
					new Opening(9.3f, 2.0f, 0.48f, 2.28f),
				}
				: sideOpenings;
			AddWallZWithOpenings(
				shell,
				$"{prefix}WestBrick",
				baseY,
				-13.82f,
				-12.8f,
				12.8f,
				0.36f,
				_brick,
				sideOpenings);
			AddWallZWithOpenings(
				shell,
				$"{prefix}WestPlaster",
				baseY,
				-13.61f,
				-12.62f,
				12.62f,
				0.045f,
				_wall,
				sideOpenings,
				collision: false);
			AddWallZWithOpenings(
				shell,
				$"{prefix}EastBrick",
				baseY,
				13.82f,
				-12.8f,
				12.8f,
				0.36f,
				_brick,
				eastSideOpenings);
			AddWallZWithOpenings(
				shell,
				$"{prefix}EastPlaster",
				baseY,
				13.61f,
				-12.62f,
				12.62f,
				0.045f,
				_wall,
				eastSideOpenings,
				collision: false);
		}

		Node3D windows = AddNode<Node3D>(shell, "ExternalWindows");
		AddFrontWindows(windows);
		AddRearWindows(windows);
		AddSideWindows(windows);

		AddBox(
			shell,
			"FrontParapet",
			new Vector3(0.0f, 7.25f, 12.88f),
			new Vector3(28.2f, 0.9f, 0.42f),
			_brick);
		AddBox(
			shell,
			"RearParapet",
			new Vector3(0.0f, 7.15f, -12.88f),
			new Vector3(28.2f, 0.7f, 0.42f),
			_brick);
		AddBox(
			shell,
			"WestParapet",
			new Vector3(-13.88f, 7.15f, 0.0f),
			new Vector3(0.42f, 0.7f, 25.8f),
			_brick);
		AddBox(
			shell,
			"EastParapet",
			new Vector3(13.88f, 7.15f, 0.0f),
			new Vector3(0.42f, 0.7f, 25.8f),
			_brick);
	}

	private void BuildGroundFloorRooms(Node3D parent)
	{
		Node3D rooms = AddNode<Node3D>(parent, "GroundFloorRooms");

		AddWallZWithOpenings(
			rooms,
			"WestCorridorWall",
			GroundFloorTop,
			-2.1f,
			-12.6f,
			12.6f,
			0.18f,
			_wall,
			DoorOpenings(-8.0f, 0.2f, 8.2f));
		AddWallZWithOpenings(
			rooms,
			"EastCorridorWall",
			GroundFloorTop,
			2.1f,
			-12.6f,
			12.6f,
			0.18f,
			_wall,
			DoorOpenings(-8.0f, 1.4f, 6.4f, 10.5f));

		AddWallXWithOpenings(
			rooms,
			"WestFrontDivider",
			GroundFloorTop,
			4.55f,
			-13.6f,
			-2.1f,
			0.18f,
			_wall);
		AddWallXWithOpenings(
			rooms,
			"WestRearDivider",
			GroundFloorTop,
			-3.55f,
			-13.6f,
			-2.1f,
			0.18f,
			_wall);
		AddWallXWithOpenings(
			rooms,
			"EastFrontDivider",
			GroundFloorTop,
			4.55f,
			2.1f,
			13.6f,
			0.18f,
			_wall);
		AddWallXWithOpenings(
			rooms,
			"EastRearDivider",
			GroundFloorTop,
			-3.55f,
			2.1f,
			13.6f,
			0.18f,
			_wall);

		// Two fully enclosed, tiled restrooms sit behind the nurse office.
		AddWallZWithOpenings(
			rooms,
			"RestroomEntryWall",
			GroundFloorTop,
			7.55f,
			4.65f,
			12.55f,
			0.18f,
			_wall,
			DoorOpenings(6.55f, 10.55f));
		AddWallXWithOpenings(
			rooms,
			"RestroomDivider",
			GroundFloorTop,
			8.55f,
			7.55f,
			13.6f,
			0.18f,
			_wall);

		Node3D doors = AddNode<Node3D>(rooms, "OpenRoomDoors");
		foreach ((float x, float z, float yaw) door in new[]
		{
			(-2.1f, -8.0f, 0.0f),
			(-2.1f, 0.2f, 0.0f),
			(-2.1f, 8.2f, 0.0f),
			(2.1f, -8.0f, 180.0f),
			(2.1f, 1.4f, 180.0f),
			(2.1f, 6.4f, 180.0f),
			(2.1f, 10.5f, 180.0f),
		})
		{
			AddProp(
				doors,
				$"{UserPropRoot}wood_door.tscn",
				$"Door_{door.x:0.0}_{door.z:0.0}",
				new Vector3(door.x, GroundFloorTop, door.z - 0.46f),
				new Vector3(0.0f, door.yaw, 0.0f));
		}

		AddProp(
			doors,
			$"{UserPropRoot}hospital_door.tscn",
			"NorthRestroomDoor",
			new Vector3(7.55f, GroundFloorTop, 10.1f),
			new Vector3(0.0f, 0.0f, 0.0f));
		AddProp(
			doors,
			$"{UserPropRoot}hospital_door.tscn",
			"SouthRestroomDoor",
			new Vector3(7.55f, GroundFloorTop, 6.1f),
			new Vector3(0.0f, 0.0f, 0.0f));
	}

	private void BuildUpperFloorRooms(Node3D parent)
	{
		Node3D rooms = AddNode<Node3D>(parent, "UpperFloorRooms");

		AddWallZWithOpenings(
			rooms,
			"WestCorridorWall",
			UpperFloorTop,
			-2.1f,
			-12.6f,
			12.6f,
			0.18f,
			_wall,
			DoorOpenings(-8.1f, 0.0f, 8.1f));
		AddWallZWithOpenings(
			rooms,
			"EastCorridorWall",
			UpperFloorTop,
			2.1f,
			-12.6f,
			12.6f,
			0.18f,
			_wall,
			DoorOpenings(-8.1f, 3.9f, 8.1f));

		foreach (float dividerZ in new[] { -3.55f, 4.55f })
		{
			AddWallXWithOpenings(
				rooms,
				$"WestDivider_{dividerZ:0.0}",
				UpperFloorTop,
				dividerZ,
				-13.6f,
				-2.1f,
				0.18f,
				_wall);
			AddWallXWithOpenings(
				rooms,
				$"EastDivider_{dividerZ:0.0}",
				UpperFloorTop,
				dividerZ,
				2.1f,
				13.6f,
				0.18f,
				_wall);
		}

		Node3D doors = AddNode<Node3D>(rooms, "OpenClassroomDoors");
		foreach ((float x, float z, float yaw) door in new[]
		{
			(-2.1f, -8.1f, 0.0f),
			(-2.1f, 0.0f, 0.0f),
			(-2.1f, 8.1f, 0.0f),
			(2.1f, -8.1f, 180.0f),
			(2.1f, 3.9f, 180.0f),
			(2.1f, 8.1f, 180.0f),
		})
		{
			AddProp(
				doors,
				$"{UserPropRoot}wood_door.tscn",
				$"Door_{door.x:0.0}_{door.z:0.0}",
				new Vector3(door.x, UpperFloorTop, door.z - 0.46f),
				new Vector3(0.0f, door.yaw, 0.0f));
		}
	}

	private void BuildStairwell(Node3D parent)
	{
		Node3D stairwell = AddNode<Node3D>(parent, "Stairwell");
		AddProp(
			stairwell,
			$"{UserPropRoot}wooden_stairs.tscn",
			"AuthoredWoodenStairs",
			new Vector3(5.1f, GroundFloorTop, 1.6f),
			new Vector3(0.0f, 180.0f, 0.0f));

		StaticBody3D ramp = AddNode<StaticBody3D>(
			stairwell,
			"InvisibleWalkableStairRamp");
		ramp.Position = new Vector3(5.1f, 1.80f, 1.6f);
		ramp.RotationDegrees = new Vector3(0.0f, 0.0f, -40.0f);
		CollisionShape3D rampCollision = AddNode<CollisionShape3D>(
			ramp,
			"Collision");
		rampCollision.Shape = new BoxShape3D
		{
			Size = new Vector3(5.24f, 0.12f, 2.04f),
		};

		AddBox(
			stairwell,
			"BottomLanding",
			new Vector3(7.65f, 0.02f, 1.6f),
			new Vector3(1.1f, 0.2f, 2.5f),
			_floor);
		AddBox(
			stairwell,
			"TopLanding",
			new Vector3(2.55f, 3.38f, 1.6f),
			new Vector3(1.0f, 0.2f, 2.5f),
			_floor);

		stairwell.SetMeta("traversal", "forward_only_no_jump");
		stairwell.SetMeta("rise_metres", StoreyHeight);
	}

	private void BuildExteriorIdentity(Node3D parent)
	{
		AddBox(
			parent,
			"CentralTowerCrown",
			new Vector3(0.0f, 7.72f, 13.08f),
			new Vector3(13.2f, 1.18f, 0.56f),
			_brick);
		AddBox(
			parent,
			"CentralTowerCap",
			new Vector3(0.0f, 8.36f, 13.1f),
			new Vector3(13.75f, 0.18f, 0.7f),
			_concrete);
		foreach (float x in new[] { -6.25f, 6.25f })
		{
			AddBox(
				parent,
				$"CentralTowerPilaster_{x:0.00}",
				new Vector3(x, 4.05f, 13.09f),
				new Vector3(0.52f, 7.85f, 0.58f),
				_brick);
		}
		foreach (float x in new[] { -13.45f, 13.45f })
		{
			AddBox(
				parent,
				$"FacadeCornerPilaster_{x:0.00}",
				new Vector3(x, 3.52f, 13.08f),
				new Vector3(0.48f, 6.72f, 0.54f),
				_brick);
		}
		AddBox(
			parent,
			"FacadeMasonryBand",
			new Vector3(0.0f, 3.46f, 13.11f),
			new Vector3(27.2f, 0.22f, 0.18f),
			_concrete,
			collision: false);
		foreach ((float x, float y) window in new[]
		{
			(-9.15f, 0.6f),
			(9.15f, 0.6f),
			(-9.1f, 3.96f),
			(-3.1f, 3.96f),
			(3.1f, 3.96f),
			(9.1f, 3.96f),
		})
		{
			AddBox(
				parent,
				$"WindowSill_{window.x:0.0}_{window.y:0.0}",
				new Vector3(window.x, window.y + 0.02f, 13.13f),
				new Vector3(2.28f, 0.12f, 0.32f),
				_concrete,
				collision: false);
			AddBox(
				parent,
				$"WindowLintel_{window.x:0.0}_{window.y:0.0}",
				new Vector3(window.x, window.y + 2.32f, 13.13f),
				new Vector3(2.3f, 0.16f, 0.32f),
				_concrete,
				collision: false);
		}
		foreach (float x in new[] { -2.35f, 2.35f })
		{
			AddBox(
				parent,
				$"EntranceMasonryPier_{x:0.00}",
				new Vector3(x, 1.28f, 13.12f),
				new Vector3(0.28f, 2.5f, 0.36f),
				_concrete,
				collision: false);
		}

		AddBox(
			parent,
			"EntranceCanopy",
			new Vector3(0.0f, 2.85f, 13.75f),
			new Vector3(6.4f, 0.18f, 2.0f),
			_greenMetal);
		AddBox(
			parent,
			"CanopyColumnWest",
			new Vector3(-2.9f, 1.45f, 14.45f),
			new Vector3(0.22f, 2.8f, 0.22f),
			_greenMetal);
		AddBox(
			parent,
			"CanopyColumnEast",
			new Vector3(2.9f, 1.45f, 14.45f),
			new Vector3(0.22f, 2.8f, 0.22f),
			_greenMetal);
		AddBox(
			parent,
			"SchoolSignBacking",
			new Vector3(0.0f, 5.82f, 13.08f),
			new Vector3(12.6f, 1.32f, 0.16f),
			_darkWood,
			collision: false);

		Label3D sign = AddNode<Label3D>(parent, "SchoolNameSign");
		sign.Position = new Vector3(0.0f, 5.96f, 13.18f);
		sign.Text = "ASHWOOD COUNTY HIGH SCHOOL";
		sign.FontSize = 72;
		sign.OutlineSize = 10;
		sign.PixelSize = 0.0084f;
		sign.Modulate = new Color(0.9f, 0.78f, 0.43f);
		sign.HorizontalAlignment = HorizontalAlignment.Center;
		sign.NoDepthTest = false;

		Label3D motto = AddNode<Label3D>(parent, "SchoolMottoSign");
		motto.Position = new Vector3(0.0f, 5.47f, 13.18f);
		motto.Text = "HOME OF THE TIMBERWOLVES  •  EST. 1954";
		motto.FontSize = 52;
		motto.OutlineSize = 8;
		motto.PixelSize = 0.0068f;
		motto.Modulate = new Color(0.78f, 0.75f, 0.63f);
		motto.HorizontalAlignment = HorizontalAlignment.Center;

		Node3D entranceDoors = AddNode<Node3D>(parent, "OpenDoubleEntrance");
		AddProp(
			entranceDoors,
			$"{UserPropRoot}hospital_door.tscn",
			"LeftEntranceLeaf",
			new Vector3(-1.56f, GroundFloorTop, 12.5f),
			new Vector3(0.0f, 86.0f, 0.0f));
		AddProp(
			entranceDoors,
			$"{UserPropRoot}hospital_door.tscn",
			"RightEntranceLeaf",
			new Vector3(1.56f, GroundFloorTop, 12.5f),
			new Vector3(0.0f, -86.0f, 0.0f));

		AddBox(
			parent,
			"EntranceThresholdRamp",
			new Vector3(0.0f, 0.06f, 13.72f),
			new Vector3(4.5f, 0.12f, 1.85f),
			_concrete,
			rotationDegrees: new Vector3(4.0f, 0.0f, 0.0f));

		AddProp(
			parent,
			$"{UserPropRoot}bulletin_board.tscn",
			"WeatheredPublicNoticeBoard",
			new Vector3(-7.1f, 1.5f, 13.12f),
			Vector3.Zero,
			new Vector3(1.25f, 1.25f, 1.25f));
		AddProp(
			parent,
			$"{UserPropRoot}trash_can.tscn",
			"EntranceTrashCan",
			new Vector3(5.5f, GroundFloorTop, 13.65f),
			new Vector3(0.0f, 12.0f, 0.0f),
			new Vector3(0.85f, 0.85f, 0.85f));

		AddProp(
			parent,
			$"{PharmacyPropRoot}wall_clock/wall_clock_1k.gltf",
			"FacadeClock",
			new Vector3(0.0f, 7.58f, 13.42f),
			Vector3.Zero,
			new Vector3(1.18f, 1.18f, 1.18f));
	}

	private void BuildGymAnnex(
		Node3D architectureParent,
		Node3D dressingParent,
		Node3D lightingParent)
	{
		const float annexWallHeight = 6.48f;
		Node3D annex = AddNode<Node3D>(
			architectureParent,
			"DoubleHeightGymAnnex");

		AddBox(
			annex,
			"GymFloor",
			new Vector3(17.55f, 0.02f, -7.65f),
			new Vector3(7.05f, 0.2f, 10.15f),
			_gymFloor);
		AddBox(
			annex,
			"GymCeiling",
			new Vector3(17.55f, 6.59f, -7.65f),
			new Vector3(6.75f, 0.08f, 9.85f),
			_ceiling,
			collision: false);
		AddBox(
			annex,
			"GymRoof",
			new Vector3(17.55f, 6.78f, -7.65f),
			new Vector3(7.3f, 0.22f, 10.4f),
			_concrete);

		Opening[] endOpenings =
		{
			new Opening(16.0f, 1.55f, 0.65f, 3.0f),
			new Opening(19.2f, 1.55f, 0.65f, 3.0f),
		};
		Opening[] eastOpenings =
		{
			new Opening(-10.25f, 1.55f, 0.65f, 3.0f),
			new Opening(-5.0f, 1.55f, 0.65f, 3.0f),
		};
		Opening[] connectionOpening =
		{
			new Opening(-8.4f, 2.2f, 0.0f, 2.36f),
		};

		AddWallXWithOpenings(
			annex,
			"FrontBrick",
			GroundFloorTop,
			-2.58f,
			14.02f,
			21.08f,
			0.36f,
			_brick,
			endOpenings,
			wallHeight: annexWallHeight);
		AddWallXWithOpenings(
			annex,
			"FrontPlaster",
			GroundFloorTop,
			-2.79f,
			14.2f,
			20.9f,
			0.045f,
			_wall,
			endOpenings,
			collision: false,
			wallHeight: annexWallHeight);
		AddWallXWithOpenings(
			annex,
			"RearBrick",
			GroundFloorTop,
			-12.72f,
			14.02f,
			21.08f,
			0.36f,
			_brick,
			endOpenings,
			wallHeight: annexWallHeight);
		AddWallXWithOpenings(
			annex,
			"RearPlaster",
			GroundFloorTop,
			-12.51f,
			14.2f,
			20.9f,
			0.045f,
			_wall,
			endOpenings,
			collision: false,
			wallHeight: annexWallHeight);
		AddWallZWithOpenings(
			annex,
			"EastBrick",
			GroundFloorTop,
			21.08f,
			-12.72f,
			-2.58f,
			0.36f,
			_brick,
			eastOpenings,
			wallHeight: annexWallHeight);
		AddWallZWithOpenings(
			annex,
			"EastPlaster",
			GroundFloorTop,
			20.87f,
			-12.51f,
			-2.79f,
			0.045f,
			_wall,
			eastOpenings,
			collision: false,
			wallHeight: annexWallHeight);
		AddWallZWithOpenings(
			annex,
			"WestReturnBrick",
			GroundFloorTop,
			14.02f,
			-12.72f,
			-2.58f,
			0.36f,
			_brick,
			connectionOpening,
			wallHeight: annexWallHeight);
		AddWallZWithOpenings(
			annex,
			"WestReturnPlaster",
			GroundFloorTop,
			14.23f,
			-12.51f,
			-2.79f,
			0.045f,
			_wall,
			connectionOpening,
			collision: false,
			wallHeight: annexWallHeight);

		Node3D windows = AddNode<Node3D>(annex, "GymWindows");
		foreach (float x in new[] { 16.0f, 19.2f })
		{
			AddProp(
				windows,
				$"{UserPropRoot}double_window.tscn",
				$"FrontWindow_{x:0.0}",
				new Vector3(x, 0.72f, -2.58f),
				Vector3.Zero);
			AddProp(
				windows,
				$"{UserPropRoot}double_window.tscn",
				$"RearWindow_{x:0.0}",
				new Vector3(x, 0.72f, -12.72f),
				new Vector3(0.0f, 180.0f, 0.0f));
		}
		foreach (float z in new[] { -10.25f, -5.0f })
		{
			AddProp(
				windows,
				$"{UserPropRoot}double_window.tscn",
				$"EastWindow_{z:0.0}",
				new Vector3(21.08f, 0.72f, z),
				new Vector3(0.0f, 90.0f, 0.0f));
		}
		AddProp(
			annex,
			$"{UserPropRoot}classroom_double_door.tscn",
			"LockedGymEmergencyExit",
			new Vector3(21.11f, GroundFloorTop, -7.65f),
			new Vector3(0.0f, 90.0f, 0.0f));

		AddProp(
			annex,
			$"{UserPropRoot}hospital_door.tscn",
			"OpenGymConnectorDoor",
			new Vector3(13.98f, GroundFloorTop, -8.95f),
			new Vector3(0.0f, 0.0f, 0.0f));

		Node3D gym = AddNode<Node3D>(
			dressingParent,
			"DoubleHeightGymDressing");
		AddProp(
			gym,
			$"{UserPropRoot}basketball_hoop.tscn",
			"NorthBasketballHoop",
			new Vector3(17.55f, GroundFloorTop, -12.18f),
			Vector3.Zero);
		AddProp(
			gym,
			$"{UserPropRoot}basketball_hoop.tscn",
			"SouthBasketballHoop",
			new Vector3(17.55f, GroundFloorTop, -3.1f),
			new Vector3(0.0f, 180.0f, 0.0f));
		foreach ((float x, float z, float yaw) ball in new[]
		{
			(15.7f, -5.0f, 18.0f),
			(17.9f, -8.0f, -33.0f),
			(19.6f, -10.5f, 61.0f),
			(19.0f, -4.2f, 9.0f),
		})
		{
			AddProp(
				gym,
				$"{UserPropRoot}basketball.tscn",
				$"GymBall_{ball.x:0.0}_{ball.z:0.0}",
				new Vector3(ball.x, 0.25f, ball.z),
				new Vector3(8.0f, ball.yaw, 5.0f));
		}
		foreach ((float z, string type) locker in new[]
		{
			(-5.0f, "old"),
			(-9.8f, "damaged"),
		})
		{
			string lockerScene = locker.type == "damaged"
				? "damaged_school_lockers.tscn"
				: "old_school_lockers.tscn";
			AddProp(
				gym,
				$"{UserPropRoot}{lockerScene}",
				$"GymLocker_{locker.z:0.0}",
				new Vector3(20.55f, GroundFloorTop, locker.z),
				new Vector3(0.0f, -90.0f, 0.0f),
				new Vector3(0.84f, 0.84f, 0.84f));
		}
		AddProp(
			gym,
			"res://scenes/world/ashwood/presentation/props/apocalypse_park_bench.tscn",
			"GymBench",
			new Vector3(15.1f, GroundFloorTop, -7.6f),
			new Vector3(0.0f, 90.0f, 0.0f));
		AddProp(
			gym,
			$"{UserPropRoot}trash_can.tscn",
			"GymTrashCan",
			new Vector3(20.2f, GroundFloorTop, -3.6f),
			new Vector3(0.0f, -18.0f, 0.0f),
			new Vector3(0.72f, 0.72f, 0.72f));
		AddProp(
			gym,
			$"{UserPropRoot}paper_debris.tscn",
			"GymScatteredPapers",
			new Vector3(16.1f, GroundFloorTop + 0.015f, -10.4f),
			new Vector3(0.0f, 26.0f, 0.0f),
			new Vector3(0.7f, 0.7f, 0.7f));

		AddBox(
			annex,
			"GymSignBacking",
			new Vector3(17.55f, 4.65f, -2.36f),
			new Vector3(5.6f, 0.86f, 0.14f),
			_darkWood,
			collision: false);
		Label3D gymSign = AddNode<Label3D>(annex, "GymnasiumSign");
		gymSign.Position = new Vector3(17.55f, 4.67f, -2.27f);
		gymSign.Text = "ASHWOOD GYMNASIUM";
		gymSign.FontSize = 58;
		gymSign.OutlineSize = 8;
		gymSign.PixelSize = 0.007f;
		gymSign.Modulate = new Color(0.9f, 0.78f, 0.43f);
		gymSign.HorizontalAlignment = HorizontalAlignment.Center;

		foreach (Vector3 position in new[]
		{
			new Vector3(16.0f, 5.55f, -5.1f),
			new Vector3(19.1f, 5.55f, -5.1f),
			new Vector3(16.0f, 5.55f, -10.1f),
			new Vector3(19.1f, 5.55f, -10.1f),
		})
		{
			OmniLight3D light = AddNode<OmniLight3D>(
				lightingParent,
				$"GymLight_{position.X:0.0}_{position.Z:0.0}");
			light.Position = position;
			light.LightColor = new Color(1.0f, 0.87f, 0.69f);
			light.LightEnergy = 0.72f;
			light.OmniRange = 6.8f;
			light.ShadowEnabled = false;
			AddProp(
				lightingParent,
				$"{PharmacyPropRoot}mounted_fluorescent_lights/mounted_fluorescent_lights_1k.gltf",
				$"GymFixture_{position.X:0.0}_{position.Z:0.0}",
				new Vector3(position.X, 6.32f, position.Z),
				new Vector3(0.0f, 90.0f, 0.0f),
				new Vector3(0.9f, 0.9f, 0.9f));
		}

		annex.SetMeta("room", "double_height_gymnasium");
		annex.SetMeta("clear_play_area", "4.2m x 7.2m");
	}

	private void BuildInteriorDressing(Node3D parent)
	{
		BuildGroundFloorDressing(
			AddNode<Node3D>(parent, "GroundFloor"));
		BuildUpperFloorDressing(
			AddNode<Node3D>(parent, "UpperFloor"));
		BuildAbandonmentDressing(
			AddNode<Node3D>(parent, "AbandonmentDetails"));
	}

	private void BuildGroundFloorDressing(Node3D parent)
	{
		// Administration / reception.
		AddProp(
			parent,
			$"{PharmacyPropRoot}metal_office_desk/metal_office_desk_1k.gltf",
			"ReceptionDesk",
			new Vector3(-7.4f, GroundFloorTop, 8.0f),
			new Vector3(0.0f, 90.0f, 0.0f));
		AddProp(
			parent,
			$"{UserPropRoot}office_chair.tscn",
			"ReceptionChair",
			new Vector3(-8.7f, GroundFloorTop, 8.0f),
			new Vector3(0.0f, -90.0f, 0.0f));
		AddProp(
			parent,
			$"{UserPropRoot}file_cabinet.tscn",
			"AdminFileCabinetA",
			new Vector3(-12.9f, GroundFloorTop, 10.7f),
			new Vector3(0.0f, 90.0f, 0.0f));
		AddProp(
			parent,
			$"{UserPropRoot}rusty_cabinet.tscn",
			"AdminFileCabinetB",
			new Vector3(-12.8f, GroundFloorTop, 6.0f),
			new Vector3(0.0f, 92.0f, 0.0f));
		AddProp(
			parent,
			$"{UserPropRoot}antique_globe.tscn",
			"AdministrationGlobe",
			new Vector3(-7.2f, 0.95f, 7.8f),
			new Vector3(0.0f, 18.0f, 0.0f));
		AddProp(
			parent,
			$"{UserPropRoot}coffee_mug.tscn",
			"ReceptionCoffeeMug",
			new Vector3(-7.0f, 0.88f, 7.35f),
			new Vector3(0.0f, -22.0f, 0.0f));
		AddProp(
			parent,
			$"{UserPropRoot}corkboard.tscn",
			"AdministrationCorkboard",
			new Vector3(-7.5f, 1.8f, 4.68f),
			Vector3.Zero);

		// Library: only a few hero shelves because the model is detailed.
		foreach ((Vector3 position, float yaw, Vector3 scale) shelf in new[]
		{
			(new Vector3(-12.95f, GroundFloorTop, 1.9f), 0.0f,
				new Vector3(0.9f, 0.9f, 0.9f)),
			(new Vector3(-12.95f, GroundFloorTop, -1.6f), 0.0f,
				new Vector3(0.9f, 0.9f, 0.9f)),
			(new Vector3(-7.9f, GroundFloorTop, -2.65f), 90.0f,
				new Vector3(0.8f, 0.8f, 0.8f)),
			(new Vector3(-7.8f, GroundFloorTop, 3.72f), 90.0f,
				new Vector3(0.76f, 0.76f, 0.76f)),
		})
		{
			AddProp(
				parent,
				$"{UserPropRoot}hero_bookshelf.tscn",
				$"LibraryShelf_{shelf.position.X:0.0}_{shelf.position.Z:0.0}",
				shelf.position,
				new Vector3(0.0f, shelf.yaw, 0.0f),
				shelf.scale);
		}
		AddProp(
			parent,
			$"{BakeryPropRoot}wooden_table_02/wooden_table_02_1k.gltf",
			"LibraryReadingTable",
			new Vector3(-7.2f, GroundFloorTop, 1.0f),
			new Vector3(0.0f, 90.0f, 0.0f),
			new Vector3(0.82f, 0.82f, 0.82f));
		AddProp(
			parent,
			$"{UserPropRoot}books_cluster.tscn",
			"LibraryTableBooks",
			new Vector3(-7.2f, 0.96f, 1.0f),
			new Vector3(0.0f, 24.0f, 0.0f),
			new Vector3(0.72f, 0.72f, 0.72f));
		foreach ((Vector3 position, float yaw) chair in new[]
		{
			(new Vector3(-8.65f, GroundFloorTop, 1.0f), 90.0f),
			(new Vector3(-5.75f, GroundFloorTop, 1.0f), -90.0f),
			(new Vector3(-7.2f, GroundFloorTop, 2.05f), 180.0f),
		})
		{
			AddSchoolChair(
				parent,
				$"LibraryChair_{chair.position.X:0.0}_{chair.position.Z:0.0}",
				chair.position,
				chair.yaw);
		}
		AddProp(
			parent,
			$"{UserPropRoot}antique_globe.tscn",
			"LibraryGlobe",
			new Vector3(-11.95f, 1.05f, 3.15f),
			new Vector3(0.0f, -25.0f, 0.0f));
		AddProp(
			parent,
			$"{UserPropRoot}paper_stack.tscn",
			"LibraryAbandonedHomework",
			new Vector3(-6.7f, 0.96f, 1.15f),
			new Vector3(0.0f, -14.0f, 0.0f),
			new Vector3(0.42f, 0.42f, 0.42f));

		// Cafeteria.
		foreach ((float x, float z, float yaw) table in new[]
		{
			(-10.4f, -6.2f, 90.0f),
			(-5.5f, -6.2f, 90.0f),
			(-10.4f, -10.0f, 90.0f),
			(-5.5f, -10.0f, 90.0f),
		})
		{
			AddProp(
				parent,
				$"{BakeryPropRoot}wooden_table_02/wooden_table_02_1k.gltf",
				$"CafeteriaTable_{table.x:0.0}_{table.z:0.0}",
				new Vector3(table.x, GroundFloorTop, table.z),
				new Vector3(0.0f, table.yaw, 0.0f),
				new Vector3(0.78f, 0.78f, 0.78f));
			AddSchoolChair(
				parent,
				$"CafeteriaChairA_{table.x:0.0}_{table.z:0.0}",
				new Vector3(table.x - 1.25f, GroundFloorTop, table.z),
				90.0f);
			AddSchoolChair(
				parent,
				$"CafeteriaChairB_{table.x:0.0}_{table.z:0.0}",
				new Vector3(table.x + 1.25f, GroundFloorTop, table.z),
				-90.0f);
		}
		AddProp(
			parent,
			$"{UserPropRoot}old_metal_table.tscn",
			"CafeteriaServingTable",
			new Vector3(-12.3f, GroundFloorTop, -8.1f),
			Vector3.Zero);
		AddProp(
			parent,
			$"{PharmacyPropRoot}steel_frame_shelves_02/steel_frame_shelves_02_1k.gltf",
			"CafeteriaDryShelf",
			new Vector3(-11.9f, GroundFloorTop, -11.8f),
			new Vector3(0.0f, 90.0f, 0.0f),
			new Vector3(0.78f, 0.78f, 0.78f));
		foreach ((float x, float y, float z, float yaw) food in new[]
		{
			(-11.9f, 0.52f, -11.45f, 12.0f),
			(-11.9f, 1.15f, -11.45f, -9.0f),
			(-12.25f, 0.92f, -8.1f, 21.0f),
		})
		{
			AddProp(
				parent,
				$"{PharmacyPropRoot}long_life_food/long_life_food_1k.gltf",
				$"CafeteriaFood_{food.y:0.00}_{food.z:0.0}",
				new Vector3(food.x, food.y, food.z),
				new Vector3(0.0f, food.yaw, 0.0f),
				new Vector3(0.8f, 0.8f, 0.8f));
		}
		AddProp(
			parent,
			$"{UserPropRoot}trash_can.tscn",
			"CafeteriaTrashCan",
			new Vector3(-3.2f, GroundFloorTop, -11.5f),
			new Vector3(0.0f, 12.0f, 0.0f),
			new Vector3(0.75f, 0.75f, 0.75f));

		// Nurse office and enclosed restrooms.
		AddProp(
			parent,
			$"{PharmacyPropRoot}metal_office_desk/metal_office_desk_1k.gltf",
			"NurseDesk",
			new Vector3(4.7f, GroundFloorTop, 8.0f),
			new Vector3(0.0f, 90.0f, 0.0f));
		AddProp(
			parent,
			$"{UserPropRoot}office_chair.tscn",
			"NurseChair",
			new Vector3(5.8f, GroundFloorTop, 8.0f),
			new Vector3(0.0f, 90.0f, 0.0f));
		AddProp(
			parent,
			$"{PharmacyPropRoot}medical_box/medical_box_1k.gltf",
			"NurseMedicalBox",
			new Vector3(4.7f, 0.94f, 7.75f),
			new Vector3(0.0f, 10.0f, 0.0f));
		AddProp(
			parent,
			$"{PharmacyPropRoot}vintage_crutches_01/vintage_crutches_01_1k.gltf",
			"NurseCrutches",
			new Vector3(6.9f, GroundFloorTop, 5.2f),
			new Vector3(0.0f, 115.0f, 0.0f));

		AddRestroomFixtures(parent, "North", 10.4f);
		AddRestroomFixtures(parent, "South", 6.35f);

		// The low athletic room acts as equipment storage and a warm-up space;
		// the full-height gymnasium continues through its east connector.
		foreach ((Vector3 position, float scale) ball in new[]
		{
			(new Vector3(5.7f, 0.24f, -7.0f), 1.0f),
			(new Vector3(9.4f, 0.24f, -9.2f), 1.02f),
			(new Vector3(11.7f, 0.24f, -5.3f), 0.98f),
		})
		{
			AddProp(
				parent,
				$"{UserPropRoot}basketball.tscn",
				$"Basketball_{ball.position.X:0.0}_{ball.position.Z:0.0}",
				ball.position,
				new Vector3(7.0f, ball.position.X * 9.0f, 11.0f),
				new Vector3(ball.scale, ball.scale, ball.scale));
		}
		AddProp(
			parent,
			$"{UserPropRoot}old_school_lockers.tscn",
			"AthleticsLockerBank",
			new Vector3(12.65f, GroundFloorTop, -6.2f),
			new Vector3(0.0f, -90.0f, 0.0f),
			new Vector3(0.82f, 0.82f, 0.82f));
		AddProp(
			parent,
			$"{UserPropRoot}damaged_school_lockers.tscn",
			"DamagedAthleticsLockerBank",
			new Vector3(12.65f, GroundFloorTop, -10.6f),
			new Vector3(0.0f, -90.0f, 0.0f),
			new Vector3(0.78f, 0.78f, 0.78f));
		AddProp(
			parent,
			$"{UserPropRoot}old_metal_table.tscn",
			"AthleticsEquipmentTable",
			new Vector3(5.2f, GroundFloorTop, -10.6f),
			new Vector3(0.0f, 90.0f, 0.0f));

		BuildCorridorDressing(parent, GroundFloorTop, "Ground");
	}

	private void BuildUpperFloorDressing(Node3D parent)
	{
		AddClassroom(
			parent,
			"Classroom201",
			new Vector2(-8.0f, 8.5f),
			UpperFloorTop,
			facesPositiveZ: false);
		AddClassroom(
			parent,
			"Classroom202",
			new Vector2(-8.0f, 0.4f),
			UpperFloorTop,
			facesPositiveZ: true);
		AddClassroom(
			parent,
			"Classroom203",
			new Vector2(-8.0f, -8.0f),
			UpperFloorTop,
			facesPositiveZ: true);
		AddClassroom(
			parent,
			"Classroom204",
			new Vector2(8.1f, 8.5f),
			UpperFloorTop,
			facesPositiveZ: false);
		AddClassroom(
			parent,
			"Classroom205",
			new Vector2(8.1f, -8.0f),
			UpperFloorTop,
			facesPositiveZ: true);

		// The upper east-middle room is a faculty workroom surrounding, but not
		// obstructing, the stair landing.
		AddProp(
			parent,
			$"{PharmacyPropRoot}metal_office_desk/metal_office_desk_1k.gltf",
			"FacultyDesk",
			new Vector3(10.5f, UpperFloorTop, 0.0f),
			new Vector3(0.0f, 90.0f, 0.0f));
		AddProp(
			parent,
			$"{UserPropRoot}file_cabinet.tscn",
			"FacultyFileCabinet",
			new Vector3(12.9f, UpperFloorTop, 2.7f),
			new Vector3(0.0f, 90.0f, 0.0f));
		AddProp(
			parent,
			$"{UserPropRoot}coffee_mug.tscn",
			"FacultyAbandonedMug",
			new Vector3(10.4f, 4.31f, -0.2f),
			new Vector3(0.0f, -28.0f, 0.0f));
		AddProp(
			parent,
			$"{UserPropRoot}post_it_notes.tscn",
			"FacultyNotes",
			new Vector3(10.1f, 4.315f, 0.28f),
			new Vector3(0.0f, 17.0f, 0.0f));

		BuildCorridorDressing(parent, UpperFloorTop, "Upper");
	}

	private void AddClassroom(
		Node3D parent,
		string name,
		Vector2 centre,
		float floorY,
		bool facesPositiveZ)
	{
		Node3D classroom = AddNode<Node3D>(parent, name);
		float facingYaw = facesPositiveZ ? 180.0f : 0.0f;
		float rowDirection = facesPositiveZ ? -1.0f : 1.0f;

		for (int row = 0; row < 3; row++)
		{
			for (int column = 0; column < 3; column++)
			{
				float x = centre.X + ((column - 1) * 2.25f);
				float z = centre.Y + ((row - 1.0f) * 1.95f * rowDirection);
				float variation = ((row * 3) + column) switch
				{
					1 => -2.2f,
					3 => 1.8f,
					5 => -1.0f,
					7 => 1.3f,
					_ => 0.0f,
				};
				AddProp(
					classroom,
					$"{UserPropRoot}school_desk.tscn",
					$"StudentDesk_R{row + 1}_C{column + 1}",
					new Vector3(x, floorY, z),
					new Vector3(0.0f, facingYaw + variation, 0.0f));
			}
		}

		float boardZ = centre.Y +
			(facesPositiveZ ? 3.65f : -3.65f);
		AddProp(
			classroom,
			$"{UserPropRoot}whiteboard.tscn",
			"TeachingBoard",
			new Vector3(centre.X, floorY + 1.18f, boardZ),
			new Vector3(0.0f, facesPositiveZ ? 180.0f : 0.0f, 0.0f));
		AddProp(
			classroom,
			$"{UserPropRoot}old_metal_table.tscn",
			"TeacherTable",
			new Vector3(
				centre.X - 3.8f,
				floorY,
				centre.Y + (facesPositiveZ ? 2.65f : -2.65f)),
			new Vector3(0.0f, 90.0f, 0.0f));
		AddProp(
			classroom,
			$"{UserPropRoot}bulletin_board.tscn",
			"ClassBulletin",
			new Vector3(
				centre.X + 3.8f,
				floorY + 1.25f,
				boardZ),
			new Vector3(0.0f, facesPositiveZ ? 180.0f : 0.0f, 0.0f));
		AddProp(
			classroom,
			$"{UserPropRoot}books_cluster.tscn",
			"TeacherBooks",
			new Vector3(
				centre.X - 3.8f,
				floorY + 0.84f,
				centre.Y + (facesPositiveZ ? 2.65f : -2.65f)),
			new Vector3(0.0f, 16.0f, 0.0f),
			new Vector3(0.48f, 0.48f, 0.48f));

		Vector3 teacherSurface = new(
			centre.X - 3.8f,
			floorY + 0.845f,
			centre.Y + (facesPositiveZ ? 2.65f : -2.65f));
		AddProp(
			classroom,
			$"{UserPropRoot}pencil.tscn",
			"TeacherPencil",
			teacherSurface + new Vector3(-0.18f, 0.0f, 0.13f),
			new Vector3(0.0f, 24.0f, 0.0f));
		AddProp(
			classroom,
			$"{UserPropRoot}pen.tscn",
			"TeacherPen",
			teacherSurface + new Vector3(0.05f, 0.0f, -0.12f),
			new Vector3(0.0f, -18.0f, 0.0f));
		AddProp(
			classroom,
			$"{UserPropRoot}eraser.tscn",
			"TeacherEraser",
			teacherSurface + new Vector3(0.22f, 0.0f, 0.08f),
			new Vector3(0.0f, 11.0f, 0.0f));
	}

	private void BuildCorridorDressing(
		Node3D parent,
		float floorY,
		string prefix)
	{
		foreach ((Vector3 position, float yaw, string kind) locker in new[]
		{
			(new Vector3(-1.78f, floorY, -11.25f), 90.0f, "damaged"),
			(new Vector3(-1.78f, floorY, -4.35f), 90.0f, "old"),
			(new Vector3(-1.78f, floorY, 4.75f), 90.0f, "damaged"),
			(new Vector3(-1.78f, floorY, 11.35f), 90.0f, "old"),
			(new Vector3(1.78f, floorY, -11.4f), -90.0f, "old"),
			(new Vector3(1.78f, floorY, -4.45f), -90.0f, "damaged"),
			(new Vector3(1.78f, floorY, 11.75f), -90.0f, "damaged"),
		})
		{
			string scene = locker.kind == "damaged"
				? "damaged_school_lockers.tscn"
				: "old_school_lockers.tscn";
			AddProp(
				parent,
				$"{UserPropRoot}{scene}",
				$"{prefix}HallLocker_{locker.position.Z:0.0}",
				locker.position,
				new Vector3(0.0f, locker.yaw, 0.0f),
				new Vector3(0.82f, 0.82f, 0.82f));
		}

		AddProp(
			parent,
			$"{UserPropRoot}bulletin_board.tscn",
			$"{prefix}HallBulletin",
			new Vector3(-1.93f, floorY + 1.3f, 3.0f),
			new Vector3(0.0f, 90.0f, 0.0f));
		AddProp(
			parent,
			$"{UserPropRoot}trash_can.tscn",
			$"{prefix}HallTrash",
			new Vector3(1.1f, floorY, -11.2f),
			new Vector3(0.0f, -18.0f, 0.0f),
			new Vector3(0.72f, 0.72f, 0.72f));
		AddProp(
			parent,
			$"{UserPropRoot}paper_debris.tscn",
			$"{prefix}HallPaperDebris",
			new Vector3(-0.6f, floorY + 0.01f, -2.4f),
			new Vector3(0.0f, 31.0f, 0.0f),
			new Vector3(0.72f, 0.72f, 0.72f));
	}

	private void BuildAbandonmentDressing(Node3D parent)
	{
		foreach ((Vector3 position, Vector3 rotation, Vector3 scale) paper in new[]
		{
			(new Vector3(-10.3f, 0.14f, 9.0f),
				new Vector3(0.0f, 18.0f, 0.0f), Vector3.One),
			(new Vector3(-5.1f, 0.14f, -9.8f),
				new Vector3(0.0f, -41.0f, 0.0f),
				new Vector3(0.78f, 0.78f, 0.78f)),
			(new Vector3(8.7f, 3.50f, 9.7f),
				new Vector3(0.0f, 63.0f, 0.0f),
				new Vector3(0.66f, 0.66f, 0.66f)),
			(new Vector3(-7.8f, 3.50f, -4.8f),
				new Vector3(0.0f, -12.0f, 0.0f),
				new Vector3(0.72f, 0.72f, 0.72f)),
		})
		{
			AddProp(
				parent,
				$"{UserPropRoot}paper_debris.tscn",
				$"ScatteredPaper_{paper.position.X:0.0}_{paper.position.Z:0.0}",
				paper.position,
				paper.rotation,
				paper.scale);
		}

		foreach ((Vector3 position, Vector3 rotation, Vector3 scale) web in new[]
		{
			(new Vector3(-12.8f, 2.62f, 11.7f),
				new Vector3(0.0f, 90.0f, 0.0f),
				new Vector3(0.55f, 0.55f, 0.55f)),
			(new Vector3(12.8f, 2.6f, -11.4f),
				new Vector3(0.0f, -90.0f, 0.0f),
				new Vector3(0.48f, 0.48f, 0.48f)),
			(new Vector3(-12.8f, 6.0f, -11.2f),
				new Vector3(0.0f, 90.0f, 0.0f),
				new Vector3(0.42f, 0.42f, 0.42f)),
		})
		{
			AddProp(
				parent,
				$"{UserPropRoot}cobweb_pack.tscn",
				$"Cobweb_{web.position.X:0.0}_{web.position.Z:0.0}",
				web.position,
				web.rotation,
				web.scale);
		}
	}

	private void BuildLighting(Node3D parent)
	{
		foreach ((Vector3 position, float energy, float range) light in new[]
		{
			(new Vector3(0.0f, 2.75f, 8.0f), 0.78f, 7.0f),
			(new Vector3(-8.0f, 2.75f, 8.0f), 0.68f, 6.8f),
			(new Vector3(-8.0f, 2.75f, 0.2f), 0.66f, 6.8f),
			(new Vector3(-8.0f, 2.75f, -8.2f), 0.64f, 6.8f),
			(new Vector3(8.0f, 2.75f, 8.0f), 0.66f, 6.8f),
			(new Vector3(8.0f, 2.75f, -8.0f), 0.74f, 7.5f),
			(new Vector3(0.0f, 6.05f, 8.0f), 0.66f, 6.8f),
			(new Vector3(-8.0f, 6.05f, 8.0f), 0.62f, 6.8f),
			(new Vector3(-8.0f, 6.05f, -8.0f), 0.62f, 6.8f),
			(new Vector3(8.0f, 6.05f, 8.0f), 0.62f, 6.8f),
			(new Vector3(8.0f, 6.05f, -8.0f), 0.62f, 6.8f),
		})
		{
			OmniLight3D omni = AddNode<OmniLight3D>(
				parent,
				$"InteriorLight_{light.position.X:0}_{light.position.Y:0.0}_{light.position.Z:0}");
			omni.Position = light.position;
			omni.LightColor = new Color(1.0f, 0.86f, 0.67f);
			omni.LightEnergy = light.energy;
			omni.OmniRange = light.range;
			omni.ShadowEnabled = false;
		}

		foreach ((Vector3 position, float yaw) fixture in new[]
		{
			(new Vector3(0.0f, 3.12f, 7.0f), 0.0f),
			(new Vector3(0.0f, 3.12f, -7.0f), 0.0f),
			(new Vector3(-8.0f, 3.12f, 8.0f), 90.0f),
			(new Vector3(-8.0f, 3.12f, -8.0f), 90.0f),
			(new Vector3(8.0f, 3.12f, -8.0f), 90.0f),
			(new Vector3(0.0f, 6.48f, 7.0f), 0.0f),
			(new Vector3(0.0f, 6.48f, -7.0f), 0.0f),
			(new Vector3(-8.0f, 6.48f, 8.0f), 90.0f),
			(new Vector3(-8.0f, 6.48f, -8.0f), 90.0f),
			(new Vector3(8.0f, 6.48f, 8.0f), 90.0f),
			(new Vector3(8.0f, 6.48f, -8.0f), 90.0f),
		})
		{
			AddProp(
				parent,
				$"{PharmacyPropRoot}mounted_fluorescent_lights/mounted_fluorescent_lights_1k.gltf",
				$"Fluorescent_{fixture.position.X:0}_{fixture.position.Y:0.0}_{fixture.position.Z:0}",
				fixture.position,
				new Vector3(0.0f, fixture.yaw, 0.0f),
				new Vector3(0.78f, 0.78f, 0.78f));
		}
	}

	private void AddRestroomFixtures(
		Node3D parent,
		string prefix,
		float z)
	{
		const string ToiletPath =
			"res://assets/third_party/interiors/shared/open_game_art/loafbrr_toilets/objects/toilet_round_a.tscn";
		const string SinkPath =
			"res://assets/third_party/interiors/shared/open_game_art/loafbrr_toilets/objects/sink_a.tscn";

		AddProp(
			parent,
			ToiletPath,
			$"{prefix}RestroomToilet",
			new Vector3(12.35f, GroundFloorTop, z),
			new Vector3(0.0f, -90.0f, 0.0f));
		AddProp(
			parent,
			SinkPath,
			$"{prefix}RestroomSink",
			new Vector3(8.25f, GroundFloorTop + 0.68f, z),
			new Vector3(0.0f, 90.0f, 0.0f));
		AddProp(
			parent,
			$"{UserPropRoot}trash_can.tscn",
			$"{prefix}RestroomTrash",
			new Vector3(9.15f, GroundFloorTop, z + 0.85f),
			new Vector3(0.0f, 15.0f, 0.0f),
			new Vector3(0.5f, 0.5f, 0.5f));
	}

	private void AddSchoolChair(
		Node3D parent,
		string name,
		Vector3 position,
		float yaw)
	{
		AddProp(
			parent,
			$"{PharmacyPropRoot}SchoolChair_01/SchoolChair_01_1k.gltf",
			name,
			position,
			new Vector3(0.0f, yaw, 0.0f),
			new Vector3(0.86f, 0.86f, 0.86f));
	}

	private void AddFrontWindows(Node3D parent)
	{
		foreach (float y in new[] { 0.6f, 3.96f })
		{
			float[] xValues = y < 1.0f
				? new[] { -9.15f, 9.15f }
				: new[] { -9.1f, -3.1f, 3.1f, 9.1f };
			foreach (float x in xValues)
			{
				AddProp(
					parent,
					$"{UserPropRoot}double_window.tscn",
					$"FrontWindow_{x:0.0}_{y:0.0}",
					new Vector3(x, y, 12.82f),
					Vector3.Zero);
			}
		}
	}

	private void AddRearWindows(Node3D parent)
	{
		foreach (float y in new[] { 0.6f, 3.96f })
		{
			foreach (float x in new[] { -8.9f, -3.0f, 3.0f, 8.9f })
			{
				AddProp(
					parent,
					$"{UserPropRoot}double_window.tscn",
					$"RearWindow_{x:0.0}_{y:0.0}",
					new Vector3(x, y, -12.82f),
					new Vector3(0.0f, 180.0f, 0.0f));
			}
		}
	}

	private void AddSideWindows(Node3D parent)
	{
		foreach (float y in new[] { 0.6f, 3.96f })
		{
			foreach (float z in new[] { -8.4f, -2.2f, 4.2f, 9.3f })
			{
				AddProp(
					parent,
					$"{UserPropRoot}double_window.tscn",
					$"WestWindow_{z:0.0}_{y:0.0}",
					new Vector3(-13.82f, y, z),
					new Vector3(0.0f, -90.0f, 0.0f));
				if (y > 1.0f || !Mathf.IsEqualApprox(z, -8.4f))
				{
					AddProp(
						parent,
						$"{UserPropRoot}double_window.tscn",
						$"EastWindow_{z:0.0}_{y:0.0}",
						new Vector3(13.82f, y, z),
						new Vector3(0.0f, 90.0f, 0.0f));
				}
			}
		}
	}

	private static Opening[] DoorOpenings(params float[] centres)
	{
		return centres
			.Select(centre => new Opening(centre, 1.08f, 0.0f, 2.22f))
			.ToArray();
	}

	private void AddWallXWithOpenings(
		Node3D parent,
		string name,
		float baseY,
		float z,
		float minimumX,
		float maximumX,
		float thickness,
		Material material,
		Opening[]? openings = null,
		bool collision = true,
		float wallHeight = WallHeight)
	{
		Node3D wall = AddNode<Node3D>(parent, name);
		Opening[] ordered = (openings ?? Array.Empty<Opening>())
			.OrderBy(opening => opening.Centre)
			.ToArray();
		float cursor = minimumX;

		for (int index = 0; index < ordered.Length; index++)
		{
			Opening opening = ordered[index];
			float openingMin = opening.Centre - (opening.Width * 0.5f);
			float openingMax = opening.Centre + (opening.Width * 0.5f);
			if (openingMin > cursor)
			{
				AddBox(
					wall,
					$"Pier_{index}",
					new Vector3(
						(cursor + openingMin) * 0.5f,
						baseY + (wallHeight * 0.5f),
						z),
					new Vector3(
						openingMin - cursor,
						wallHeight,
						thickness),
					material,
					collision);
			}

			if (opening.Bottom > 0.001f)
			{
				AddBox(
					wall,
					$"Sill_{index}",
					new Vector3(
						opening.Centre,
						baseY + (opening.Bottom * 0.5f),
						z),
					new Vector3(opening.Width, opening.Bottom, thickness),
					material,
					collision);
			}

			if (opening.Top < wallHeight)
			{
				float headerHeight = wallHeight - opening.Top;
				AddBox(
					wall,
					$"Header_{index}",
					new Vector3(
						opening.Centre,
						baseY + opening.Top + (headerHeight * 0.5f),
						z),
					new Vector3(opening.Width, headerHeight, thickness),
					material,
					collision);
			}

			cursor = openingMax;
		}

		if (cursor < maximumX)
		{
			AddBox(
				wall,
				"PierEnd",
				new Vector3(
					(cursor + maximumX) * 0.5f,
					baseY + (wallHeight * 0.5f),
					z),
				new Vector3(maximumX - cursor, wallHeight, thickness),
				material,
				collision);
		}
	}

	private void AddWallZWithOpenings(
		Node3D parent,
		string name,
		float baseY,
		float x,
		float minimumZ,
		float maximumZ,
		float thickness,
		Material material,
		Opening[]? openings = null,
		bool collision = true,
		float wallHeight = WallHeight)
	{
		Node3D wall = AddNode<Node3D>(parent, name);
		Opening[] ordered = (openings ?? Array.Empty<Opening>())
			.OrderBy(opening => opening.Centre)
			.ToArray();
		float cursor = minimumZ;

		for (int index = 0; index < ordered.Length; index++)
		{
			Opening opening = ordered[index];
			float openingMin = opening.Centre - (opening.Width * 0.5f);
			float openingMax = opening.Centre + (opening.Width * 0.5f);
			if (openingMin > cursor)
			{
				AddBox(
					wall,
					$"Pier_{index}",
					new Vector3(
						x,
						baseY + (wallHeight * 0.5f),
						(cursor + openingMin) * 0.5f),
					new Vector3(
						thickness,
						wallHeight,
						openingMin - cursor),
					material,
					collision);
			}

			if (opening.Bottom > 0.001f)
			{
				AddBox(
					wall,
					$"Sill_{index}",
					new Vector3(
						x,
						baseY + (opening.Bottom * 0.5f),
						opening.Centre),
					new Vector3(thickness, opening.Bottom, opening.Width),
					material,
					collision);
			}

			if (opening.Top < wallHeight)
			{
				float headerHeight = wallHeight - opening.Top;
				AddBox(
					wall,
					$"Header_{index}",
					new Vector3(
						x,
						baseY + opening.Top + (headerHeight * 0.5f),
						opening.Centre),
					new Vector3(thickness, headerHeight, opening.Width),
					material,
					collision);
			}

			cursor = openingMax;
		}

		if (cursor < maximumZ)
		{
			AddBox(
				wall,
				"PierEnd",
				new Vector3(
					x,
					baseY + (wallHeight * 0.5f),
					(cursor + maximumZ) * 0.5f),
				new Vector3(thickness, wallHeight, maximumZ - cursor),
				material,
				collision);
		}
	}

	private StaticBody3D AddBox(
		Node3D parent,
		string name,
		Vector3 position,
		Vector3 size,
		Material material,
		bool collision = true,
		Vector3? rotationDegrees = null)
	{
		StaticBody3D body = AddNode<StaticBody3D>(parent, name);
		body.Position = position;
		body.RotationDegrees = rotationDegrees ?? Vector3.Zero;

		MeshInstance3D mesh = AddNode<MeshInstance3D>(body, "Mesh");
		mesh.Mesh = new BoxMesh
		{
			Size = size,
			Material = material,
		};

		if (collision)
		{
			CollisionShape3D collisionShape =
				AddNode<CollisionShape3D>(body, "Collision");
			collisionShape.Shape = new BoxShape3D
			{
				Size = size,
			};
		}

		return body;
	}

	private Node3D AddProp(
		Node3D parent,
		string scenePath,
		string name,
		Vector3 position,
		Vector3 rotationDegrees,
		Vector3? scale = null)
	{
		if (!_sceneCache.TryGetValue(scenePath, out PackedScene? scene))
		{
			scene = LoadRequired<PackedScene>(scenePath);
			_sceneCache[scenePath] = scene;
		}

		Node3D instance = scene.Instantiate<Node3D>();
		instance.Name = name;
		ApplyPropVisibilityRange(
			instance,
			HasNamedAncestor(parent, "ExteriorIdentity") ||
				HasNamedAncestor(parent, "ExternalWindows") ||
				HasNamedAncestor(parent, "GymWindows") ||
				name == "LockedGymEmergencyExit"
				? ExteriorPropVisibilityRange
				: InteriorPropVisibilityRange);
		parent.AddChild(instance);
		instance.Position = position;
		instance.RotationDegrees = rotationDegrees;
		instance.Scale = scale ?? Vector3.One;
		return instance;
	}

	private static bool HasNamedAncestor(Node node, string ancestorName)
	{
		for (Node? current = node; current is not null; current = current.GetParent())
		{
			if (current.Name == ancestorName)
			{
				return true;
			}
		}

		return false;
	}

	private static void ApplyPropVisibilityRange(Node node, float range)
	{
		if (node is GeometryInstance3D geometry)
		{
			geometry.VisibilityRangeEnd = range;
			geometry.VisibilityRangeEndMargin = PropVisibilityFadeMargin;
		}

		for (int index = 0; index < node.GetChildCount(); index++)
		{
			ApplyPropVisibilityRange(node.GetChild(index), range);
		}
	}

	private static T AddNode<T>(Node parent, string name)
		where T : Node, new()
	{
		T node = new()
		{
			Name = name,
		};
		parent.AddChild(node);
		return node;
	}

	private static T LoadRequired<T>(string path)
		where T : GodotObject
	{
		return GD.Load<T>(path) ??
			throw new InvalidOperationException(
				$"Required school resource could not be loaded: {path}");
	}

	private readonly record struct Opening(
		float Centre,
		float Width,
		float Bottom,
		float Top);
}
