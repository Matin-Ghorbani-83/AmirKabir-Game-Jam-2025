using UnityEngine;
using SimpleFactory;
using System.Collections;

public class BirdEnemy : MonoBehaviour, IEnemy
{
    const int ONE_HUNDRED = 100;
    const float TIME = 1f;

    [Header("Choice Point Position")]
    public SpawnPointType spawnPointType;
    [Header("Choice Movement Type")]
    public EnemyMovementType enemyMovementType;

    [Space(height: 20f)]
    [SerializeField] SOBirdEnemy sOEnemy;
    [SerializeField] Transform point;

    float speed;
    Vector2 knockbackForce;

    Rigidbody2D rb;
    Animator anym;

    bool right;
    bool top;
    bool move = true;
    bool once = true;
    bool getOut = false;
    bool startGetOut = false;
    bool fakeCheck = false;


    private void Awake()
    {

    }

    private void Start()
    {
        if (transform.position.x <= 0)
            right = false;
        else
            right = true;

        if (transform.position.y <= 0)
            top = false;
        else
            top = true;


        switch (spawnPointType)
        {
            case SpawnPointType.Side:
                point.position = new Vector3(-7f + transform.position.x, 0f + transform.position.y, 0f);
                break;
            case SpawnPointType.Top:
                if (top)
                    point.position = new Vector3(transform.position.x, -3f + transform.position.y, 0f);
                else
                    point.position = new Vector3(transform.position.x, 3f + transform.position.y, 0f);
                break;
        }

        speed = sOEnemy.Speed;
        knockbackForce = sOEnemy.KnockbackForce;

        rb = GetComponent<Rigidbody2D>();
        anym = GetComponent<Animator>();

        setRotationAndLocalScale();
    }

    private void Update()
    {
        if (!getOut)
        {
            if (move)
                Movement(speed);
            else
                rb.velocity = Vector3.zero;
        }
        else
        {
            if (right)
            {
                // -2f ==> y position for exit enemy
                transform.position = Vector2.MoveTowards(transform.position, new Vector2(12.5f, -2f), (speed / 3) * Time.deltaTime);
                //transform.rotation = Quaternion.Euler(0, 0, 180);
            }
            else
            {
                transform.position = Vector2.MoveTowards(transform.position, new Vector2(-12.5f, -2f), (speed / 3) * Time.deltaTime);
                //if (transform.localScale.x > 0)
                //    transform.rotation = Quaternion.Euler(0, 0, 0);
                //else
                //    transform.rotation = Quaternion.Euler(0, 0, 180);
            }
        }
    }

    public void Movement(float iSpeed)
    {
        switch (enemyMovementType)
        {
            case EnemyMovementType.Normal:
                normalMovement(iSpeed);
                break;
            case EnemyMovementType.Fake:
                normalMovement(iSpeed);
                fakeMovement();
                break;
            case EnemyMovementType.Redirect:
                normalMovement(iSpeed);
                redirectMovement(iSpeed);
                break;
        }
    }

    void normalMovement(float iSpeed)
    {
        switch (spawnPointType)
        {
            case SpawnPointType.Side:

                if (right)
                {
                    if (point.position.x <= 0 && once)
                        StartCoroutine(idle());

                    if (!fakeCheck)
                        rb.velocity = Vector2.left * iSpeed * ONE_HUNDRED * Time.deltaTime;
                    else
                        rb.velocity = Vector2.right * iSpeed * ONE_HUNDRED * Time.deltaTime;
                }
                else
                {
                    if (point.position.x >= 0 && once)
                        StartCoroutine(idle());

                    if (!fakeCheck)
                        rb.velocity = Vector2.right * iSpeed * ONE_HUNDRED * Time.deltaTime;
                    else
                        rb.velocity = Vector2.left * iSpeed * ONE_HUNDRED * Time.deltaTime;
                }

                break;
            case SpawnPointType.Top:

                if (top)
                {
                    if (point.position.y <= 0 && once)
                        StartCoroutine(idle());

                    if (!fakeCheck)
                        rb.velocity = Vector2.down * iSpeed * ONE_HUNDRED * Time.deltaTime;
                    else
                    rb.velocity = Vector2.up * iSpeed * ONE_HUNDRED * Time.deltaTime;
                }
                else
                {
                    if (point.position.y >= 0 && once)
                        StartCoroutine(idle());

                    if (!fakeCheck)
                        rb.velocity = Vector2.up * iSpeed * ONE_HUNDRED * Time.deltaTime;
                    else
                        rb.velocity = Vector2.down * iSpeed * ONE_HUNDRED * Time.deltaTime;
                }

                break;
        }
    }

    void fakeMovement()
    {
        switch (spawnPointType)
        {
            case SpawnPointType.Side:

                if (right)
                {
                    if (point.position.x <= 0)
                        fake();
                }
                else
                {
                    if (point.position.x >= 0)
                        fake();
                }

                break;
            case SpawnPointType.Top:

                if (top)
                {
                    if (point.position.y <= 0)
                        fake();
                }
                else
                {
                    if (point.position.y >= 0)
                        fake();
                }

                break;

        }
    }
    void fake()
    {
        if (once)
            StartCoroutine(idle());

        fakeCheck = true;

        Invoke("changeLocalScale", TIME);
    }

    private void changeLocalScale() => transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);

    void redirectMovement(float iSpeed)
    {
        switch (spawnPointType)
        {
            case SpawnPointType.Side:

                if (right)
                {
                    if (point.position.x <= 0)
                    {
                        if (once)
                            StartCoroutine(idle());

                        if (top)
                            rb.velocity = new Vector2(-iSpeed, -iSpeed) * ONE_HUNDRED * Time.deltaTime;
                        else
                            rb.velocity = new Vector2(-iSpeed, iSpeed) * ONE_HUNDRED * Time.deltaTime;

                        Invoke("changeRotation", TIME);
                    }
                }
                else
                {
                    if (point.position.x >= 0)
                    {
                        if (once)
                            StartCoroutine(idle());

                        if (top)
                            rb.velocity = new Vector2(iSpeed, -iSpeed) * ONE_HUNDRED * Time.deltaTime;
                        else
                            rb.velocity = new Vector2(iSpeed, iSpeed) * ONE_HUNDRED * Time.deltaTime;

                        Invoke("changeRotation", TIME);
                    }
                }

                break;
            case SpawnPointType.Top:

                if (top)
                {
                    if (point.position.y <= 0)
                    {
                        if (once)
                            StartCoroutine(idle());

                        if (right)
                            rb.velocity = new Vector2(-iSpeed, -iSpeed) * ONE_HUNDRED * Time.deltaTime;
                        else
                            rb.velocity = new Vector2(iSpeed, -iSpeed) * ONE_HUNDRED * Time.deltaTime;

                        Invoke("changeRotation", TIME);
                    }
                }
                else
                {
                    if (point.position.y >= 0)
                    {
                        if (once)
                            StartCoroutine(idle());

                        if (right)
                            rb.velocity = new Vector2(-iSpeed, iSpeed) * ONE_HUNDRED * Time.deltaTime;
                        else
                            rb.velocity = new Vector2(iSpeed, iSpeed) * ONE_HUNDRED * Time.deltaTime;

                        Invoke("changeRotation", TIME);
                    }
                }

                break;
        }
    }

    void changeRotation()
    {
        switch (spawnPointType)
        {
            case SpawnPointType.Side:

                if (right)
                {
                    if (point.position.x <= 0)
                    {
                        if (top)
                        {
                            point.position = new Vector2(point.position.x - 50f, point.position.y);
                            //transform.rotation = Quaternion.Euler(transform.rotation.x, transform.rotation.y, 45f);
                        }
                        else
                        {
                            point.position = new Vector2(point.position.x - 50f, point.position.y);
                            //transform.rotation = Quaternion.Euler(transform.rotation.x, transform.rotation.y, -45f);
                        }
                    }
                }
                else
                {
                    if (point.position.x >= 0)
                    {
                        if (top)
                        {
                            point.position = new Vector2(point.position.x + 50f, point.position.y);
                            //transform.rotation = Quaternion.Euler(transform.rotation.x, transform.rotation.y, -45f);
                        }
                        else
                        {
                            point.position = new Vector2(point.position.x + 50f, point.position.y);
                            //transform.rotation = Quaternion.Euler(transform.rotation.x, transform.rotation.y, 45f);
                        }
                    }
                }

                break;
            case SpawnPointType.Top:

                if (top)
                {
                    if (point.position.y <= 0)
                    {
                        if (right)
                        {
                            point.position = new Vector2(point.position.x + 50f, point.position.y);
                            //transform.rotation = Quaternion.Euler(transform.rotation.x, transform.rotation.y, 45f);
                        }
                        else
                        {
                            point.position = new Vector2(point.position.x - 50f, point.position.y);
                            //transform.rotation = Quaternion.Euler(transform.rotation.x, transform.rotation.y, 135f);
                        }
                    }
                }
                else
                {
                    if (point.position.y >= 0)
                    {
                        if (right)
                        {
                            point.position = new Vector2(point.position.x - 50f, point.position.y);
                            //transform.rotation = Quaternion.Euler(transform.rotation.x, transform.rotation.y, 45f);
                        }
                        else
                        {
                            point.position = new Vector2(point.position.x - 50f, point.position.y);
                            //transform.rotation = Quaternion.Euler(transform.rotation.x, transform.rotation.y, -45f);
                        }
                    }
                }

                break;
        }
    }

    void setRotationAndLocalScale()
    {
        switch (spawnPointType)
        {
            case SpawnPointType.Side:
                if (right)
                {
                    transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, transform.localScale.z);
                    //transform.rotation = Quaternion.Euler(0, 0, 0);
                }
                else
                {
                    transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
                    //transform.rotation = Quaternion.Euler(0, 0, 0);
                }
                break;
            case SpawnPointType.Top:
                if (top)
                {
                    transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, transform.localScale.z);
                    //transform.rotation = Quaternion.Euler(0, 0, 90);
                }
                else
                {
                    transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
                    //transform.rotation = Quaternion.Euler(0, 0, 90);
                }
                break;
        }
    }

    IEnumerator idle()
    {
        anym.SetBool("IsMove", false);
        move = false;
        once = false;
        yield return new WaitForSeconds(TIME);
        move = true;
        anym.SetBool("IsMove", true);
        speed *= 3;
        if (startGetOut)
        {
            getOut = true;
        }
    }

    public void Attack()
    {
        // In "OnTriggerEnter2D" method \\
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
            GetComponent<CircleCollider2D>().enabled = false;

            switch (spawnPointType)
            {
                case SpawnPointType.Side:

                    if (right)
                        playerRb.AddForce(new Vector2(-knockbackForce.x, knockbackForce.y), ForceMode2D.Impulse);
                    else
                        playerRb.AddForce(knockbackForce, ForceMode2D.Impulse);

                    break;
                case SpawnPointType.Top:

                    if (transform.position.x >= collision.gameObject.transform.position.x)
                        playerRb.AddForce(new Vector2(-knockbackForce.x, knockbackForce.y), ForceMode2D.Impulse);
                    else
                        playerRb.AddForce(knockbackForce, ForceMode2D.Impulse);

                    break;
            }
        }

        if (collision.gameObject.CompareTag("Platform"))
        {
            GetComponent<CircleCollider2D>().enabled = false;
            once = true;
            startGetOut = true;
        }
    }
}