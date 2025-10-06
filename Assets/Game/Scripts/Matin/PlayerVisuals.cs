using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerVisuals : MonoBehaviour
{
    [SerializeField] GameObject PlayerSprite;

    private void Start()
    {
        //PlayerController.instance.OnPlayerMovement =+
    }
    private void Update()
    {
        float dir = PlayerController.instance.GetDirection();
        if (dir == -1)
        {
            PlayerSprite.transform.localScale = new Vector3(-.5f, .5f, .5f);
        }
        else if(dir==1)
        {
            PlayerSprite.transform.localScale= new(.5f, .5f, .5f);
        }
    }  
}
