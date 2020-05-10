using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;

public class Character : Node2D
{
    enum States {
        Idle, Follow
    }

    [Export]
    private float speed = 200;

    private States? state;
    private List<Vector2> path;
    private Vector2 targetPointWorld;
    private Vector2 targetPosition;
    private Vector2 velocity;

    public override void _Ready() {
        ChangeState(States.Idle);
    }

    public override void _Process(float delta) {
        if (state != States.Follow) return;
        var arrivedToNextPoint = MoveTo(targetPointWorld);
        if (arrivedToNextPoint) {
            path.RemoveAt(0);
            if (path.Count == 0) {
                ChangeState(States.Idle);
                return;
            }

            targetPointWorld = path[0];
        }
    }

    public override void _Input(InputEvent @event) {
        if (@event.IsActionPressed("click")) {
            var globalMousePos = GetGlobalMousePosition();
            if (Input.IsKeyPressed((int) KeyList.Shift)) {
                GlobalPosition = globalMousePos;
            } else {
                targetPosition = globalMousePos;
            }
            ChangeState(States.Follow);
        }
    }

    private bool MoveTo(Vector2 worldPosition) {
        var mass = 10.0f;
        var arrive_distance = 10.0;

        var desiredVelocity = (worldPosition - Position).Normalized() * speed;
        var steering = desiredVelocity - velocity;
        velocity += steering / mass;
        Position += velocity * GetProcessDeltaTime();
        Rotation = velocity.Angle();
        return Position.DistanceTo(worldPosition) < arrive_distance;
    }

    private void ChangeState(States newState) {
        if (newState == States.Follow) {
            path = GetParent().GetNode<PathfindAstar>("TileMapCs").GetAStarPath(Position, targetPosition);
            if (path == null || path.Count < 2) {
                ChangeState(States.Idle);
                return;
            }

            targetPointWorld = path[1];
        }

        this.state = newState;
    }
}
