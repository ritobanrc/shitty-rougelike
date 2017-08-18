using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;

    private Vector3 offset;
    private void Start()
    {
        offset = transform.position - player.position;
    }
    private void Update()
    {
        this.transform.position = offset + player.position;
    }

}
