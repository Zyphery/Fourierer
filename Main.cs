using Godot;
using System.Collections.Generic;

public class FourierStep
{
	public double length = 1.0;
	public double offset = 0.0;
	public double speed = 1.0;

	public Color color;
	public Node UInodeRef;
	public Node VisualnodeRef;

	public FourierStep() {}
}

public partial class Main : Node
{
	[Export] private double Time;
	[Export] private double Speed = 1.0;

	// UI

	[Export] private Control UI_ItemContainer;
	[Export] private PackedScene UI_ItemScene;

	[Export] private Label UI_Time;
	[Export] private Label UI_Speed;

	// Visual

	[Export] private Line2D VS_Tracer;
	[Export] private Node2D VS_ObjectContainer;
	[Export] private PackedScene VS_ObjectScene;

	private List<FourierStep> FourierSteps = new List<FourierStep>();

	private bool trace_ClearOnChange = false;
	private bool hidden_Trace = false;
	private bool hidden_Circles = true;
	private bool hidden_Lines = true;
	private float line_Width = 1.0f;

	private int sim_Iterations = 64;

	private void AddNewItem()
	{
		var newStep = new FourierStep();
		newStep.length = 100.0;
		newStep.speed = 1.0;

		var newItem = UI_ItemScene.Instantiate();
		UI_ItemContainer.AddChild(newItem);

		var newItemVisual = VS_ObjectScene.Instantiate();
		VS_ObjectContainer.AddChild(newItemVisual);

		newStep.UInodeRef = newItem;
		newStep.VisualnodeRef = newItemVisual;
		FourierSteps.Add(newStep);
		
		(newItem.GetNode("CenterContainer/Label") as Label).Text = $"Step {FourierSteps.Count}";

		(newItem.GetNode("Margin/PropertiesContainer/LengthContainer/Length") as SpinBox).ValueChanged += (double value) => { UpdateItem(newStep); };
		(newItem.GetNode("Margin/PropertiesContainer/OffsetContainer/Offset") as SpinBox).ValueChanged += (double value) => { UpdateItem(newStep); };
		(newItem.GetNode("Margin/PropertiesContainer/SpeedContainer/Speed") as SpinBox).ValueChanged += (double value) => { UpdateItem(newStep); };
		(newItem.GetNode("Margin/PropertiesContainer/Color") as ColorPickerButton).ColorChanged += (Color color) => { UpdateItem(newStep); };

		UpdateItem(newStep);
	}

	private void UpdateVisual(FourierStep step)
	{
		Line2D directionLine = step.VisualnodeRef.GetNode("Direction") as Line2D;
		directionLine.Width = line_Width;
		directionLine.Visible = hidden_Lines;
		directionLine.DefaultColor = step.color;

		directionLine.SetPointPosition(1, new Vector2((float)step.length, 0.0f));


		Line2D lengthLine = (step.VisualnodeRef.GetNode("Length") as Line2D);
		lengthLine.Width = line_Width;
		lengthLine.Visible = hidden_Circles;
		lengthLine.DefaultColor = step.color;
		
		lengthLine.ClearPoints();

		for(int i = 0; i < 32; i++)
		{
			float angle = ((float)i / 32.0f) * Mathf.Tau;

			lengthLine.AddPoint(Vector2.FromAngle(angle) * (float)step.length);
		}

		lengthLine.AddPoint(Vector2.FromAngle(0.0f) * (float)step.length);


		(step.VisualnodeRef as Node2D).Rotation = (float)step.offset;

		if(trace_ClearOnChange)
			VS_Tracer.ClearPoints();
	}

	private void UpdateItem(FourierStep step)
	{
		var properties = step.UInodeRef.GetNode("Margin/PropertiesContainer");

		double length = (properties.GetNode("LengthContainer/Length") as SpinBox).Value;
		double offset = (properties.GetNode("OffsetContainer/Offset") as SpinBox).Value;
		double speed = (properties.GetNode("SpeedContainer/Speed") as SpinBox).Value;
		Color color = (properties.GetNode("Color") as ColorPickerButton).Color;

		step.length = length;
		step.offset = offset;
		step.speed = speed;
		step.color = color;

		UpdateVisual(step);
	}

	private void HideCircles(bool hidden)
	{
		hidden_Circles = hidden;

		foreach (var step in FourierSteps)
		{
			(step.VisualnodeRef.GetNode("Length") as Node2D).Visible = hidden;
		}
	}

	private void HideLines(bool hidden)
	{
		hidden_Lines = hidden;

		foreach (var step in FourierSteps)
		{
			(step.VisualnodeRef.GetNode("Direction") as Node2D).Visible = hidden;
		}
	}

	private void ChangeThickness(float width)
	{
		line_Width = width;

		foreach (var step in FourierSteps)
		{
			(step.VisualnodeRef.GetNode("Direction") as Line2D).Width = width;
			(step.VisualnodeRef.GetNode("Length") as Line2D).Width = width;
		}
	}

	private void RemoveItem()
	{
		if(FourierSteps.Count > 1)
		{
			var last = FourierSteps[FourierSteps.Count - 1];
			
			last.UInodeRef.QueueFree();
			last.VisualnodeRef.QueueFree();
			FourierSteps.Remove(last);
		}
	}

	private void Update(double delta)
	{
		Time += delta * Speed;
		UI_Time.Text = $"Time: {Time.ToString("0.000")}";
		
		if(FourierSteps.Count == 0)
			return;

		double currentAngle = 0.0;
		Vector2 currentOffset = Vector2.Zero;

		Node2D start = FourierSteps[0].VisualnodeRef as Node2D;
		start.Position = Vector2.Zero;
		currentAngle = FourierSteps[0].speed * Time + FourierSteps[0].offset;
		start.Rotation = (float)currentAngle;

		currentOffset = start.Position + Vector2.FromAngle(start.Rotation) * (float)FourierSteps[0].length;
		
		for(int i = 1; i < FourierSteps.Count; i++)
		{
			Node2D step = FourierSteps[i].VisualnodeRef as Node2D;

			currentAngle += FourierSteps[i].speed * Time + FourierSteps[i].offset;

			step.Position = currentOffset;
			step.Rotation = (float)currentAngle;

			currentOffset = step.Position + Vector2.FromAngle(step.Rotation) * (float)FourierSteps[i].length;
		}

		Vector2 finalPosition = currentOffset;

		if(hidden_Trace)
			VS_Tracer.AddPoint(finalPosition);
	}

	public override void _Ready()
	{
		AddNewItem();
	}

	public override void _Process(double delta)
	{
		double deltaStep = (1.0 / (double)sim_Iterations) * delta;

		for(int i = 0; i < sim_Iterations; i++)
		{
			Update(deltaStep);
		}
	}



	private void _on_NewItem_pressed()
	{
		AddNewItem();
	}

	private void _on_RemoveItem_pressed()
	{
		RemoveItem();
	}

	private void _on_thickness_value_changed(float value)
	{
		ChangeThickness(value);
	}

	private void _on_circles_toggled(bool button_pressed)
	{
		HideCircles(button_pressed);
	}

	private void _on_lines_toggled(bool button_pressed)
	{
		HideLines(button_pressed);
	}

	private void _on_trace_toggled(bool button_pressed)
	{
		hidden_Trace = button_pressed;
		VS_Tracer.ClearPoints();
	}

	private void _on_iterations_value_changed(float value)
	{
		sim_Iterations = (int)value;
	}

	private void _on_time_value_changed(float value)
	{
		Time = (double)value;
	}

	private void _on_speed_value_changed(float value)
	{
		Speed = (double)value;
		UI_Speed.Text = $"Speed: {Speed.ToString("0.00")}x";
	}

	private void _on_trace_color_color_changed(Color color)
	{
		VS_Tracer.DefaultColor = color;
	}
}
