using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public bool isGrounded;
    public bool isSprinting;
    public bool ApplyGravity = true;

    private Transform _mainCamera;
    private World World;

    Vector2 mouse = new(0f, 0f);
    Vector2 movement = new(0f, 0f);

    private Vector3 angles;

    public float mouseSensitivity = 500f;
    public float walkSpeed = 3f;
    public float sprintSpeed = 6f;
    public float jumpForce = 7f;
    // -9.8f
    public float gravity = -15f;

    public float playerWidth = 0.3f;


    private Vector3 velocity;
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
    public float checkIncrement = 0.75f;
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
        if (World != null && highlightBlock.gameObject.activeSelf && value.isPressed)
        {
            //World.GetChunkFromVector3(highlightBlock.position).EditVoxel(highlightBlock.position, 0);
            BlockObject blockObjetc = World.GetVoxel(highlightBlock.position);
            if (blockObjetc.blockType != Block.Air)
            {
                World.PlaceBlock(highlightBlock.position, 0);

                var inventory = GetComponent<PlayerInventory>();
                inventory.AddItem(new Item(blockObjetc), 1);
            }
        }
    }

    private void OnPlaceBlock(InputValue value)
    {
        if (selectedBlockIndex < 0) return;

        BlockObject blockObjet = World.GetVoxel(placeBlock.position);
        if (blockObjet.blockType != Block.Air) return;

        if (!placeBlock.gameObject.activeSelf || !value.isPressed) return;

        int xSelf = Mathf.FloorToInt(transform.position.x);
        int ySelf = Mathf.FloorToInt(transform.position.y);
        int zSelf = Mathf.FloorToInt(transform.position.z);

        int xBlock = Mathf.FloorToInt(placeBlock.position.x);
        int yBlock = Mathf.FloorToInt(placeBlock.position.y);
        int zBlock = Mathf.FloorToInt(placeBlock.position.z);

        if (xSelf == xBlock && (ySelf == yBlock || ySelf + 1 == yBlock) && zSelf == zBlock) return;

        var inventory = GetComponent<PlayerInventory>();
        Block selBlock = (Block)selectedBlockIndex;
        BlockObject selObj = World.BlocksScObj.Blocks[selBlock];

        if (inventory.RemoveItem(new Item(selObj), 1))
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

    private void OnOpenInventory(InputValue value)
    {
        World.InUI = !World.InUI;
        if (World.InUI)
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

#pragma warning restore IDE0051

    private void Start()
    {
        //camera = GameObject.Find("Main Camera").transform;
        _mainCamera = transform.Find("LookFrom");
        //_mainCamera = Camera.main.transform;

        World = World.Instance;

        Cursor.lockState = CursorLockMode.Locked;
    }

    private void FixedUpdate()
    {
        CalculateVelocity();
        transform.Translate(velocity, Space.World);
    }

    private void Update()
    {
        if (!World.InUI)
        {
            PlaceCursorBlock();

            //float rotationY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
            //float rotationX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float rotationY = mouse.y * mouseSensitivity * Time.deltaTime;
            float rotationX = mouse.x * mouseSensitivity * Time.deltaTime;
            if (rotationY > 0)
                angles = new Vector3(Mathf.MoveTowards(angles.x, -90, rotationY), angles.y + rotationX, 0);
            else
                angles = new Vector3(Mathf.MoveTowards(angles.x, 90, -rotationY), angles.y + rotationX, 0);

            transform.localEulerAngles = new(0f, angles.y, 0f);
            _mainCamera.localEulerAngles = new(angles.x, 0f, 0f);
        }


        //frustumPlanes[0].GetSide

    }

    //void OnDrawGizmos()
    //{
    //    Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
    //    //Debug.Log(frustumPlanes.Length);
    //    //frustumPlanes[0].ToString
    //    //Camera.main.transform.rotation
    //    //frustumPlanes
    //    //Plane.
    //    //frustumPlanes[0].

    //    //Quaternion rotation = Quaternion.LookRotation(Camera.main.transform.TransformDirection(frustumPlanes[0].normal));
    //    //Matrix4x4 trs = Matrix4x4.TRS(Camera.main.transform.TransformPoint(frustumPlanes[0].normal), rotation, Camera.main.transform.localScale);
    //    //Gizmos.matrix = trs;
    //    //Color32 color = Color.blue;
    //    //color.a = 125;
    //    //Gizmos.color = color;
    //    //Gizmos.DrawCube(Vector3.zero, new Vector3(1.0f, 1.0f, 0.0001f));

    //    //Gizmos.matrix = Matrix4x4.identity;
    //    //Gizmos.color = Color.white;
    //    for (int i = 0; i < frustumPlanes.Length; i++)
    //    {
    //        //DrawPlane(Camera.main.transform.position, frustumPlanes[i].normal);

    //        //frustumPlanes[i].GetSide();
    //        Debug.DrawRay(Camera.main.transform.position, frustumPlanes[i].normal * frustumPlanes[i].distance * -1, Color.red);
    //    }
    //}

    //public void DrawPlane(Vector3 position, Vector3 normal)
    //{
    //    Vector3 v3;

    //    if (normal.normalized != Vector3.forward)
    //        v3 = Vector3.Cross(normal, Vector3.forward).normalized * normal.magnitude;
    //    else
    //        v3 = Vector3.Cross(normal, Vector3.up).normalized * normal.magnitude;

    //    var corner0 = position + v3;
    //    var corner2 = position - v3;
    //    var q = Quaternion.AngleAxis(90.0f, normal);
    //    v3 = q * v3;
    //    var corner1 = position + v3;
    //    var corner3 = position - v3;

    //    Debug.DrawLine(corner0, corner2, Color.green);
    //    Debug.DrawLine(corner1, corner3, Color.green);
    //    Debug.DrawLine(corner0, corner1, Color.green);
    //    Debug.DrawLine(corner1, corner2, Color.green);
    //    Debug.DrawLine(corner2, corner3, Color.green);
    //    Debug.DrawLine(corner3, corner0, Color.green);
    //    Debug.DrawRay(position, normal, Color.red);
    //}

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
        velocity = moveSpeed * Time.fixedDeltaTime * ((transform.forward * movement.y) + (transform.right * movement.x));

        velocity += Time.fixedDeltaTime * verticalMomentum * Vector3.up;

        if ((velocity.z > 0f && Front) || (velocity.z < 0f && Back))
            velocity.z = 0f;
        if ((velocity.x > 0f && Right) || (velocity.x < 0f && Left))
            velocity.x = 0f;

        if (velocity.y < 0f)
            velocity.y = CheckDownSpeed(velocity.y);
        else if (velocity.y > 0f)
            velocity.y = CheckUpSpeed(velocity.y);

    }

    private void PlaceCursorBlock()
    {
        Vector3 lastPos = new();

        for (float step = checkIncrement; step < reach; step += checkIncrement)
        {
            Vector3 pos = _mainCamera.position + (_mainCamera.forward * step);

            if (World.CheckForVoxel(pos))
            {
                //World.GetChunkCoordFromVector3(pos)
                highlightBlock.position = new(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
                //placeBlock.position = lastPos;

                float xCheck = pos.x % 1;
                if (xCheck > 0.5f)
                    xCheck--;
                else if (xCheck < -0.5f)
                    xCheck++;
                float yCheck = pos.y % 1;
                if (yCheck > 0.5f)
                    yCheck--;
                else if (yCheck < -0.5f)
                    yCheck++;
                float zCheck = pos.z % 1;
                if (zCheck > 0.5f)
                    zCheck--;
                else if (zCheck < -0.5f)
                    zCheck++;

                if (Mathf.Abs(xCheck) < Mathf.Abs(yCheck) && Mathf.Abs(xCheck) < Mathf.Abs(zCheck))
                {
                    // place block on x axis
                    if (xCheck < 0)
                        placeBlock.position = highlightBlock.position + Vector3.right;
                    else
                        placeBlock.position = highlightBlock.position + Vector3.left;
                }
                else if (Mathf.Abs(zCheck) < Mathf.Abs(yCheck) && Mathf.Abs(zCheck) < Mathf.Abs(xCheck))
                {
                    // place block on z axis
                    if (zCheck < 0)
                        placeBlock.position = highlightBlock.position + Vector3.forward;
                    else
                        placeBlock.position = highlightBlock.position + Vector3.back;
                }
                else
                {
                    // place block on y axis by default
                    if (yCheck < 0)
                        placeBlock.position = highlightBlock.position + Vector3.up;
                    else
                        placeBlock.position = highlightBlock.position + Vector3.down;
                }

                highlightBlock.gameObject.SetActive(true);
                placeBlock.gameObject.SetActive(true);

                return;
            }
            lastPos = new(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
        }

        highlightBlock.gameObject.SetActive(false);
        placeBlock.gameObject.SetActive(false);
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


}
