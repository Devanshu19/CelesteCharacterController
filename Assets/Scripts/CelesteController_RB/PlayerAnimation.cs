﻿using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController))]
public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;
    private PlayerController playerController;
    [SerializeField] private ParticleSystem dashParticleGO;

    private void Start()
    {
        animator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();

        PlayerController.EDashed += ToggleDashAnimation;
        PlayerController.EGrabbed += ToggleGrabAnimation;
    }

    private void OnDisable()
    {
        PlayerController.EDashed -= ToggleDashAnimation;
        PlayerController.EGrabbed -= ToggleGrabAnimation;
    }

    private void Update()
    {
        SetIdleMoveAnimation();
        SetVerticalMovementAndGroundCheckValues();
        SetJumpAnimation();
        RotatePlayer();
    }

    private void SetIdleMoveAnimation()
    {
        int inputX = (int)playerController.inputX;
        animator.SetInteger("HorizontalInput", inputX);
    }

    private void SetJumpAnimation()
    {
        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            animator.SetTrigger("Jump");
        }
    }

    private void SetVerticalMovementAndGroundCheckValues()
    {
        int verticalMovement = (int)playerController.thisVelocity.y;
        bool groundCheckValue = playerController.groundCheckRealtime;

        animator.SetBool("IsGrounded", groundCheckValue);
        animator.SetInteger("VerticalMovement", verticalMovement);
    }

    public void RotatePlayer(bool shouldRotateLeft)
    {
        //If not in simple state then avoid rotation
        if (playerController.CurrentMovementState != MovementState.SIMPLE) return;

        if (shouldRotateLeft) playerController.gameObject.transform.rotation = Quaternion.Euler(0, 180, 0);
        else playerController.gameObject.transform.rotation = Quaternion.Euler(0, 0, 0);
    }

    public void RotatePlayer()
    {
        //If not in simple state then avoid rotation
        if (playerController.CurrentMovementState != MovementState.SIMPLE && playerController.CurrentMovementState != MovementState.JUMP) return;

        if (this.playerController.inputX > 0)
        {
            playerController.gameObject.transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        else if (playerController.inputX < 0)
        {
            playerController.gameObject.transform.rotation = Quaternion.Euler(0, 180, 0);
        }
    }

    private void ToggleDashAnimation(bool start)
    {
        if (start == true)
        {
            dashParticleGO.Play();
        }
        else
        {
            dashParticleGO.Stop();
        }
    }

    private void ToggleGrabAnimation(bool start)
    {
        if (start == true) { animator.SetTrigger("Grab"); animator.SetBool("IsGrabbing", true); }
        else
        {

            animator.SetBool("IsGrabbing", false);

        }
    }
}
