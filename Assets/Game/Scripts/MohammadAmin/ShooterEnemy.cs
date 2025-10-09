using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleFactory;

public class ShooterEnemy : MonoBehaviour, IEnemy
{
    const int ONE_HUNDRED = 100;

    [Header("Choice Movement Type")]
    public EnemyMovementType enemyMovementType;

    [Space(height: 20f)]
    [SerializeField] SOShooterEnemy sOEnemy;
    [SerializeField] Transform bulletPosition;
    [SerializeField] GameObject bullet;
    [SerializeField] float shootPositionY;

    Rigidbody2D rb;
    Animator anym;
    int count = 1;

    bool top;
    bool arrive = false;
    bool once = true;


    private void Awake()
    {
        if (transform.position.y > 0)
            top = true;
        else
            top = false;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anym = GetComponent<Animator>();

        if (transform.position.x > 0)
            transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, transform.localScale.z);
        else
            transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
    }

    void Update()
    {
        Movement(sOEnemy.Speed);
        Attack();
    }

    public void Movement(float iSpeed)
    {
        switch (enemyMovementType)
        {
            case EnemyMovementType.Moving:
                if (top)
                {
                    rb.velocity = Vector3.down * iSpeed * ONE_HUNDRED * Time.deltaTime;
                }
                else
                {
                    rb.velocity = Vector3.up * iSpeed * ONE_HUNDRED * Time.deltaTime;
                }
                break;
            case EnemyMovementType.Const:
                if (transform.position.y >= shootPositionY - 0.1f && transform.position.y <= shootPositionY + 0.1f)
                    arrive = true;

                if (!arrive)
                {
                    transform.position = Vector2.Lerp(transform.position, new Vector2(transform.position.x, shootPositionY), iSpeed * Time.deltaTime);
                }
                else
                {
                    anym.SetBool("IsMove", false);
                    if (sOEnemy.FireCount <= count)
                        StartCoroutine(getOut(iSpeed));
                }
                break;
        }
    }

    IEnumerator getOut(float iSpeed)
    {
        yield return new WaitForSeconds(sOEnemy.FireRate * 2f);
        anym.SetBool("IsMove", true);
        transform.position = Vector2.Lerp(transform.position, new Vector2(transform.position.x, -7.5f), (iSpeed / 2) * Time.deltaTime);
    }

    public void Attack()
    {
        switch (enemyMovementType)
        {
            case EnemyMovementType.Moving:
                if (once)
                {
                    StartCoroutine(shootMoving());
                    once = false;
                }
                break;
            case EnemyMovementType.Const:
                if (once && arrive)
                {
                    StartCoroutine(shootConst());
                    once = false;
                }
                break;
        }
    }

    IEnumerator shootMoving()
    {
        yield return new WaitForSeconds(sOEnemy.FireRate);
        anym.SetTrigger("Shoot");
        Instantiate(bullet, bulletPosition.position, Quaternion.identity);
        StartCoroutine(shootMoving());
    }
    IEnumerator shootConst()
    {
        yield return new WaitForSeconds(sOEnemy.FireRate);
        anym.SetTrigger("Shoot");
        Instantiate(bullet, bulletPosition.position, Quaternion.identity);
        if (sOEnemy.FireCount != count)
        {
            StartCoroutine(shootConst());
            ++count;
        }
    }
}
