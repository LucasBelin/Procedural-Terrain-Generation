using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Camera cam;

    public float moveSpeed = 10f;
    public float mouseSensitivity;

    private bool stop = false;

    void Update()
    {
        Cursor.lockState = CursorLockMode.Locked;
        RotateCamera();
        if (!stop)
        {
            UpdateMoveSpeed();
            MovePlayer();
        }

        if(Input.GetKeyDown(KeyCode.Space))
        {
            stop = !stop;
        }
    }

    void RotateCamera()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        float rotAmountX = mouseX * mouseSensitivity;
        float rotAmountY = mouseY * mouseSensitivity;

        Vector3 rotPlayer = transform.rotation.eulerAngles;

        rotPlayer.x -= rotAmountY;
        rotPlayer.z = 0;
        rotPlayer.y += rotAmountX;

        transform.rotation = Quaternion.Euler(rotPlayer);
    }

    void MovePlayer()
    {
        Vector3 movement = transform.rotation * Vector3.forward;
        transform.position += movement * Time.deltaTime * moveSpeed;
    }

    void UpdateMoveSpeed()
    {
        float mouseScroll = Input.GetAxis("Mouse ScrollWheel");
        moveSpeed += mouseScroll * 5f;
    }
}
