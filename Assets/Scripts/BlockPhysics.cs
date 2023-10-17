
using UnityEngine;

public class BlockPhysics : MonoBehaviour
{
    private World _world;

    private float _width;
    private float _height;
    private float _length;

    public bool Center { get; private set; }
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

    private Transform _curTansform;
    private BoxCollider _collider;

    private void Awake()
    {
        _world = ServiceLocator.Get<World>();
        _curTansform = transform;
        _collider = GetComponent<BoxCollider>();
        //Reset();
    }

    //private void OnDisable()
    //{
    //    _velocity = Vector3.zero;
    //    _verticalMomentum = 0f;
    //}

    public void Reset()
    {
        Vector3 size = _collider.size;
        Vector3 center = _collider.center;
        _width = transform.localScale.x * size.x / 2f;
        _height = (transform.localScale.y * size.y - center.y) / 2f;
        _length = transform.localScale.z * size.z / 2f;
        _velocity = Vector3.zero;
        _verticalMomentum = 0f;
    }

    private void FixedUpdate()
    {
        //_curTansform.GetPositionAndRotation()
        Vector3 pos = _curTansform.position;
        UpdateDirections(pos);
        CalculateVelocity(pos);
        transform.Translate(_velocity, Space.World);
    }

    public void UpdateDirections(Vector3 pos)
    {
        Center = _world.CheckForVoxel(new(pos.x, pos.y - _height, pos.z));
        Front = _world.CheckForVoxel(new(pos.x, pos.y - _height, pos.z + _length));
        Back = _world.CheckForVoxel(new(pos.x, pos.y - _height, pos.z - _length));
        Left = _world.CheckForVoxel(new(pos.x - _width, pos.y - _height, pos.z));
        Right = _world.CheckForVoxel(new(pos.x + _width, pos.y - _height, pos.z));
        FrontLeft = _world.CheckForVoxel(new(pos.x - _width, pos.y - _height, pos.z + _length));
        FrontRight = _world.CheckForVoxel(new(pos.x + _width, pos.y - _height, pos.z + _length));
        BackLeft = _world.CheckForVoxel(new(pos.x - _width, pos.y - _height, pos.z - _length));
        BackRight = _world.CheckForVoxel(new(pos.x + _width, pos.y - _height, pos.z - _length));
    }

    private void CalculateVelocity(Vector3 pos)
    {
        if (ApplyGravity && _verticalMomentum > _gravity)
            _verticalMomentum += Time.fixedDeltaTime * _gravity;

        if (Center)
            _verticalMomentum = Time.fixedDeltaTime * -_gravity;

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

    public void SetVelocity(Vector3 velocity)
    {
        Reset();
        _velocity = velocity;
        _verticalMomentum = 0f;
    }

    private float CheckDownSpeed(Vector3 pos, float downSpeed)
    {
        if (
            (_world.CheckForVoxel(new(pos.x - _width, pos.y - _height + downSpeed, pos.z - _length)) && !BackLeft) ||
            (_world.CheckForVoxel(new(pos.x + _width, pos.y - _height + downSpeed, pos.z - _length)) && !BackRight) ||
            (_world.CheckForVoxel(new(pos.x + _width, pos.y - _height + downSpeed, pos.z + _length)) && !FrontRight) ||
            (_world.CheckForVoxel(new(pos.x - _width, pos.y - _height + downSpeed, pos.z + _length)) && !FrontLeft)
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
