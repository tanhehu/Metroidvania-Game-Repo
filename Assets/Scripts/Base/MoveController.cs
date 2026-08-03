using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveController : MonoBehaviour
{
    private Vector3 direction; 
    public Vector3 Direction => direction;

    public float speed; 

    public virtual void Move(Vector3 direction)
    {
        transform.position += direction * speed * Time.deltaTime;
    }
}
