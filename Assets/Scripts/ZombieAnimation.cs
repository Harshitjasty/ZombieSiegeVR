
using UnityEngine;

public class ZombieAnimation : MonoBehaviour
{
    private Animator animator;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
        else
        {
            Debug.LogWarning("No Animator found on this zombie.");
        }
    }

    private void Update()
    {
        if (animator == null)
            return;

        // Keep the walk animation playing.
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName("HumanWalking2"))
        {
            animator.Play("HumanWalking2", 0, 0f);
        }
    }
}
