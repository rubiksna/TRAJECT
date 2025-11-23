using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Vector3 velocity;
    private Vector3 PlayerMovementInput;
    private Vector2 PlayerMouseInput;
    private float xRot;

    [SerializeField] private Transform playerCamera;
    [SerializeField] private CharacterController controller;
    [Space]
    [SerializeField] private float speed;
    [SerializeField] private float jumpForce;
    [SerializeField] private float sensitivity;
    [SerializeField] private float gravity = -9.81f;


    void Update()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        PlayerMovementInput = transform.right * horizontalInput + transform.forward * verticalInput;
        PlayerMouseInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        movePlayer();
        moveCamera();
    }

    private void movePlayer ()
    {
        Vector3 MoveVector = transform.TransformDirection(PlayerMovementInput);

        if(controller.isGrounded)
        {
            velocity.y = -1f;

            if(Input.GetButtonDown("Jump")) {
                velocity.y = jumpForce;
            }
        }
        else
        {
            velocity.y -= gravity * -2f * Time.deltaTime;
        }

            controller.Move(MoveVector * speed * Time.deltaTime);
        controller.Move(velocity * Time.deltaTime);
    }

    private void moveCamera ()
    {
        xRot -= PlayerMouseInput.y * sensitivity;

        transform.Rotate(0f, PlayerMouseInput.x * sensitivity, 0f);
        playerCamera.transform.localRotation = Quaternion.Euler(xRot, 0f, 0f);
    }
}
