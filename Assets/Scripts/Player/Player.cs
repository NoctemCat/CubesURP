using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

#pragma warning disable UNT0006

public class Player : MonoBehaviour
{
    public Transform LookFrom { get; private set; }
    private World World;
    private PlayerInventory _inventory;

    public bool isGrounded;
    public bool isSprinting;
    public bool ApplyGravity = true;


    Vector2 mouse = new(0f, 0f);
    Vector2 movement = new(0f, 0f);

    private Vector3 angles;

    public float walkSpeed = 3f;
    public float sprintSpeed = 6f;
    public float jumpForce = 7f;
    // -9.8f
    public float gravity = -15f;

    public float playerWidth = 0.3f;


    private Vector3 _velocity;
    public Vector3 Velocity => _velocity;
    private float verticalMomentum = 0f;

    private float PX => transform.position.x;
    private float PY => transform.position.y;
    private float PZ => transform.position.z;

    public bool Front => World.CheckForVoxel(new(PX, PY, PZ + playerWidth)) || World.CheckForVoxel(new(PX, PY + 1f, PZ + playerWidth));
    public bool Back => World.CheckForVoxel(new(PX, PY, PZ - playerWidth)) || World.CheckForVoxel(new(PX, PY + 1f, PZ - playerWidth));
    public bool Left => World.CheckForVoxel(new(PX - playerWidth, PY, PZ)) || World.CheckForVoxel(new(PX - playerWidth, PY + 1f, PZ));
    public bool Right => World.CheckForVoxel(new(PX + playerWidth, PY, PZ)) || World.CheckForVoxel(new(PX + playerWidth, PY + 1f, PZ));

    public bool FrontLeft => World.CheckForVoxel(new(PX - playerWidth, PY, PZ + playerWidth)) || World.CheckForVoxel(new(PX - playerWidth, PY + 1f, PZ + playerWidth));
    public bool FrontRight => World.CheckForVoxel(new(PX + playerWidth, PY, PZ + playerWidth)) || World.CheckForVoxel(new(PX + playerWidth, PY + 1f, PZ + playerWidth));
    public bool BackLeft => World.CheckForVoxel(new(PX - playerWidth, PY, PZ - playerWidth)) || World.CheckForVoxel(new(PX - playerWidth, PY + 1f, PZ - playerWidth));
    public bool BackRight => World.CheckForVoxel(new(PX + playerWidth, PY, PZ - playerWidth)) || World.CheckForVoxel(new(PX + playerWidth, PY + 1f, PZ - playerWidth));

    public Transform highlightBlock;
    public Transform placeBlock;
    public float reach = 8f;

    public int selectedBlockIndex;
    public bool isJumping;

#pragma warning disable IDE0051
    // "CodeQuality", "IDE0051: Private member is unused"
    // These methods gets automatically called by Unity's new Input System

    private void OnMovement(InputValue value)
    {
        movement = value.Get<Vector2>();
    }

    private void OnLook(InputValue value)
    {
        mouse = value.Get<Vector2>();
    }

    private void OnDestroyBlock(InputValue value)
    {
        if (
            Time.timeScale == 0 ||
            World == null ||
            !highlightBlock.gameObject.activeSelf ||
            !value.isPressed
        ) return;

        //World.GetChunkFromVector3(highlightBlock.position).EditVoxel(highlightBlock.position, 0);
        BlockObject blockObject = World.GetVoxel(highlightBlock.position);
        if (blockObject.BlockType != Block.Air)
        {
            World.PlaceBlock(highlightBlock.position, 0);

            GroundItemsPool.DropItems(
                highlightBlock.position + new Vector3(0.5f, 0.5f, 0.5f),
                new(UnityEngine.Random.value * 0.05f, UnityEngine.Random.value * 0.1f, UnityEngine.Random.value * 0.05f),
                blockObject, 1
            );
        }
    }

    private void OnPlaceBlock(InputValue value)
    {
        if (Time.timeScale == 0 || selectedBlockIndex < 0) return;

        BlockObject blockObjet = World.GetVoxel(placeBlock.position);

        if (
            blockObjet.BlockType != Block.Air ||
            !placeBlock.gameObject.activeSelf ||
            !value.isPressed
        ) return;

        int xSelf = Mathf.FloorToInt(transform.position.x);
        int ySelf = Mathf.FloorToInt(transform.position.y);
        int zSelf = Mathf.FloorToInt(transform.position.z);

        int xBlock = Mathf.FloorToInt(placeBlock.position.x);
        int yBlock = Mathf.FloorToInt(placeBlock.position.y);
        int zBlock = Mathf.FloorToInt(placeBlock.position.z);

        if (xSelf == xBlock && (ySelf == yBlock || ySelf + 1 == yBlock) && zSelf == zBlock) return;

        Block selBlock = (Block)selectedBlockIndex;
        BlockObject selObj = World.BlocksScObj.Blocks[selBlock];

        if (_inventory.RemoveItem(new Item(selObj), 1))
        {
            World.PlaceBlock(placeBlock.position, selBlock);
        }
    }

    private void OnJump(InputValue value)
    {
        isJumping = value.isPressed;
    }

    private void OnSprint(InputValue value)
    {
        isSprinting = value.isPressed;
    }

#pragma warning restore IDE0051

    private async UniTaskVoid Start()
    {
        //camera = GameObject.Find("Main Camera").transform;
        LookFrom = transform.GetChild(1);

        World = World.Instance;
        _inventory = GetComponent<PlayerInventory>();

        Cursor.lockState = CursorLockMode.Locked;

        gameObject.SetActive(false);

        await UniTask.WaitForSeconds(0.5f);
        gameObject.SetActive(true);
        //var blok = CubesUtils.GetIntersectedWorldBlocksD(new(0f, 0f, 0f), new(0, 5f, 4f));
        //foreach (var b in blok)
        //{
        //    Debug.Log(b);
        //}
    }

    private void OnDestroy()
    {
    }

    private void FixedUpdate()
    {
        CalculateVelocity();
        transform.Translate(_velocity, Space.World);
    }

    private void Update()
    {
        if (!_inventory.InInventory)
        {
            PlaceCursorBlock();

            float rotationY = mouse.y * World.Settings.MouseSenstivity * Time.deltaTime;
            float rotationX = mouse.x * World.Settings.MouseSenstivity * Time.deltaTime;
            if (rotationY > 0)
                angles = new Vector3(Mathf.MoveTowards(angles.x, -90, rotationY), angles.y + rotationX, 0);
            else
                angles = new Vector3(Mathf.MoveTowards(angles.x, 90, -rotationY), angles.y + rotationX, 0);

            transform.localEulerAngles = new(0f, angles.y, 0f);
            LookFrom.localEulerAngles = new(angles.x, 0f, 0f);
        }
        else
        {
            highlightBlock.gameObject.SetActive(false);
            placeBlock.gameObject.SetActive(false);
        }
        //frustumPlanes[0].GetSide
    }

    private void CalculateVelocity()
    {
        if (isGrounded && isJumping)
        {
            verticalMomentum = jumpForce;
            isGrounded = false;
        }

        if (ApplyGravity && verticalMomentum > gravity)
            verticalMomentum += Time.fixedDeltaTime * gravity;

        float moveSpeed = (!isSprinting) ? walkSpeed : sprintSpeed;
        _velocity = moveSpeed * Time.fixedDeltaTime * ((transform.forward * movement.y) + (transform.right * movement.x));

        _velocity += Time.fixedDeltaTime * verticalMomentum * Vector3.up;

        if ((_velocity.z > 0f && Front) || (_velocity.z < 0f && Back))
            _velocity.z = 0f;
        if ((_velocity.x > 0f && Right) || (_velocity.x < 0f && Left))
            _velocity.x = 0f;

        //World.GetAccessForPlayer();
        if (_velocity.y < 0f)
            _velocity.y = CheckDownSpeed(_velocity.y);
        else if (_velocity.y > 0f)
            _velocity.y = CheckUpSpeed(_velocity.y);

    }

    private void PlaceCursorBlock()
    {
        bool disable = true;
        var cast = new VoxelRaycast(null);

        cast.Raycast(LookFrom.position, LookFrom.forward, reach, (Vector3Int block, Vector3Int faceNormal) =>
        {
            bool isSolid = World.CheckForVoxel(block);

            if (isSolid)
            {
                disable = false;
                highlightBlock.position = block;
                placeBlock.position = block + faceNormal;
                highlightBlock.gameObject.SetActive(true);
                placeBlock.gameObject.SetActive(true);
            }
            return isSolid;
        });

        if (disable)
        {
            highlightBlock.gameObject.SetActive(false);
            placeBlock.gameObject.SetActive(false);
        }
    }

    private float CheckDownSpeed(float downSpeed)
    {
        if (
            (World.CheckForVoxel(new(PX - playerWidth, PY + downSpeed, PZ - playerWidth)) && !BackLeft) ||
            (World.CheckForVoxel(new(PX + playerWidth, PY + downSpeed, PZ - playerWidth)) && !BackRight) ||
            (World.CheckForVoxel(new(PX + playerWidth, PY + downSpeed, PZ + playerWidth)) && !FrontRight) ||
            (World.CheckForVoxel(new(PX - playerWidth, PY + downSpeed, PZ + playerWidth)) && !FrontLeft)
        )
        {
            isGrounded = true;
            verticalMomentum = 0f;
            return 0;
        }
        else
        {
            isGrounded = false;
            return downSpeed;
        }
    }

    private float CheckUpSpeed(float upSpeed)
    {

        if (
            (World.CheckForVoxel(new(PX - playerWidth, PY + 1.8f + upSpeed, PZ - playerWidth)) && !BackLeft) ||
            (World.CheckForVoxel(new(PX + playerWidth, PY + 1.8f + upSpeed, PZ - playerWidth)) && !BackRight) ||
            (World.CheckForVoxel(new(PX + playerWidth, PY + 1.8f + upSpeed, PZ + playerWidth)) && !FrontRight) ||
            (World.CheckForVoxel(new(PX - playerWidth, PY + 1.8f + upSpeed, PZ + playerWidth)) && !FrontLeft)
        )
        {
            verticalMomentum = 0;
            return 0;
        }
        else
        {
            return upSpeed;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent(out GroundItem item) && Time.time - item.CreateTime > 0.5f)
        {
            //Debug.Log($"{Time.time}, {item.CreateTime}");
            _inventory.AddItem(item.ItemObj.Data, item.Amount);
            other.gameObject.SetActive(false);
            return;
        }

        //if (other.TryGetComponent(out BlockPhysics physics))
        //{
        //    Vector3 dir = (physics.transform.position - transform.position).normalized;
        //    physics.AddVelocity(dir * Time.deltaTime);
        //}
    }

    public void OnTriggerEnter(Collider other)
    {
        //Debug.Log(other.name);
    }

    public Vector3 GetDropItemVelocity()
    {
        return LookFrom.forward * .25f + _velocity * 2f;
    }

}
