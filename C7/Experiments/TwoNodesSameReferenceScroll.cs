using Godot;
using System;

public class TwoNodesSameReferenceScroll : Node2D
{
    public Node2D OriginalNode;
    public Node2D CopyNode;
    public int ImageWidth = 320;
    public override void _Ready()
    {
        OriginalNode = GetNode<Node2D>("OriginalNode");
        // I believe all the childern will be references and not duplicated
        CopyNode = OriginalNode.Duplicate() as Node2D;
        AddChild(CopyNode);
        CopyNode.Position = new Vector2(ImageWidth * 2, CopyNode.Position.y);
        OriginalNode.Position = new Vector2(ImageWidth, OriginalNode.Position.y);

        // Rotate the original icon; if the CopyNode shows the same rotation then it's the same object
        GetNode<Sprite>("OriginalNode/icon").RotationDegrees = 90;
    }
    public override void _Draw()
    {
        DrawRect(
            new Rect2(
                new Vector2(ImageWidth, 0),
                new Vector2(ImageWidth, ImageWidth)
            ),
            new Color(1, 0, 0, 1),
            false,
            5
        );
        base._Draw();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
   

 // Called every frame. 'delta' is the elapsed time since the previous frame.
 public override void _Process(float delta)
 {
     OriginalNode.Position += new Vector2(-1, 0);
     if(OriginalNode.Position.x < 0)
        OriginalNode.Position = new Vector2(ImageWidth * 2, 0);
     CopyNode.Position += new Vector2(-1, 0);
     if(CopyNode.Position.x < 0)
        CopyNode.Position = new Vector2(ImageWidth * 2, 0);
 }
}
