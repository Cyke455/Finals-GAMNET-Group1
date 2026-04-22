 using UnityEngine;

public class FruitController : MonoBehaviour
{
    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public void Collect()
    {
        _animator.SetTrigger("Collect");
    }

    // Called as an animation event
    public void OnCollect()
    {
        Destroy(gameObject);
    }
}
