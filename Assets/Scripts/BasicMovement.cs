using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using static ConsoleProDebug;

public class BasicMovement : NetworkBehaviour
{
    public float moveSpeed;
    Vector3 movement;
    public Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    void Update()
    {
        if(!IsLocalPlayer) return;
        Watch("horizontal", "" + Input.GetAxisRaw("Horizontal"));
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
}
