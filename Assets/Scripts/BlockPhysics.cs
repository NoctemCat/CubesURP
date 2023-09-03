
using UnityEngine;

public class BlockPhysics : MonoBehaviour
{
    private World World;

    private float _width;
    private float _height;
    private float _length;

    public bool Front { get; private set; }
    public bool Back { get; private set; }
    public bool Left { get; private set; }
    public bool Right { get; private set; }
    public bool FrontLeft { get; private set; }
    public bool FrontRight { get; private set; }
    public bool BackLeft { get; private set; }
    public bool BackRight { get; private set; }

    private Vector3 _velocity;
    private float _verticalMomentum;
    private float _gravity = -15f;
    private float _drag = 4f;

    public bool ApplyGravity;

    private void Start()
    {
        World = World.Instance;
        Reset();
    }

    public void Reset()
    {
        _width = transform.localScale.x / 2f;
        _height = transform.localScale.y / 2f;
        _length = transform.localScale.z / 2f;

        _velocity = Vector3.zero;
        _verticalMomentum = 0f;
    }

    private void FixedUpdate()
    {
        Vector3 pos = transform.position;
        UpdateDirections(pos);
        CalculateVelocity(pos);
        transform.Translate(_velocity, Space.World);
    }

    public void UpdateDirections(Vector3 pos)
    {
        Front = World.CheckForVoxel(new(pos.x, pos.y - _height, pos.z + _length));
        Back = World.CheckForVoxel(new(pos.x, pos.y - _height, pos.z - _length));
        Left = World.CheckForVoxel(new(pos.x - _width, pos.y - _height, pos.z));
        Right = World.CheckForVoxel(new(pos.x + _width, pos.y - _height, pos.z));
        FrontLeft = World.CheckForVoxel(new(pos.x - _width, pos.y - _height, pos.z + _length));
        FrontRight = World.CheckForVoxel(new(pos.x + _width, pos.y - _height, pos.z + _length));
        BackLeft = World.CheckForVoxel(new(pos.x - _width, pos.y - _height, pos.z - _length));
        BackRight = World.CheckForVoxel(new(pos.x + _width, pos.y - _height, pos.z - _length));
    }

    private void CalculateVelocity(Vector3 pos)
    {
        if (ApplyGravity && _verticalMomentum > _gravity)
            _verticalMomentum += Time.fixedDeltaTime * _gravity;

        _velocity += Time.fixedDeltaTime * _verticalMomentum * Vector3.up;

        _velocity *= 1 - Time.fixedDeltaTime * _drag;

        if ((_velocity.z > 0f && Front) || (_velocity.z < 0f && Back))
            _velocity.z *= -1;
        if ((_velocity.x > 0f && Right) || (_velocity.x < 0f && Left))
            _velocity.x *= -1f;

        if (_velocity.y < 0f)
            _velocity.y = CheckDownSpeed(pos, _velocity.y);
        else if (_velocity.y > 0f)
            _velocity.y = CheckDownSpeed(pos, _velocity.y);
    }

    public void AddVelocity(Vector3 velocity)
    {
        _velocity += velocity;
    }

    private float CheckDownSpeed(Vector3 pos, float downSpeed)
    {
        if (
            (World.CheckForVoxel(new(pos.x - _width, pos.y - _height + downSpeed, pos.z - _length)) && !BackLeft) ||
            (World.CheckForVoxel(new(pos.x + _width, pos.y - _height + downSpeed, pos.z - _length)) && !BackRight) ||
            (World.CheckForVoxel(new(pos.x + _width, pos.y - _height + downSpeed, pos.z + _length)) && !FrontRight) ||
            (World.CheckForVoxel(new(pos.x - _width, pos.y - _height + downSpeed, pos.z + _length)) && !FrontLeft)
        )
        {
            _verticalMomentum = -(downSpeed + _verticalMomentum) * 0.46f;
            return 0;
        }
        else
        {
            return downSpeed;
        }
    }
}