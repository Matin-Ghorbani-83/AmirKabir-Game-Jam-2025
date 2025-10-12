using UnityEngine;

public class PlayerZone : MonoBehaviour
{
    [SerializeField] float xZone;
    [SerializeField] float yZone;

    private void Update()
    {
        if(transform.position.x <= -xZone)
            transform.position = new Vector2(-xZone, transform.position.y);
        else if (transform.position.x >= xZone)
            transform.position = new Vector2(xZone, transform.position.y);

        if (transform.position.y >= yZone)
            transform.position = new Vector2(transform.position.x, yZone);
    }
}
