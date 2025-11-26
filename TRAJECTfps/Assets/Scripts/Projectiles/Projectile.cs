using UnityEngine;

public class Projectile : MonoBehaviour
{
        private Rigidbody rb;

        private bool targetHit;
        
        private void Start()
        {
            rb = GetComponent<Rigidbody>();
        }

    private void OnCollisionEnter(Collision collision)
    {
        if (targetHit)
            return;
        else
            targetHit = true;

        // Stick the projectile to the target
        rb.isKinematic = true;

        // make sure projectile moves with the target
        transform.SetParent(collision.transform);
    }
}


//THROWING Grenades, Knives and Other Objects - Unity Tutorial 5:37/8:07
