using Godot;
using System;
using System.Xml.Linq;

public partial class MapCollision : Node2D
{
	[Export]
	public string TmxFilePath = "res://Assets/Maps/map.tmx";

	public override void _Ready()
	{
		GenerateWallCollisions();
	}

	private void GenerateWallCollisions()
	{
		if (!FileAccess.FileExists(TmxFilePath))
		{
			GD.PrintErr($"TMX file not found: {TmxFilePath}");
			return;
		}

		using FileAccess file = FileAccess.Open(
			TmxFilePath,
			FileAccess.ModeFlags.Read
		);

		string xmlText = file.GetAsText();

		XDocument document = XDocument.Parse(xmlText);

		XElement? obstaclesLayer = null;

		foreach (XElement layer in document.Root!.Elements("objectgroup"))
		{
			if ((string?)layer.Attribute("name") == "Obstacles")
			{
				obstaclesLayer = layer;
				break;
			}
		}

		if (obstaclesLayer == null)
		{
			GD.PrintErr("Obstacles object layer not found in TMX.");
			return;
		}

		Node2D wallsContainer = new Node2D
		{
			Name = "Walls"
		};

		AddChild(wallsContainer);

		int wallCount = 0;

		foreach (XElement objectElement in obstaclesLayer.Elements("object"))
		{
			string objectName =
				(string?)objectElement.Attribute("name") ?? "";

			if (objectName != "walls")
				continue;

			float x = ParseFloat(objectElement.Attribute("x"));
			float y = ParseFloat(objectElement.Attribute("y"));
			float width = ParseFloat(objectElement.Attribute("width"));
			float height = ParseFloat(objectElement.Attribute("height"));

			StaticBody2D wall = new StaticBody2D
			{
				Name = $"Wall_{wallCount}"
			};

			CollisionShape2D collision = new CollisionShape2D();

			RectangleShape2D rectangle = new RectangleShape2D
			{
				Size = new Vector2(width, height)
			};

			collision.Shape = rectangle;

			// Tiled rectangles use their top-left corner.
			// Godot CollisionShape2D uses its center.
			wall.Position = new Vector2(
				x + width / 2f,
				y + height / 2f
			);

			wall.AddChild(collision);
			wallsContainer.AddChild(wall);

			wallCount++;
		}

		GD.Print($"Generated {wallCount} wall colliders.");
	}

	private float ParseFloat(XAttribute? attribute)
	{
		if (attribute == null)
			return 0f;

		return float.Parse(
			attribute.Value,
			System.Globalization.CultureInfo.InvariantCulture
		);
	}
}
