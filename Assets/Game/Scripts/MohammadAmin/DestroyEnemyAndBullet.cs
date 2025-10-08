using UnityEngine;

public class DestroyEnemyAndBullet : MonoBehaviour
{
    const float X = 12, Y = 7;
    // Use this numbers in "GetOut" IEnumarator of ShooterEnemy script and "Update" method of BirdEnemy \\

    void Update() => myDestroy();

    void myDestroy()
    {
        if (transform.position.x < -X ||
            transform.position.x > X  ||
            transform.position.y < -Y ||
            transform.position.y > Y)
            Destroy(gameObject);
    }
}
