using UnityEngine;
using SimpleFactory;

public class BirdEnemy : MonoBehaviour, IEnemy
{
    const int ONE_HUNDRED = 100;

    [Header("Choice Point Position")]
    public SpawnPointType spawnPointType;
    [Header("Choice Movement Type")]
    public EnemyMovementType enemyMovementType;

    [Space(height: 20f)]
    [SerializeField] SOEnemy sOEnemy;
    [SerializeField] Transform point;

    float speed;
    Vector2 knockbackForce;

    Rigidbody2D rb;

    bool right;
    bool top;


    private void Start()
    {
        speed = sOEnemy.Speed;
        knockbackForce = sOEnemy.KnockbackForce;

        rb = GetComponent<Rigidbody2D>();

        if (transform.position.x <= 0)
            right = false;
        else
            right = true;

        if (transform.position.y <= 0)
            top = false;
        else
            top = true;

        setRotationAndLocalScale();
    }

    private void Update() => Movement(speed);

    public void Movement(float iSpeed)
    {
        normalMovement(iSpeed);

        switch (enemyMovementType)
        {
            case EnemyMovementType.Normal:
                // null
                break;
            case EnemyMovementType.Fake:
                fakeMovement();
                break;
            case EnemyMovementType.Redirect:
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
                    rb.velocity = Vector2.left * iSpeed * ONE_HUNDRED * Time.deltaTime;
                else
                    rb.velocity = Vector2.right * iSpeed * ONE_HUNDRED * Time.deltaTime;

                break;
            case SpawnPointType.Top:

                if (top)
                    rb.velocity = Vector2.down * iSpeed * ONE_HUNDRED * Time.deltaTime;
                else
                    rb.velocity = Vector2.up * iSpeed * ONE_HUNDRED * Time.deltaTime;

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
        speed = -speed;
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
    }

    void redirectMovement(float iSpeed)
    {
        switch (spawnPointType)
        {
            case SpawnPointType.Side:

                if (right)
                {
                    if (point.position.x <= 0)
                    {
                        if (top)
                            rb.velocity = new Vector2(-iSpeed, -iSpeed) * ONE_HUNDRED * Time.deltaTime;
                        else
                            rb.velocity = new Vector2(-iSpeed, iSpeed) * ONE_HUNDRED * Time.deltaTime;
                    }
                }
                else
                {
                    if (point.position.x >= 0)
                    {
                        if (top)
                            rb.velocity = new Vector2(iSpeed, -iSpeed) * ONE_HUNDRED * Time.deltaTime;
                        else
                            rb.velocity = new Vector2(iSpeed, iSpeed) * ONE_HUNDRED * Time.deltaTime;
                    }
                }

                break;
            case SpawnPointType.Top:

                if (top)
                {
                    if (point.position.y <= 0)
                    {
                        if (right)
                            rb.velocity = new Vector2(-iSpeed, -iSpeed) * ONE_HUNDRED * Time.deltaTime;
                        else
                            rb.velocity = new Vector2(iSpeed, -iSpeed) * ONE_HUNDRED * Time.deltaTime;
                    }
                }
                else
                {
                    if (point.position.y >= 0)
                    {
                        if (right)
                            rb.velocity = new Vector2(-iSpeed, iSpeed) * ONE_HUNDRED * Time.deltaTime;
                        else
                            rb.velocity = new Vector2(iSpeed, iSpeed) * ONE_HUNDRED * Time.deltaTime;
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
                    transform.rotation = Quaternion.Euler(0, 0, 0);
                }
                else
                {
                    transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
                    transform.rotation = Quaternion.Euler(0, 0, 0);
                }
                break;
            case SpawnPointType.Top:
                if (top)
                {
                    transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, transform.localScale.z);
                    transform.rotation = Quaternion.Euler(0, 0, 90);
                }
                else
                {
                    transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
                    transform.rotation = Quaternion.Euler(0, 0, 90);
                }
                break;
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
    }
}