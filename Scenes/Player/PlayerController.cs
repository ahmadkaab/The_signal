using Godot;
using System.Collections.Generic;
using TheSignal.Core;
using TheSignal.Core.Stats;
using TheSignal.Core.Progression;
using TheSignal.Data;
using TheSignal.Systems;
using PlayerType = TheSignal.Core.Progression.Player;

namespace TheSignal.Scenes.Player;

public partial class PlayerController : CharacterBody3D
{
    [Export] public float WalkSpeed = 4.0f;
    [Export] public float SprintSpeed = 7.0f;
    [Export] public float JumpVelocity = 5.0f;
    [Export] public float Gravity = 9.8f;
    [Export] public float MouseSensitivity = 0.002f;
    [Export] public float CameraDistance = 4.0f;
    [Export] public float CameraHeight = 1.5f;

    // Components
    private Node3D _cameraPivot;
    private Camera3D _camera;
    private AnimationPlayer _animationPlayer;
    private MeshInstance3D _bodyMesh;
    private RayCast3D _groundRay;

    // State
    private Vector2 _inputDirection = Vector2.Zero;
    private bool _isSprinting = false;
    private bool _isGrounded = false;
    private Vector3 _velocity = Vector3.Zero;

    // Player data reference
    private TheSignal.Core.Progression.Player _playerData;

    public override void _Ready()
    {
        _cameraPivot = GetNode<Node3D>("CameraPivot");
        _camera = GetNode<Camera3D>("CameraPivot/Camera3D");
        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        _bodyMesh = GetNode<MeshInstance3D>("BodyMesh");
        _groundRay = GetNode<RayCast3D>("GroundRay");

        Input.MouseMode = Input.MouseModeEnum.Captured;

        // Make camera current
        _camera.MakeCurrent();
    }

    public void Initialize(PlayerType playerData)
    {
        _playerData = playerData;
        
        // Apply stats to movement
        var derived = GameManager.Instance.ProgressionFormulas.GetDerivedStats(_playerData.BaseStats, _playerData.Level);
        WalkSpeed = 4.0f + derived.Agility * 0.1f;
        SprintSpeed = WalkSpeed * 1.5f;
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        HandleInput();
        HandleMovement(dt);
        HandleCamera(dt);
        UpdateAnimations();

        MoveAndSlide();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion)
        {
            RotateY(-mouseMotion.Relative.X * MouseSensitivity);
            _cameraPivot.RotateX(-mouseMotion.Relative.Y * MouseSensitivity);
            _cameraPivot.RotationDegrees = new Vector3(
                Mathf.Clamp(_cameraPivot.RotationDegrees.X, -80, 80),
                _cameraPivot.RotationDegrees.Y,
                0
            );
        }

        if (@event.IsActionPressed("explore_interact"))
        {
            TryInteract();
        }

        if (@event.IsActionPressed("explore_dodge"))
        {
            Dodge();
        }
    }

    private void HandleInput()
    {
        _inputDirection = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        _isSprinting = Input.IsActionPressed("sprint");

        if (Input.IsActionJustPressed("jump") && _isGrounded)
        {
            _velocity.Y = JumpVelocity;
        }
    }

    private void HandleMovement(float delta)
    {
        _isGrounded = IsOnFloor() || (_groundRay != null && _groundRay.IsColliding());

        if (!_isGrounded)
        {
            _velocity.Y -= Gravity * delta;
        }
        else if (_velocity.Y < 0)
        {
            _velocity.Y = 0;
        }

        Vector3 direction = (Transform.Basis * new Vector3(_inputDirection.X, 0, _inputDirection.Y)).Normalized();
        
        float targetSpeed = _isSprinting ? SprintSpeed : WalkSpeed;
        
        if (direction != Vector3.Zero)
        {
            _velocity.X = Mathf.MoveToward(_velocity.X, direction.X * targetSpeed, targetSpeed * 10f * delta);
            _velocity.Z = Mathf.MoveToward(_velocity.Z, direction.Z * targetSpeed, targetSpeed * 10f * delta);
        }
        else
        {
            _velocity.X = Mathf.MoveToward(_velocity.X, 0, targetSpeed * 10f * delta);
            _velocity.Z = Mathf.MoveToward(_velocity.Z, 0, targetSpeed * 10f * delta);
        }

        Velocity = _velocity;
    }

    private void HandleCamera(float delta)
    {
        // Smooth camera follow
        if (_cameraPivot != null)
        {
            _cameraPivot.GlobalPosition = GlobalPosition + Vector3.Up * CameraHeight;
        }
    }

    private void UpdateAnimations()
    {
        if (_animationPlayer == null) return;

        float speed = new Vector2(Velocity.X, Velocity.Z).Length();
        
        if (!_isGrounded)
        {
            _animationPlayer.Play("jump");
        }
        else if (speed > SprintSpeed * 0.5f)
        {
            _animationPlayer.Play("run");
            _animationPlayer.PlaybackSpeed = speed / SprintSpeed;
        }
        else if (speed > 0.1f)
        {
            _animationPlayer.Play("walk");
            _animationPlayer.PlaybackSpeed = speed / WalkSpeed;
        }
        else
        {
            _animationPlayer.Play("idle");
        }
    }

    private void TryInteract()
    {
        // Raycast forward for interactables
        var spaceState = GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(
            GlobalPosition + Vector3.Up * 1.5f,
            GlobalPosition + Vector3.Up * 1.5f + Transform.Basis.Z * -3f
        );
        query.CollisionMask = 1 << 6; // Interactable layer
        query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };

        var result = spaceState.IntersectRay(query);
        if (result.Count > 0)
        {
            var collider = result["collider"].AsGodotObject();
            if (collider is IInteractable interactable)
            {
                interactable.Interact(this);
            }
        }
    }

    private void Dodge()
    {
        if (_inputDirection == Vector2.Zero) return;

        var direction = (Transform.Basis * new Vector3(_inputDirection.X, 0, _inputDirection.Y)).Normalized();
        _velocity += direction * 10f;
        _animationPlayer.Play("dodge");
    }

    public void AddItem(string itemId, int count = 1)
    {
        _playerData?.AddItem(itemId, count);
        GameManager.Instance.UIManager.ShowLootNotification(itemId, count);
    }

    public void TakeDamage(int amount, DamageType type = DamageType.Physical)
    {
        // Apply to player data
        _playerData?.TakeDamage(amount, type);
        
        // Visual feedback
        _animationPlayer.Play("hit");
        
        // Screen shake
        GetViewport().ShakeCamera(0.3f, 0.5f);
    }

    public void Heal(int amount)
    {
        _playerData?.Heal(amount);
    }
}

public interface IInteractable
{
    void Interact(PlayerController player);
}