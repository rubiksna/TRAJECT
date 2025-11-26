using UnityEngine;

public class Throwing : MonoBehaviour
{
    [Header("References")]
    public Transform cam;
    public Transform attackPoint;
    public GameObject ObjectToThrow;

    [Header("Settings")]
    public int totalThrows;
    public float throwcooldown;

    [Header("Throwing")]
    public KeyCode throwKey = KeyCode.Mouse0;
    public float throwForce;
    public float throwUpwardForce;

    bool readyToThrow;

    private void Start()
    {
        readyToThrow = true;
    }

    private void Update()
    {
        if (Input.GetKeyDown(throwKey) && readyToThrow && totalThrows > 0)
        {
            Throw();
        }
    }

    private void Throw()
    { 
        readyToThrow = false;

        // Instantiate object to throw
        GameObject projectile = Instantiate(ObjectToThrow, attackPoint.position, cam.rotation);

        // Get Rigidbody component
        Rigidbody projectileRb = projectile.GetComponent<Rigidbody>();

        // calculate direction
        Vector3 forceDirection = cam.forward;

        RaycastHit hit;

        if(Physics.Raycast(cam.position, cam.forward, out hit, 500f))
        {
            forceDirection = (hit.point - attackPoint.position).normalized;
        }

        // Add force to the object
        Vector3 forceToAdd = forceDirection * throwForce + transform.up * throwUpwardForce;

        projectileRb.AddForce(forceToAdd, ForceMode.Impulse);

        // Decrement remaining throws
        totalThrows--;

        // Implement throw cooldown
        Invoke(nameof(ResetThrow), throwcooldown);
    }

    private void ResetThrow()
    {
        readyToThrow = true;
    }
}
