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

    private new Transform camera;
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

    public Block selectedBlockIndex = Block.Bedrock;

    public bool isJumping;

#pragma warning disable IDE0051
    // "CodeQuality", "IDE0051: Private member is unused"
    // These methods gets automatically called by Unity's new Input System

    private void OnMovement(InputValue value)
    {
        movement = value.Get<Vector2>();
    }

    //private void OnRotationX(InputValue value)
    //{
    //    mouse.x = value.Get<float>();
    //}

    //private void OnRotationY(InputValue value)
    //{
    //    mouse.y = value.Get<float>();
    //}

    private void OnDestroyBlock(InputValue value)
    {
        if (World != null && highlightBlock.gameObject.activeSelf && value.isPressed)
        {
            World.GetChunkFromVector3(highlightBlock.position).EditVoxel(highlightBlock.position, 0);
        }
    }

    private void OnPlaceBlock(InputValue value)
    {
        if (placeBlock.gameObject.activeSelf && value.isPressed)
        {
            int xSelf = Mathf.FloorToInt(transform.position.x);
            int ySelf = Mathf.FloorToInt(transform.position.y);
            int zSelf = Mathf.FloorToInt(transform.position.z);

            int xBlock = Mathf.FloorToInt(placeBlock.position.x);
            int yBlock = Mathf.FloorToInt(placeBlock.position.y);
            int zBlock = Mathf.FloorToInt(placeBlock.position.z);

            if (!(xSelf == xBlock && (ySelf == yBlock || ySelf + 1 == yBlock) && zSelf == zBlock))
            {
                World.GetChunkFromVector3(placeBlock.position).EditVoxel(placeBlock.position, selectedBlockIndex);
            }
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

    private void Start()
    {
        //camera = GameObject.Find("Main Camera").transform;
        camera = Camera.main.transform;

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
        PlaceCursorBlock();

        float rotationY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        float rotationX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        if (rotationY > 0)
            angles = new Vector3(Mathf.MoveTowards(angles.x, -90, rotationY), angles.y + rotationX, 0);
        else
            angles = new Vector3(Mathf.MoveTowards(angles.x, 90, -rotationY), angles.y + rotationX, 0);

        transform.localEulerAngles = new(0f, angles.y, 0f);
        camera.localEulerAngles = new(angles.x, 0f, 0f);
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
            Vector3 pos = camera.position + (camera.forward * step);

            if (World.CheckForVoxel(pos))
            {
                highlightBlock.position = new(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
                //placeBlock.position = lastPos;

                float xCheck = pos.x % 1;
                if (xCheck > 0.5f)
                    xCheck--;
                float yCheck = pos.y % 1;
                if (yCheck > 0.5f)
                    yCheck--;
                float zCheck = pos.z % 1;
                if (zCheck > 0.5f)
                    zCheck--;

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
