using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Coord PlayerPosition { get; protected set; }
    public float animSpeed = 5;
    public float waitAfterMove = 0.3f;
    public float maxSpeed = 5;

    private bool moving = false;

    public Action<int, int, Coord> OnPlayerMove;

    private void Start()
    {
        PlayerPosition = new Coord(0, 0);
    }

    private void Update()
    {
        if (moving)
            return;
        int h = (int)Input.GetAxisRaw("Horizontal");
        int v = (int)Input.GetAxisRaw("Vertical");
        if (Mathf.Max(Mathf.Abs(h), Mathf.Abs(v)) > 0.8)
        {
            if (Mathf.Abs(h) + Mathf.Abs(v) > 1)
            {
                return;
            }
            PlayerPosition = new Coord(PlayerPosition.x + h, PlayerPosition.y + v);
            if (OnPlayerMove != null)
                OnPlayerMove(h, v, PlayerPosition);
            StartCoroutine(Movement(h, v));
        }
    }

    Vector3 velocity;

    private IEnumerator Movement(int h, int v)
    {
        moving = true;
        Vector3 finalPos = this.transform.position + new Vector3(h, v);
        while (Vector3.Distance(this.transform.position, finalPos) > 0.01)
        {
            this.transform.position = Vector3.SmoothDamp(this.transform.position, finalPos, ref velocity, 1f / animSpeed, maxSpeed);
            yield return null;
        }
        this.transform.position = finalPos;
        yield return new WaitForSeconds(waitAfterMove);
        moving = false;
    }
}
