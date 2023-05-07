using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.Timeline;

public class PlayerController : MonoBehaviour
{
    [Header("조작키 설정")]
    public KeyCodeManager KCM;
    
    
    [Header("플레이어 설정")]
    [SerializeField] private float moveSpeed = 0;
    [SerializeField] private float jumpPower = 0;

    private bool isJumping = false; // 현재 점프 중인지 여부를 저장할 변수
    private bool isFalling = false; // 현재 떨어지고 있는 중인지 여부를 저장할 변수
    private bool hasJumped = false; // 이미 점프했는지 여부를 저장할 변수


    // Animator 컴포넌트
    private Animator animator;

    // 컴포넌트
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    // 공격 관련
    [SerializeField] private Transform attackPoint; // 공격 포인트
    [SerializeField] private float attackRange = 0.5f; // 히트 박스 범위
    [SerializeField] private LayerMask enemyLayers; // 적 감지
    [SerializeField] private float attackDuration = 0.5f; // 공격애니메이션 지속 시간
    [SerializeField] private float attackDelay = 0.5f; // 공격 딜레이
    [SerializeField] private int damage; // 무기 대미지 
    private bool isAttacking = false; // 공격 중인지 판단
    private bool canAttack = true; // 다음 공격을 할 수 있는지
    private bool canMove = true; // 움직일 수 있는지 판단
    private bool isJumpAttack = false; // 점프 후 공격 중인지 



    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }
    

    private void Update()
    {
        // 플레이어 이동 
        PlayerMove();

        // 플레이어 점프
        PlayerJump();

        // 레이캐스트
        Debug.DrawRay(rb.position - new Vector2(0.5f, 0), Vector3.down, new Color(0, 1, 0));
        Debug.DrawRay(rb.position + new Vector2(0.5f, 0), Vector3.down, new Color(0, 1, 0));

        RaycastHit2D rayHitLeft = Physics2D.Raycast(rb.position - new Vector2(0.5f, 0), Vector3.down, 1,
            LayerMask.GetMask("Ground"));
        RaycastHit2D rayHitRight = Physics2D.Raycast(rb.position + new Vector2(0.5f, 0), Vector3.down, 1,
            LayerMask.GetMask("Ground"));

        // 레이캐스트 충돌 검사
        if (rayHitLeft.collider != null || rayHitRight.collider != null)
        {
            // 점프 중일 때 땅에 닿으면 점프 애니메이션을 끝내고 isJumping 변수를 false로 변경
            if (isJumping)
            {
                animator.SetBool("isJumping", false);
                isJumping = false;
            }

            // 떨어지고 있을 때 땅에 닿으면 Falling 애니메이션을 끝내고 isFalling 변수를 false로 변경
            if (isFalling)
            {
                animator.SetBool("isFalling", false);
                isFalling = false;
            }
        }
        else // 레이캐스트 충돌하지 않으면 떨어지는 중임을 저장하기 위해 isFalling 변수를 true로 변경
        {
            isFalling = true;
        }

        if (Input.GetKeyDown(KeyCode.A) && !isAttacking)
        {
            StartCoroutine(AttackCoroutine());
        }
    }



    private void PlayerMove()
    {
        // 오른쪽 이동
        if (Input.GetKey(KeyCode.RightArrow))
        {
            rb.velocity = new Vector2(moveSpeed, rb.velocity.y);
            spriteRenderer.flipX = false;
            animator.SetBool("isRun", true);
        }
        // 왼쪽 이동
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            rb.velocity = new Vector2(-moveSpeed, rb.velocity.y);
            spriteRenderer.flipX = true;
            animator.SetBool("isRun", true);

        }
        // 이동 중이 아닐 때
        else
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            animator.SetBool("isRun", false);

        }

        if (!canMove || isAttacking) //  공격 중이거나 움직일 수 없는 상태라면 움직이지 않음
        {
            rb.velocity = Vector2.zero;
            return;
        }
    }

    private void PlayerJump()
    {
        if (!hasJumped && Input.GetButtonDown("Jump") && !animator.GetBool("isJumping"))
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpPower);
            animator.SetBool("isJumping", true);
            animator.SetBool("isRun", false); // 떨어지는 중에는 달리기 애니메이션 끄기
            hasJumped = true; // 점프했으므로 hasJumped 변수를 true로 설정

            if (isAttacking)
            {
                isJumpAttack = true;
                rb.gravityScale = 1.5f; // 빠른 떨어짐을 위해 중력 스케일 조정
            }
        }
        else if (rb.velocity.y < 0) // 떨어지는 중일 때
        {
            animator.SetBool("isJumping", false);
            animator.SetBool("isFalling", true); // Fall 애니메이션 실행
            animator.SetBool("isRun", false); // 달리기 애니메이션 끄기

            if (isJumpAttack)
            {
                rb.gravityScale = 3f; // 원래 중력 스케일로 복원
            }
        }
        else if (rb.velocity.y == 0) // 땅에 닿은 경우
        {
            animator.SetBool("isJumping", false);
            animator.SetBool("isFalling", false);
            hasJumped = false; // 다시 점프할 수 있도록 hasJumped 변수를 false로 설정

            if (isJumpAttack)
            {
                rb.gravityScale = 3f; // 원래 중력 스케일로 복원
                isJumpAttack = false;
            }
        }
    }


    private void Attack()
    {
        // 점프 중인 경우 공격 애니메이션 실행하지 않음
        if (isJumping)
        {
            return;
        }
        // 공격 애니메이션 
        animator.SetTrigger("Attack");
        // 무기 가 적에게 닿을 때 감지
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);

        foreach (Collider2D enemy in hitEnemies)
        {
            Debug.Log("hit" + enemy.name);
        }
    }
    private IEnumerator AttackCoroutine()
    {
        if (!canAttack || isJumping) yield break; // 점프 중일 때는 공격을 실행하지 않음

        isAttacking = true;
        canAttack = false;
        canMove = false; // 움직임을 제어하기 위해 canMove 변수를 false로 설정합니다.

        Attack();

        yield return new WaitForSeconds(attackDuration);

        isAttacking = false;

        yield return new WaitForSeconds(attackDelay);

        canAttack = true;
        canMove = true; // 움직임을 다시 허용하기 위해 canMove 변수를 true로 설정합니다.

        yield return null;
    }


    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        
        Gizmos.DrawWireSphere(attackPoint.position,attackRange);
    }
    
    [System.Serializable]
    public struct KeyCodeManager
    {
        public KeyCode ATTACK_1;
        public KeyCode JUMP;
        public KeyCode MOVE_LEFT;
        public KeyCode MOVE_RIGHT;
        public KeyCode MOVE_DOWN;
        public KeyCode MOVE_DASH;
    }
}


