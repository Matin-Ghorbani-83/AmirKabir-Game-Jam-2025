using UnityEngine;

public class BulletEnemy : MonoBehaviour
{
    [SerializeField] SOShooterEnemy sOEnemy;
    bool right;


    private void Start()
    {
        if (transform.position.x > 0)
            right = true;
        else
            right = false;
    }

    void Update()
    {
        if (right)
            transform.Translate(Vector2.left * sOEnemy.BulletSpeed * Time.deltaTime);
        else
            transform.Translate(Vector2.right * sOEnemy.BulletSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Platform"))
            Destroy(gameObject);
    }
}
