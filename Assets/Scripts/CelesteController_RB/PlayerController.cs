﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum MovementState { SIMPLE, DASH, GRAB };

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    private PlayerMovementAction playerMovementActionMap;
    private Rigidbody2D thisBody;

    [SerializeField] private MovementState currentMovementState = MovementState.SIMPLE;

    [HideInInspector] public float inputX;
    [HideInInspector] public float inputY;

    //Stuff for Animations
    [HideInInspector] public float veriticalVelcity;

    [Header("General Properties")]
    [SerializeField] private bool isControllable;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float jumpForce;

    [Header("Checkers")]
    [SerializeField] private bool groundCheck;
    //GroundCheck = GroundCheckRealtime + coyotiness

    [Header("Gravity Values")]
    [SerializeField] private float overallGravityModifier;
    [SerializeField] private float fallGravityModifier;
    [SerializeField] private float jumpGravityModifier;
    [SerializeField] private float airDrag, landDrag, dashDrag;

    [Header("Ground Check values")]
    [SerializeField] private Transform foot;
    [SerializeField] private Vector2 footSize;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float coyoteTime;
    [SerializeField] private float groundCheckTimer;
    [SerializeField] private bool coyoteEnabled;
    [HideInInspector] public bool groundCheckRealtime;
    [SerializeField] private bool footVisualization;

    [Header("Dash Values")]
    [SerializeField] private float dashForce;
    private Vector2 dashVector;
    [SerializeField] private float dashRecoverTime;
    private bool wasDashed;
    [SerializeField] private float dashTimeOffset;
    private float dashTimer;
    [SerializeField] private bool canDash;

    [Header("Grab Values")]
    [SerializeField] private float grabMovementModifier;
    [SerializeField] private bool isGrabbing;
    [SerializeField] private bool canGrab;
    [SerializeField] private Transform leftHand, rightHand;
    [SerializeField] private Vector2 handSize;
    [SerializeField] private LayerMask grabMask;
    [SerializeField] private bool handVisualization;

    private void Awake()
    {
        //Initialize variables
        playerMovementActionMap = new PlayerMovementAction();
        thisBody = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        playerMovementActionMap.Enable();
    }

    private void OnDisable()
    {
        playerMovementActionMap.Disable();
    }

    private void Update()
    {
        SetValues();

        GrabInput();
    }

    private void FixedUpdate()
    {
        SetHorizontalVelocity();

        GroundCheck(); ApplyDrag();

        JumpMechanism();

        RemoveFloatyness();

        SetDash(); //No value mean set the dash according to the groundCheckRealtime

        Dash();

        GrabMechanism();
    }


    private void SetValues()
    {
        veriticalVelcity = thisBody.velocity.y;
        dashVector = new Vector2(inputX * dashForce, inputY * dashForce);
    }

    private void GrabInput()
    {
        inputX = playerMovementActionMap.General.Movement.ReadValue<float>();
        inputY = playerMovementActionMap.General.VerticalMovement.ReadValue<float>();
    }

    private void SetHorizontalVelocity()
    {
        if (isControllable == false) return;

        thisBody.velocity = new Vector2(inputX * moveSpeed, thisBody.velocity.y);
    }

    private void JumpMechanism()
    {
        if (currentMovementState == MovementState.SIMPLE)
        {
            if (groundCheck == true && Keyboard.current.cKey.wasPressedThisFrame)
            {
                thisBody.velocity = new Vector2(thisBody.velocity.x, jumpForce);
            }
        }
        else if (currentMovementState == MovementState.GRAB)
        {
            if (isGrabbing == true && Keyboard.current.cKey.wasPressedThisFrame)
            {
                thisBody.velocity = new Vector2(thisBody.velocity.x, jumpForce);
            }
        }

    }

    private void RemoveFloatyness()
    {
        if (thisBody.velocity.y <= 0)
        {
            thisBody.gravityScale = fallGravityModifier * overallGravityModifier;
        }
        else if (thisBody.velocity.y > 0)
        {
            thisBody.gravityScale = jumpGravityModifier * overallGravityModifier;
        }
    }

    private void GroundCheck()
    {
        groundCheckRealtime = Physics2D.OverlapBox(foot.position, footSize, 0, groundMask);

        if (groundCheckRealtime == false && groundCheck == true && coyoteEnabled == false)
        {
            groundCheckTimer = coyoteTime;
            coyoteEnabled = true;
        }

        if (groundCheckRealtime == false)
        {
            if (groundCheckTimer > 0) { groundCheckTimer -= Time.deltaTime; }
            else if (groundCheckTimer <= 0) { groundCheck = false; coyoteEnabled = false; }
        }
        else
        {
            groundCheckTimer = 0;
            groundCheck = true;
            coyoteEnabled = false;
        }
    }

    private void ApplyDrag()
    {
        if (thisBody.drag != dashDrag)
        {
            if (groundCheckRealtime == true)
            {
                thisBody.drag = landDrag;
            }
            else
            {
                thisBody.drag = airDrag;
            }
        }
    }

    //Can be called from another script (crystal) to replenish the dash
    public void SetDash(int value = -1)
    {
        if (value == -1)
        {
            if (groundCheckRealtime == true) canDash = true;
        }
        else
        {
            if (value == 0) canDash = false;
            else if (value == 1) canDash = true;
        }
    }

    private void Dash()
    {
        //Take Dash Input
        if (Keyboard.current.zKey.wasPressedThisFrame && canDash == true)
        {
            dashTimer = dashTimeOffset;
        }
        else
        {
            dashTimer -= Time.deltaTime;
        }

        //Apply dash according to the input
        if (dashTimer > 0 && currentMovementState != MovementState.DASH)
        {
            if (dashVector == Vector2.zero) return;

            if (dashVector == new Vector2(1 * dashForce, 0) || dashVector == new Vector2(-1 * dashForce, 0))
            {
                //If dashing sideways then overall gravity acting on the player should be zero to give a uncontrolled situation.
                isControllable = false;
                thisBody.velocity = Vector2.zero;
                overallGravityModifier = 0;
                thisBody.drag = dashDrag;
            }
            else
            {
                //If not dashing sideways then overall gravity should be normal as there is no need of uncontrolled situation as it is already is
                isControllable = false;
                thisBody.velocity = Vector2.zero;
                thisBody.drag = dashDrag;
            }

            thisBody.AddForce(dashVector);
            StartCoroutine(DashRecover(dashRecoverTime));

            currentMovementState = MovementState.DASH;
            canDash = false;
        }
    }

    private IEnumerator<WaitForSeconds> DashRecover(float time)
    {
        yield return new WaitForSeconds(time);

        isControllable = true;
        overallGravityModifier = 1;
        thisBody.drag = airDrag;
        currentMovementState = MovementState.SIMPLE;
    }

    private void GrabMechanism()
    {
        canGrab = Physics2D.OverlapBox(leftHand.position, handSize, 0, grabMask) || Physics2D.OverlapBox(rightHand.position, handSize, 0, grabMask);

        if (canGrab)
        {
            if (Keyboard.current.xKey.isPressed)
            {
                isControllable = false;
                isGrabbing = true;
                overallGravityModifier = 0;
                currentMovementState = MovementState.GRAB;
                thisBody.velocity = new Vector2(thisBody.velocity.x, inputY * moveSpeed * grabMovementModifier);
            }
            else
            {
                isControllable = true;
                isGrabbing = false;
                overallGravityModifier = 1;
                currentMovementState = MovementState.SIMPLE;
            }
        }
        else
        {
            isControllable = true;
            isGrabbing = false;
            overallGravityModifier = 1;
            currentMovementState = MovementState.SIMPLE;
        }
    }

    private void OnDrawGizmos()
    {
        if (footVisualization == true) Gizmos.DrawCube(foot.position, footSize);
        if (handVisualization == true) Gizmos.DrawCube(leftHand.position, handSize);
        if (handVisualization == true) Gizmos.DrawCube(rightHand.position, handSize);
    }
}
