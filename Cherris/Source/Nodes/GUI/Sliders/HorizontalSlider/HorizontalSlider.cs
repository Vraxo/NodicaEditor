﻿using Raylib_cs;

namespace Cherris;

public partial class HorizontalSlider : BaseSlider
{
    public HorizontalSlider()
    {
        Size = new(100, 10);
        OriginPreset = OriginPreset.CenterLeft;
    }

    protected override void UpdatePercentage()
    {
        if (Raylib.IsWindowMinimized())
        {
            return;
        }

        float currentPosition = Grabber.GlobalPosition.X;
        float minPos = GlobalPosition.X;
        float maxPos = minPos + Size.X;

        Percentage = Math.Clamp((currentPosition - minPos) / (maxPos - minPos), 0, 1);
    }

    public override void MoveGrabber(int direction)
    {
        if (MaxExternalValue == 0)
        {
            return;
        }

        float unit = Size.X / MaxExternalValue;
        float x = Grabber.GlobalPosition.X + direction * unit;
        float y = Grabber.GlobalPosition.Y;

        Grabber.GlobalPosition = new(x, y);

        UpdatePercentageBasedOnGrabber();
    }

    //public override void MoveGrabber(float)
    //{
    //    if (MaxExternalValue == 0)
    //    {
    //        return;
    //    }
    //
    //    float unit = Dimensions.X / MaxExternalValue;
    //    float x = Grabber.GlobalPosition.X + direction * unit;
    //    float y = Grabber.GlobalPosition.Y;
    //
    //    Grabber.GlobalPosition = new(x, y);
    //
    //    UpdatePercentageBasedOnGrabber();
    //}

    protected override void MoveGrabberToPercentage(float percentage)
    {
        float x = GlobalPosition.X + percentage * Size.X;
        float y = GlobalPosition.Y;

        Grabber.GlobalPosition = new(x, y);
    }

    protected override void HandleClicks()
    {
        if (IsMouseOver())
        {
            if (Input.IsMouseButtonPressed(MouseButtonCode.Left) && OnTopLeft)
            {
                float x = Raylib.GetMousePosition().X;
                float y = Grabber.GlobalPosition.Y;

                Grabber.GlobalPosition = new(x, y);
                Grabber.Pressed = true;
            }

            if (Input.IsMouseButtonPressed(MouseButtonCode.Right) && OnTopRight)
            {
                if (ResetOnRitghtClick)
                {
                    RevertToDefaultPercentage();
                    OnPercentageChanged();
                    OnReleased();
                }
            }
        }
    }

    protected override void Draw()
    {
        DrawRectangleThemed(
            GlobalPosition - Offset,
            Size,
            EmptyStyle);

        Vector2 filledSize = new(Size.X * Percentage, Size.Y);

        DrawRectangleThemed(
            GlobalPosition - Offset,
            filledSize,
            FilledStyle);
    }
}