using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerController : MoveController
{
    [Header("Movement")]
    [SerializeField] private float runMultiplier;
    private bool isIdle = true;
    private bool isWalking = false;
    private bool isRunning = false;
    private float xInput;
    private bool isFacingRight = true;

    [Header("Jump")]
    [SerializeField] private float jumpCutMultiplier;
    [SerializeField] private float jumpMoveSpeedBuffer;
    public float jumpGravity;
    public float fallGravity;
    private bool isGrounded;
    private bool isLanding = false;
    [SerializeField] private float jumpForce;
    [SerializeField] private float landingVelocity;
    [SerializeField] private float landingRecoveryTime;
    public LayerMask groundCheckMask;
    public float groundCheckRadius;

    [Header("Dash")]
    [SerializeField] private float dashForce;
    [SerializeField] private float dashCooldown;
    private bool isDashing = false;
    private bool canDash = true;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] public Rigidbody2D rb;
    [SerializeField] private Transform groundCheck;

    private void Start()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundCheckMask);
    }

    public void Update()
    {
        Idle();
        Move(new Vector3(xInput, 0, 0));
        Flip();
        Dash();
        Jump();
        GroundCheck();
        ControlAnimation();
    }

    private void LateUpdate()
    {

    }

    public void Idle()
    {
        isIdle = isGrounded && !isWalking && !isLanding;
    }

    public override void Move(Vector3 direction)
    {
        xInput = Input.GetAxisRaw("Horizontal");
        isWalking = Mathf.Abs(xInput) > 0.1f && isGrounded && !isLanding;

        if (isRunning)
        {
            direction *= runMultiplier;
        }

        if (rb.linearVelocityY > 0.1f)
        {
            if(!isRunning)
            {
                direction *= jumpMoveSpeedBuffer;
            }                                               // Increase movement speed while jumping
        }

        if(isRunning && Input.GetKeyUp(KeyCode.LeftShift))
        {
            isRunning = false;
        }

        if (isLanding)
        {
            xInput = 0;
        }

        base.Move(direction);
    }

    public void ControlAnimation()
    {
        animator.SetBool("Walking", isWalking);
        animator.SetBool("Running", isRunning);
        animator.SetBool("Dashing", isDashing);
        animator.SetBool("Idle", isIdle);
        animator.SetFloat("yVelocity", rb.linearVelocityY);
        animator.SetBool("Grounded", isGrounded);
        animator.SetBool("Landing", isLanding);
    }

    public void Jump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isLanding)
        {
            rb.gravityScale = jumpGravity;
            rb.AddForce(Vector3.up * jumpForce, ForceMode2D.Impulse);
            isGrounded = false;

            isRunning = false;  
        }
        else
        {
            if (rb.linearVelocityY > 0.1f && Input.GetKeyUp(KeyCode.Space))          //Release jump button while going up: Short jump
            {
                rb.gravityScale = fallGravity * jumpCutMultiplier;
            }
            else if (rb.linearVelocityY < -0.1f)
            {
                rb.gravityScale = fallGravity;
            }
        }
    }

    public void Dash()
    {
        isDashing = false;
        int direction = isFacingRight ? 1 : -1;
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            if (canDash)
            {
                rb.AddForce(new Vector3(direction, 0, 0) * dashForce, ForceMode2D.Impulse);
                StartCoroutine(OnDashing());
                isDashing = true;
            }
            isRunning = true;
        }

        if (isRunning && Input.GetKeyUp(KeyCode.LeftShift))
        {
            isRunning = false;
        }
    }

    private IEnumerator OnDashing()
    {
        canDash = false;
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    public void Flip()
    {
        if (!isLanding)
        {
            if ((xInput < 0 && isFacingRight) || (xInput > 0 && !isFacingRight))
            {
                float value = Mathf.Sign(xInput);                                     // Get the sign of xInput (-1 for left, 1 for right)
                transform.localScale = new Vector3(value, 1, 1);
                isFacingRight = !isFacingRight;
            }
        }
    }

    public void GroundCheck()
    {
        float velocityY = rb.linearVelocityY;
        if (velocityY < -0.1f)
        {
            isGrounded = false;        
            isRunning = false;

            if (Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundCheckMask))
            {
                if (velocityY < landingVelocity)
                {
                    StartCoroutine(OnLanding());
                }

                isGrounded = true;
            }

            if(velocityY < landingVelocity)
            {
                rb.linearVelocityY = landingVelocity;                                    // Max falling velocity
            }
        }
                                                                                       // Check for when the player lands on the ground when the velocity is around 0 
        if(velocityY < 0.1f && velocityY >= 0 && !isGrounded)                          //(e.g., Jumping up a platform with equal height to jump force)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundCheckMask);
        }
    }

    public IEnumerator OnLanding()                                                     // Limit player movement for a short time after landing 
    {
        isLanding = true;
        yield return new WaitForSeconds(landingRecoveryTime);
        isLanding = false;
    }


    private void OnDrawGizmos()
    {
        DrawGizmo();
    }

    public void DrawGizmo()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}

public class Player : SingletonMonoBehaviour<PlayerController>
{

}
